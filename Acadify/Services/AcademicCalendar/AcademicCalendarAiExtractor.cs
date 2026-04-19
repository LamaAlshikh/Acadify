using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Acadify.Models;
using Acadify.Services.AcademicCalendar.Interfaces;

namespace Acadify.Services.AcademicCalendar
{
    public class AcademicCalendarAiExtractor : IAcademicCalendarAiExtractor
    {
        private readonly OpenAiVisionClient _vision;
        private readonly IPdfOcrService _ocr;

        public AcademicCalendarAiExtractor(OpenAiVisionClient vision, IPdfOcrService ocr)
        {
            _vision = vision;
            _ocr = ocr;
        }

        private static readonly string[] AllowedEvents =
        {
            "بداية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
            "نهاية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
            "بداية فترة تسجيل المقررات للمرشدين الأكاديميين على ODUS PLUS وللشؤون التعليمية والوكلاء والوكيلات بالكليات",
            "نهاية فترة التسجيل للمرشدين الأكاديميين",
            "بداية تقديم طلبات سحب مقرر للطالب والطالبات في الفصل الدراسي الحالي",
            "نهاية فترة تقديم طلب سحب مقرر للفصل الدراسي الحالي",
            "بداية تقديم طلبات التأجيل",
            "نهاية تقديم طلبات التأجيل",
            "بداية تقديم طلبات الاعتذار",
            "نهاية فترة تقديم طلبات الاعتذار",
            "إجازة نهاية العام"
        };

        private sealed class SingleEventRoot
        {
            public string? gregorian_date { get; set; }
        }

        public async Task<List<AcademicCalendarEvent>> ExtractEventsFromPdfAsync(string pdfPath, int calendarId)
        {
            var images = await PdfToImages.RenderAllPagesAsPngAsync(pdfPath);

            if (images == null || images.Count == 0)
                throw new InvalidOperationException("Could not render PDF pages to images.");

            int selectedPageIndex = await DetectTargetPageByTextAsync(pdfPath, images.Count);

            if (selectedPageIndex < 0 || selectedPageIndex >= images.Count)
                throw new InvalidOperationException($"Could not determine the correct second semester page. Page index = {selectedPageIndex}, Page count = {images.Count}");

            var selectedImage = images[selectedPageIndex];

            var result = new List<AcademicCalendarEvent>();

            foreach (var allowedEvent in AllowedEvents)
            {
                var aiText = await _vision.GetJsonFromImagesAsync(
                    BuildSingleEventPrompt(allowedEvent),
                    new List<byte[]> { selectedImage });

                var json = ExtractJsonObject(aiText) ?? aiText;

                SingleEventRoot? root;
                try
                {
                    root = JsonSerializer.Deserialize<SingleEventRoot>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    continue;
                }

                if (root == null ||
                    string.IsNullOrWhiteSpace(root.gregorian_date) ||
                    root.gregorian_date.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                    continue;

                var date = ParseGregorianDateOrThrow(root.gregorian_date);

                result.Add(new AcademicCalendarEvent
                {
                    CalendarId = calendarId,
                    EventName = allowedEvent,
                    GregorianDate = date,
                    HijriDate = "-",
                    DayAr = null
                });
            }

            ValidateResults(result);

            return result;
        }

        private async Task<int> DetectTargetPageByTextAsync(string pdfPath, int pageCount)
        {
            int bestPageIndex = -1;
            int bestScore = -1;

            for (int i = 0; i < pageCount; i++)
            {
                var pageText = await _ocr.ExtractPageTextByOcrAsync(pdfPath, i + 1);
                var normalized = NormalizeText(pageText);

                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                int score = 0;

                if (normalized.Contains(NormalizeText("الفصل الدراسي الثاني")))
                    score += 100;

                if (normalized.Contains(NormalizeText("الفصل الدراسي الاول")))
                    score -= 80;

                if (normalized.Contains(NormalizeText("الفصل الصيفي")))
                    score -= 80;

                foreach (var ev in AllowedEvents)
                {
                    score += CountMatchedWords(normalized, NormalizeText(ev)) * 5;
                }

                if (normalized.Contains(NormalizeText("بداية الدراسة")))
                    score += 10;

                if (normalized.Contains(NormalizeText("تسجيل المقررات")))
                    score += 10;

                if (normalized.Contains(NormalizeText("الاعتذار")))
                    score += 10;

                if (normalized.Contains(NormalizeText("التأجيل")))
                    score += 10;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPageIndex = i;
                }
            }

            return bestPageIndex;
        }

