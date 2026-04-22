using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Acadify.Models;
using UglyToad.PdfPig;

namespace Acadify.Services
{
    public class TranscriptAiParserService : ITranscriptAiParserService
    {
        private readonly AiSummaryService _aiSummaryService;

        private static readonly HashSet<string> PassedGrades = new(StringComparer.OrdinalIgnoreCase)
        {
            "A+", "A", "B+", "B", "C+", "C", "D+", "D", "P"
        };

        private static readonly HashSet<string> KnownGrades = new(StringComparer.OrdinalIgnoreCase)
        {
            "A+", "A", "B+", "B", "C+", "C", "D+", "D",
            "F", "P", "NP", "W", "WF", "WP", "IP", "I", "IC"
        };

        public TranscriptAiParserService(AiSummaryService aiSummaryService)
        {
            _aiSummaryService = aiSummaryService;
        }

        public async Task<List<TranscriptCourseItem>> ParseTranscriptAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new List<TranscriptCourseItem>();

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            string rawText;
            try
            {
                rawText = ExtractPdfText(memoryStream);
            }
            catch
            {
                return new List<TranscriptCourseItem>();
            }

            if (string.IsNullOrWhiteSpace(rawText))
                return new List<TranscriptCourseItem>();

            var cleanedText = CleanTranscriptText(rawText);

            if (string.IsNullOrWhiteSpace(cleanedText))
                return new List<TranscriptCourseItem>();

            string aiResponse = string.Empty;

            try
            {
                string prompt = BuildPrompt(cleanedText);

                aiResponse = await _aiSummaryService.GetRawResponseAsync(
                    prompt,
                    "You extract structured course data from university transcripts and return clean JSON only.",
                    1600);
            }
            catch
            {
                aiResponse = string.Empty;
            }

            var aiCourses = NormalizeAndFilterCourses(ParseJsonResponse(aiResponse));
            if (aiCourses.Any())
                return aiCourses;

            // Fallback لو الـ AI رجّع شيء غير قابل للـ deserialize
            var regexCourses = NormalizeAndFilterCourses(ParseCoursesFromRawText(cleanedText));
            return regexCourses;
        }

        private string ExtractPdfText(Stream pdfStream)
        {
            var sb = new StringBuilder();

            using var document = PdfDocument.Open(pdfStream);
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            return sb.ToString();
        }

