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

        public AcademicCalendarAiExtractor(OpenAiVisionClient vision)
        {
            _vision = vision;
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
            "نهاية فترة تقديم طلبات الاعتذار"
        };

        private sealed class AiRoot
        {
            public List<AiEvent>? events { get; set; }
        }

        private sealed class AiEvent
        {
            public string? @event { get; set; }
            public string? gregorian_date { get; set; }
        }

        public async Task<List<AcademicCalendarEvent>> ExtractEventsFromPdfAsync(string pdfPath, int calendarId)
        {
            var images = await PdfToImages.RenderAllPagesAsPngAsync(pdfPath);

            if (images == null || images.Count == 0)
                throw new InvalidOperationException("Could not render PDF pages to images.");

            var prompt = BuildPrompt();

            var aiText = await _vision.GetJsonFromImagesAsync(prompt, images);
            var json = ExtractJsonObject(aiText) ?? aiText;

            AiRoot? root;
            try
            {
                root = JsonSerializer.Deserialize<AiRoot>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                throw new InvalidOperationException("AI returned invalid JSON.");
            }

            if (root?.events == null)
                throw new InvalidOperationException("AI returned empty events list.");

            var result = new List<AcademicCalendarEvent>();

            foreach (var allowedEvent in AllowedEvents)
            {
                var matched = root.events.FirstOrDefault(e =>
                    string.Equals(NormalizeText(e.@event), NormalizeText(allowedEvent), StringComparison.Ordinal));

                if (matched == null)
                    continue;

                if (string.IsNullOrWhiteSpace(matched.gregorian_date) ||
                    matched.gregorian_date.Trim().ToLower() == "null")
                {
                    continue;
                }

                var date = ParseGregorianDateOrThrow(matched.gregorian_date);

                result.Add(new AcademicCalendarEvent
                {
                    CalendarId = calendarId,
                    EventName = allowedEvent,
                    GregorianDate = date,
                    HijriDate = "-",
                    DayAr = null
                });
            }

            return result;
        }

        private static string BuildPrompt()
        {
            var sb = new StringBuilder();

            sb.AppendLine("أنت متخصص في قراءة التقويمات الأكاديمية العربية من الصور.");
            sb.AppendLine("اقرأ صفحات التقويم كاملة بدقة.");
            sb.AppendLine("استخرج فقط الأحداث التالية، وأعد فقط التاريخ الميلادي لكل حدث.");
            sb.AppendLine("إذا لم تجد الحدث بشكل مؤكد فأعد gregorian_date = null.");
            sb.AppendLine("لا تخمن.");
            sb.AppendLine("يجب مطابقة الحدث بالنص الكامل.");
            sb.AppendLine("يجب أخذ التاريخ من نفس العمود المرتبط بالحدث.");
            sb.AppendLine("لا تخرج أي شرح.");
            sb.AppendLine("أعد JSON فقط.");
            sb.AppendLine();
            sb.AppendLine("الأحداث المطلوبة بالنص EXACT:");
            for (int i = 0; i < AllowedEvents.Length; i++)
            {
                sb.AppendLine($"{i + 1}) {AllowedEvents[i]}");
            }

            sb.AppendLine();
            sb.AppendLine("قواعد التنظيف:");
            sb.AppendLine("- احذف الرموز داخل الأقواس مثل (W1) (W2) وغيرها.");
            sb.AppendLine("- وحّد ODUS و PLUS ODUS إلى ODUS PLUS.");
            sb.AppendLine("- إذا كان النص موزعًا على أكثر من سطر، اجمعه في سطر واحد.");
            sb.AppendLine("- لا تغيّر معنى اسم الحدث.");
            sb.AppendLine();
            sb.AppendLine("صيغة الإخراج المطلوبة EXACT:");
            sb.AppendLine(@"
{
  ""events"": [
    {
      ""event"": ""بداية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""نهاية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""بداية فترة تسجيل المقررات للمرشدين الأكاديميين على ODUS PLUS وللشؤون التعليمية والوكلاء والوكيلات بالكليات"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""نهاية فترة التسجيل للمرشدين الأكاديميين"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""بداية تقديم طلبات سحب مقرر للطالب والطالبات في الفصل الدراسي الحالي"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""نهاية فترة تقديم طلب سحب مقرر للفصل الدراسي الحالي"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""بداية تقديم طلبات التأجيل"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""نهاية تقديم طلبات التأجيل"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""بداية تقديم طلبات الاعتذار"",
      ""gregorian_date"": ""dd/MM/yyyy""
    },
    {
      ""event"": ""نهاية فترة تقديم طلبات الاعتذار"",
      ""gregorian_date"": ""dd/MM/yyyy""
    }
  ]
}");
            return sb.ToString();
        }

        private static DateTime ParseGregorianDateOrThrow(string input)
        {
            var value = input.Trim();

            value = value.Replace("م", "").Trim();
            value = Regex.Replace(value, @"\s+", "");

            if (DateTime.TryParseExact(
                value,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            {
                return dt.Date;
            }

            throw new InvalidOperationException($"Invalid gregorian date returned by AI: {input}");
        }

        private static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var t = text;

            t = Regex.Replace(t, @"ODUS\s*PLUS|PLUS\s*ODUS|ODUSPLUS|ODUS", "ODUS PLUS", RegexOptions.IgnoreCase);

            t = Regex.Replace(t, @"\([A-Za-z]\d+\)", " ");
            t = Regex.Replace(t, @"[■◆●•\u2022\u25A0\u25C6]+", " ");
            t = t.Replace("للطلاب", "للطالب");
            t = t.Replace("الطلاب", "الطالب");
            t = t.Replace("والطالبات", "والطالبات");
            t = Regex.Replace(t, @"\s+", " ").Trim();

            return t;
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