        private static string BuildSingleEventPrompt(string eventName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("أنت تقرأ صفحة واحدة من التقويم الأكاديمي العربي.");
            sb.AppendLine("المطلوب استخراج التاريخ الميلادي لحدث واحد فقط.");
            sb.AppendLine($"اسم الحدث المطلوب: {eventName}");
            sb.AppendLine("ابحث عن هذا الحدث بالنص داخل الصفحة.");
            sb.AppendLine("خذ التاريخ الميلادي المرتبط بنفس الخلية أو بنفس المربع أو الصف الخاص بهذا الحدث فقط.");
            sb.AppendLine("لا تستخدم تاريخًا من حدث آخر.");
            sb.AppendLine("لا تستخدم تاريخًا من أسبوع آخر.");
            sb.AppendLine("إذا لم تجد الحدث بشكل مؤكد فأعد gregorian_date = null.");
            sb.AppendLine("إذا كان داخل النص رموز مثل (W1) أو (W19) فتجاهلها.");
            sb.AppendLine("أعد JSON فقط بهذه الصيغة:");
            sb.AppendLine(@"
{
  ""gregorian_date"": ""dd/MM/yyyy""
}");

            return sb.ToString();
        }

        private static int CountMatchedWords(string actual, string expected)
        {
            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
                return 0;

            var words = expected
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .ToList();

            int matched = 0;

            foreach (var word in words)
            {
                if (actual.Contains(word))
                    matched++;
            }

            return matched;
        }
        private static DateTime ParseGregorianDateOrThrow(string input)
        {
            var value = input.Trim();

            value = ConvertArabicDigitsToEnglish(value);
            value = value.Replace("م", "").Trim();
            value = Regex.Replace(value, @"\s+", "");

            string[] formats =
            {
        "dd/MM/yyyy",
        "d/M/yyyy",
        "yyyy-MM-dd",
        "yyyy/M/d",
        "yyyy/MM/dd",
        "dd-MM-yyyy",
        "d-M-yyyy"
    };

            if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            {
                return dt.Date;
            }

            throw new InvalidOperationException($"Invalid gregorian date: {input}");
        }

        
        private static void ValidateResults(List<AcademicCalendarEvent> events)
        {
            if (events == null || events.Count == 0)
                throw new InvalidOperationException("No valid calendar events were extracted.");

            var duplicatedNames = events
                .GroupBy(e => e.EventName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatedNames.Any())
                throw new InvalidOperationException("Duplicate event names were found in extracted results.");
        }

        private static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var t = text;

            t = ConvertArabicDigitsToEnglish(t);

            t = Regex.Replace(t, @"ODUS\s*PLUS|PLUS\s*ODUS|ODUSPLUS|ODUS", "ODUS PLUS", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"\([A-Za-z]\d+\)", " ");
            t = Regex.Replace(t, @"[■◆●•\u2022\u25A0\u25C6]+", " ");

            t = t.Replace("أ", "ا")
                 .Replace("إ", "ا")
                 .Replace("آ", "ا")
                 .Replace("ى", "ي")
                 .Replace("ة", "ه")
                 .Replace("ؤ", "و")
                 .Replace("ئ", "ي")
                 .Replace("ـ", "");

            t = Regex.Replace(t, @"[\u064B-\u065F]", "");
            t = Regex.Replace(t, @"\s+", " ").Trim();

            return t;
        }

        private static string ConvertArabicDigitsToEnglish(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var map = new Dictionary<char, char>
            {
                ['٠'] = '0',
                ['١'] = '1',
                ['٢'] = '2',
                ['٣'] = '3',
                ['٤'] = '4',
                ['٥'] = '5',
                ['٦'] = '6',
                ['٧'] = '7',
                ['٨'] = '8',
                ['٩'] = '9',
                ['۰'] = '0',
                ['۱'] = '1',
                ['۲'] = '2',
                ['۳'] = '3',
                ['۴'] = '4',
                ['۵'] = '5',
                ['۶'] = '6',
                ['۷'] = '7',
                ['۸'] = '8',
                ['۹'] = '9'
            };

            var chars = input.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (map.TryGetValue(chars[i], out var replacement))
                    chars[i] = replacement;
            }

            return new string(chars);
        }

        private static string? ExtractJsonObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            if (start >= 0 && end > start)
                return text.Substring(start, end - start + 1);

            return null;
        }
    }
}