        private string CleanTranscriptText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var lines = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join(Environment.NewLine, lines);
        }

        private string BuildPrompt(string transcriptText)
        {
            // تقليل النص إذا كان طويل جدًا
            if (transcriptText.Length > 15000)
                transcriptText = transcriptText[..15000];

            return
                "You are reading a university transcript for a student in the Information Systems department.\n\n" +
                "Extract only actual course records from the transcript.\n\n" +
                "For each course, return:\n" +
                "- courseId\n" +
                "- grade\n" +
                "- isPassed\n\n" +
                "Rules:\n" +
                "1. Normalize course IDs to this format: ABCD-123\n" +
                "2. If the transcript shows codes like \"CPIS 342\", convert them to \"CPIS-342\"\n" +
                "3. Passed grades are: A+, A, B+, B, C+, C, D+, D, P\n" +
                "4. Not passed grades include: F, NP, W, WF, WP, IP, I, IC\n" +
                "5. Ignore GPA, totals, semester labels, page headers, student info, signatures, and non-course text\n" +
                "6. Return valid JSON array only, with no markdown fences and no explanation\n" +
                "7. If a course appears multiple times, return the latest meaningful record if possible\n" +
                "8. Output format exactly like this:\n" +
                "[\n" +
                "  {\n" +
                "    \"courseId\": \"CPIS-342\",\n" +
                "    \"grade\": \"A+\",\n" +
                "    \"isPassed\": true\n" +
                "  }\n" +
                "]\n\n" +
                "Transcript text:\n" +
                transcriptText;
        }

        private List<TranscriptCourseItem> ParseJsonResponse(string aiResponse)
        {
            if (string.IsNullOrWhiteSpace(aiResponse))
                return new List<TranscriptCourseItem>();

            var candidates = ExtractJsonCandidates(aiResponse);

            foreach (var candidate in candidates)
            {
                var directList = TryDeserializeList(candidate);
                if (directList.Any())
                    return directList;

                var wrappedList = TryDeserializeWrappedList(candidate);
                if (wrappedList.Any())
                    return wrappedList;
            }

            return new List<TranscriptCourseItem>();
        }

        private List<string> ExtractJsonCandidates(string aiResponse)
        {
            var results = new List<string>();

            if (string.IsNullOrWhiteSpace(aiResponse))
                return results;

            var trimmed = aiResponse.Trim();

            results.Add(trimmed);

            // إزالة ```json
            var noFence = trimmed
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "")
                .Trim();

            if (!string.IsNullOrWhiteSpace(noFence) && !results.Contains(noFence))
                results.Add(noFence);

            // استخراج أول array
            int firstBracket = noFence.IndexOf('[');
            int lastBracket = noFence.LastIndexOf(']');
            if (firstBracket >= 0 && lastBracket > firstBracket)
            {
                var arrayOnly = noFence.Substring(firstBracket, lastBracket - firstBracket + 1).Trim();
                if (!results.Contains(arrayOnly))
                    results.Add(arrayOnly);
            }

            // استخراج أول object
            int firstBrace = noFence.IndexOf('{');
            int lastBrace = noFence.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                var objectOnly = noFence.Substring(firstBrace, lastBrace - firstBrace + 1).Trim();
                if (!results.Contains(objectOnly))
                    results.Add(objectOnly);
            }

            return results;
        }

        private List<TranscriptCourseItem> TryDeserializeList(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<List<TranscriptCourseItem>>(json, options);
                return result ?? new List<TranscriptCourseItem>();
            }
            catch
            {
                return new List<TranscriptCourseItem>();
            }
        }

        private List<TranscriptCourseItem> TryDeserializeWrappedList(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return new List<TranscriptCourseItem>();

                foreach (var propName in new[] { "courses", "items", "data", "result" })
                {
                    if (doc.RootElement.TryGetProperty(propName, out var prop) &&
                        prop.ValueKind == JsonValueKind.Array)
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var result = JsonSerializer.Deserialize<List<TranscriptCourseItem>>(prop.GetRawText(), options);
                        return result ?? new List<TranscriptCourseItem>();
                    }
                }

                return new List<TranscriptCourseItem>();
            }
            catch
            {
                return new List<TranscriptCourseItem>();
            }
        }

        private List<TranscriptCourseItem> ParseCoursesFromRawText(string text)
        {
            var result = new List<TranscriptCourseItem>();

            if (string.IsNullOrWhiteSpace(text))
                return result;

            string pattern =
                @"\b([A-Z]{4})\s*-?\s*(\d{3})\b" +
                @"(?:(?!\b[A-Z]{4}\s*-?\s*\d{3}\b).){0,120}?" +
                @"\b(A\+|A|B\+|B|C\+|C|D\+|D|F|P|NP|W|WF|WP|IP|IC|I)\b";

            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                var courseId = $"{match.Groups[1].Value.ToUpper()}-{match.Groups[2].Value}";
                var grade = NormalizeGrade(match.Groups[3].Value);

                result.Add(new TranscriptCourseItem
                {
                    CourseId = courseId,
                    Grade = grade,
                    IsPassed = PassedGrades.Contains(grade)
                });
            }

            return result;
        }

        private List<TranscriptCourseItem> NormalizeAndFilterCourses(IEnumerable<TranscriptCourseItem> courses)
        {
            return courses
                .Where(x => x != null)
                .Select(x =>
                {
                    var normalizedGrade = NormalizeGrade(x.Grade);

                    return new TranscriptCourseItem
                    {
                        CourseId = NormalizeCourseId(x.CourseId),
                        Grade = normalizedGrade,
                        IsPassed = x.IsPassed || PassedGrades.Contains(normalizedGrade)
                    };
                })
                .Where(x => IsValidCourseId(x.CourseId))
                .Where(x => !string.IsNullOrWhiteSpace(x.Grade))
                .Where(x => KnownGrades.Contains(x.Grade))
                .GroupBy(x => x.CourseId)
                .Select(g =>
                {
                    var passed = g.FirstOrDefault(x => x.IsPassed);
                    return passed ?? g.First();
                })
                .OrderBy(x => x.CourseId)
                .ToList();
        }

        private string NormalizeCourseId(string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return string.Empty;

            courseId = courseId.Trim().ToUpper();
            courseId = courseId.Replace(" ", "")
                               .Replace("_", "")
                               .Replace("/", "")
                               .Replace("--", "-");

            if (!courseId.Contains("-"))
            {
                var match = Regex.Match(courseId, @"^([A-Z]{4})(\d{3})$");
                if (match.Success)
                    return $"{match.Groups[1].Value}-{match.Groups[2].Value}";
            }

            return courseId;
        }

        private string NormalizeGrade(string? grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
                return string.Empty;

            grade = grade.Trim().ToUpper();
            grade = grade.Replace(" ", "");

            return grade;
        }

        private bool IsValidCourseId(string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return false;

            return Regex.IsMatch(courseId, @"^[A-Z]{4}-\d{3}$");
        }
    }
}