using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Acadify.Models.Db;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.RegularExpressions;



namespace Acadify.Controllers
{
    public class StudentController : Controller
    {

        private readonly AcadifyDbContext _context;

       


        private readonly AcadifyDbContext _db;
        private readonly IWebHostEnvironment _env;

        public StudentController(AcadifyDbContext db, IWebHostEnvironment env , AcadifyDbContext context)
        {
            _db = db;
            _env = env;
            _context = context;
        }

        // =========================
        // Helpers
        // =========================

        private static string ReadPdfText(string fullPath)
        {
            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(fullPath))
            {
                foreach (var page in document.GetPages())
                    sb.AppendLine(page.Text);
            }

            return sb.ToString();
        }

        private sealed class ParsedTranscript
        {
            public decimal? CumulativeGpa { get; set; }
            public decimal? LatestTermGpa { get; set; }
            public HashSet<string> CourseCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static ParsedTranscript ParseTranscriptText(string text)
        {
            var result = new ParsedTranscript();

            if (string.IsNullOrWhiteSpace(text))
                return result;

            var normalized = Regex.Replace(text, @"\s+", " ").Trim();

            // Cumulative GPA
            var cumMatch = Regex.Match(
                normalized,
                @"Cumulative\s*GPA\s*[:\-]?\s*([0-5]\.\d{2})",
                RegexOptions.IgnoreCase);

            if (!cumMatch.Success)
            {
                cumMatch = Regex.Match(
                    normalized,
                    @"([0-5]\.\d{2})\s*Cumulative\s*GPA",
                    RegexOptions.IgnoreCase);
            }

            if (cumMatch.Success)
                result.CumulativeGpa = TryDec(cumMatch.Groups[1].Value);

            // Latest Term GPA
            var termMatches = Regex.Matches(
                normalized,
                @"\bTerm\b.*?\b([0-5]\.\d{2})\b",
                RegexOptions.IgnoreCase);

            if (termMatches.Count > 0)
                result.LatestTermGpa = TryDec(termMatches[^1].Groups[1].Value);

            // Course codes: any academic-looking code مثل CPIS352 أو FMIS420 أو ARAB201
            var blockedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "FALL", "SPRING", "SUMMER", "WINTER", "TERM", "PAGE"
    };

            foreach (Match m in Regex.Matches(normalized, @"(?<![A-Z0-9])([A-Z]{3,6})\s*[-]?\s*(\d{3})(?!\d)"))
            {
                var prefix = m.Groups[1].Value.ToUpperInvariant();
                var number = m.Groups[2].Value;
                var code = prefix + number;

                if (blockedPrefixes.Contains(prefix))
                    continue;

                result.CourseCodes.Add(code);
            }

            return result;

            static decimal? TryDec(string s)
            {
                if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                    return d;
                return null;
            }
        }
        private static (decimal? cumulativeGpa, decimal? lastTermGpa) ParseGpaFromTranscriptText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (null, null);

            var normalized = Regex.Replace(text, @"\s+", " ").Trim();

            decimal? cumulative = null;

            var m1 = Regex.Match(normalized, @"Cumulative\s*GPA\s*[:\-]?\s*([0-5]\.\d{2})", RegexOptions.IgnoreCase);
            if (m1.Success) cumulative = TryDec(m1.Groups[1].Value);

            if (!cumulative.HasValue)
            {
                var m3 = Regex.Match(normalized, @"\b\d+\s+([0-5]\.\d{2})\s*Cumulative\s*Total", RegexOptions.IgnoreCase);
                if (m3.Success) cumulative = TryDec(m3.Groups[1].Value);
            }

            decimal? term = null;
            {
                var tail = normalized.Length > 2500 ? normalized[^2500..] : normalized;
                var nums = Regex.Matches(tail, @"\b[0-5]\.\d{2}\b")
                    .Select(x => TryDec(x.Value))
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();

                if (nums.Count > 0)
                {
                    if (cumulative.HasValue)
                        nums = nums.Where(x => x != cumulative.Value).ToList();

                    if (nums.Count > 0)
                        term = nums.Max();
                }
            }

            return (cumulative, term);

            static decimal? TryDec(string s)
            {
                if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                    return d;
                return null;
            }
        }

        private static List<string> ExtractCourseCodesFromPdf(string fullPath)
        {
            var blockedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "FALL", "SPRING", "SUMMER", "WINTER", "TERM", "PAGE"
    };

            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var doc = UglyToad.PdfPig.PdfDocument.Open(fullPath);

            foreach (var page in doc.GetPages())
            {
                var words = page.GetWords().ToList();

                // Case 1: CPIS352 or CPIS-352
                foreach (var word in words)
                {
                    var text = word.Text.Trim().ToUpperInvariant();

                    var m = Regex.Match(text, @"^([A-Z]{3,6})[-]?(\d{3})$");
                    if (m.Success)
                    {
                        var prefix = m.Groups[1].Value.ToUpperInvariant();
                        var code = prefix + m.Groups[2].Value;

                        if (!blockedPrefixes.Contains(prefix))
                            results.Add(code);
                    }
                }

                // Case 2: CPIS 352
                var lineGroups = words
                    .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                    .OrderByDescending(g => g.Key);

                foreach (var line in lineGroups)
                {
                    var tokens = line
                        .OrderBy(w => w.BoundingBox.Left)
                        .Select(w => w.Text.Trim().ToUpperInvariant())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    for (int i = 0; i < tokens.Count - 1; i++)
                    {
                        var p = tokens[i];
                        var n = tokens[i + 1];

                        if (Regex.IsMatch(p, @"^[A-Z]{3,6}$") && Regex.IsMatch(n, @"^\d{3}$"))
                        {
                            if (!blockedPrefixes.Contains(p))
                                results.Add(p + n);
                        }
                    }
                }
            }

            return results.OrderBy(x => x).ToList();
        }



        private static List<string> ExtractCourseCodesByKnownTitles(string text)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(text))
                return results.ToList();

            var normalized = Regex.Replace(text, @"\s+", " ").ToUpperInvariant();

            var titleToCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "ARABIC LANGUAGE (2)", "ARAB201" },
        { "IS APPLICATIONS DESIGN & DEVEL", "CPIS352" },
        { "HISTORY OF ASTRONOMY", "ASTR203" },
        { "PERSUASION", "ARAB292" },
        { "PRINCIPLES OF MARKETING", "MRKT260" },
        { "INFORMATION & COMPUTER SECURIT", "CPIS312" },
        { "PRINCIPLES OF HUMAN COMPUTER I", "CPIS354" },
        { "INTERNET APPLICATIONS&WEB PRO", "CPIS358" },
        { "SOFTWARE QUALITY AND TESTING", "CPIS357" },
        { "INTELLIGENT SYSTEMS", "CPIS363" },
        { "INTRODUCTION TO E-BUSINESS SYS", "CPIS380" },
        { "SUMMER(WORKPLACE) TRAINING", "CPIS323" },
                 { "SYSTEMS DESIGN PATTERNS", "CPIS350" }
    };

            foreach (var item in titleToCode)
            {
                if (normalized.Contains(item.Key.ToUpperInvariant()))
                    results.Add(item.Value);
            }

            return results.OrderBy(x => x).ToList();
        }



        private static Dictionary<string, int> ExtractCourseHoursMapFromPdf(string fullPath, List<string>? targetCourseIds)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using var doc = UglyToad.PdfPig.PdfDocument.Open(fullPath);

            foreach (var page in doc.GetPages())
            {
                var words = page.GetWords()
                    .OrderByDescending(w => w.BoundingBox.Bottom)
                    .ThenBy(w => w.BoundingBox.Left)
                    .ToList();

                var lines = GroupWordsIntoLines(words);

                foreach (var line in lines)
                {
                    var tokens = line
                        .OrderBy(w => w.BoundingBox.Left)
                        .Select(w => w.Text.Trim().ToUpperInvariant())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (tokens.Count == 0)
                        continue;

                    var lineCourseIds = ExtractCourseIdsFromTokens(tokens);

                    foreach (var courseId in lineCourseIds)
                    {
                        if (targetCourseIds != null && !targetCourseIds.Contains(courseId, StringComparer.OrdinalIgnoreCase))
                            continue;

                        if (result.ContainsKey(courseId))
                            continue;

                        if (!TryFindCourseOnLine(tokens, courseId, out var codeIndex))
                            continue;

                        if (TryExtractHourNearCourse(tokens, codeIndex, out var hours))
                            result[courseId] = hours;
                    }
                }
            }

            return result;
        }

        private static List<List<UglyToad.PdfPig.Content.Word>> GroupWordsIntoLines(List<UglyToad.PdfPig.Content.Word> words)
        {
            var lines = new List<List<UglyToad.PdfPig.Content.Word>>();

            foreach (var word in words)
            {
                var existingLine = lines.FirstOrDefault(line =>
                    Math.Abs(line[0].BoundingBox.Bottom - word.BoundingBox.Bottom) <= 3.5);

                if (existingLine == null)
                {
                    lines.Add(new List<UglyToad.PdfPig.Content.Word> { word });
                }
                else
                {
                    existingLine.Add(word);
                }
            }

            return lines;
        }

        private static List<string> ExtractCourseIdsFromTokens(List<string> tokens)
        {
            var result = new List<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                var m = Regex.Match(tokens[i], @"^([A-Z]{3,6})[-]?(\d{3})$");
                if (m.Success)
                {
                    result.Add((m.Groups[1].Value + m.Groups[2].Value).ToUpperInvariant());
                    continue;
                }

                if (i < tokens.Count - 1 &&
                    Regex.IsMatch(tokens[i], @"^[A-Z]{3,6}$") &&
                    Regex.IsMatch(tokens[i + 1], @"^\d{3}$"))
                {
                    result.Add((tokens[i] + tokens[i + 1]).ToUpperInvariant());
                }
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool TryFindCourseOnLine(List<string> tokens, string courseId, out int codeIndex)
        {
            codeIndex = -1;

            var upperCourseId = courseId.ToUpperInvariant();
            var prefix = new string(upperCourseId.TakeWhile(char.IsLetter).ToArray());
            var number = new string(upperCourseId.SkipWhile(char.IsLetter).ToArray());

            for (int i = 0; i < tokens.Count; i++)
            {
                var m = Regex.Match(tokens[i], @"^([A-Z]{3,6})[-]?(\d{3})$");
                if (m.Success)
                {
                    var code = (m.Groups[1].Value + m.Groups[2].Value).ToUpperInvariant();
                    if (code == upperCourseId)
                    {
                        codeIndex = i;
                        return true;
                    }
                }

                if (i < tokens.Count - 1 &&
                    Regex.IsMatch(tokens[i], @"^[A-Z]{3,6}$") &&
                    tokens[i] == prefix &&
                    tokens[i + 1] == number)
                {
                    codeIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtractHourNearCourse(List<string> tokens, int codeIndex, out int hours)
        {
            hours = 0;

            var candidates = new List<(int Distance, int Value, bool IsBefore)>();

            for (int i = Math.Max(0, codeIndex - 10); i <= Math.Min(tokens.Count - 1, codeIndex + 10); i++)
            {
                if (i == codeIndex)
                    continue;

                if (Regex.IsMatch(tokens[i], @"^[0-5]$"))
                {
                    int value = int.Parse(tokens[i]);
                    int distance = Math.Abs(i - codeIndex);
                    bool isBefore = i < codeIndex;

                    candidates.Add((distance, value, isBefore));
                }
            }

            if (candidates.Count == 0)
                return false;

            var best = candidates
                .OrderBy(c => c.Distance)
                .ThenBy(c => c.IsBefore ? 0 : 1)
                .ThenBy(c => c.Value == 0 ? 1 : 0)
                .First();

            hours = best.Value;
            return true;
        }










        // Student Home Page
        public IActionResult StudentHome()
        {
            // مؤقتًا: القيمة جاية من الإيجنت
            // لاحقًا: تستبدل بقيمة من Database
            int progressFromAgent = 80;

            var model = new StudentHomeViewModel
            {
                StudentName = "lama alshikh",
                StudentEmail = "lalshikh@stu.kau.edu.sa",
                ProgressPercentage = progressFromAgent,
                CurrentStatus = GetStatus(progressFromAgent)
            };

            return View(model);
        }

        // تحديد حالة الطالبة بناءً على نسبة التقدم
        private string GetStatus(int progress)
        {
            if (progress <= 30)
                return "Beginning";

            if (progress <= 70)
                return "Has Remaining Courses";

            return "Near Graduation";
        }
        // =======================
        // Upload Transcript Page
        // =======================

        // GET: Student/UploadTranscript
        // =========================
        [HttpGet]
        public IActionResult UploadTranscript()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTranscript(IFormFile transcriptFile)
        {
            // 1) Validate file
            if (transcriptFile == null || transcriptFile.Length == 0)
            {
                ViewBag.Error = "Please select a PDF file.";
                return View();
            }

            var ext = Path.GetExtension(transcriptFile.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                ViewBag.Error = "Only PDF files are allowed.";
                return View();
            }

            // 2) Save file
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "transcripts");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await transcriptFile.CopyToAsync(stream);
            }

            // 3) Extract text (PdfPig)
            string extractedText;
            try
            {
                extractedText = ReadPdfText(fullPath);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "PDF uploaded, but text extraction failed: " + ex.Message;
                return View();
            }

            // 4) Parse GPA من النص
            var (cumulativeGpa, latestTermGpa) = ParseGpaFromTranscriptText(extractedText);

            // 5) Extract course codes من النص + من layout الـ PDF + من أسماء المواد المعروفة
            var parsedTranscript = ParseTranscriptText(extractedText);
            var pdfCourseCodes = ExtractCourseCodesFromPdf(fullPath);
            var titleBasedCodes = ExtractCourseCodesByKnownTitles(extractedText);

            // 6) دمج الأكواد
            var courseCodesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var code in parsedTranscript.CourseCodes)
                courseCodesSet.Add(code);

            foreach (var code in pdfCourseCodes)
                courseCodesSet.Add(code);

            foreach (var code in titleBasedCodes)
                courseCodesSet.Add(code);

            // 7) تنظيف الأكواد غير الصحيحة
            courseCodesSet.RemoveWhere(code =>
                string.IsNullOrWhiteSpace(code) ||
                code.StartsWith("FALL", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("SPRING", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("SUMMER", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("WINTER", StringComparison.OrdinalIgnoreCase));

            // اقرأ ساعات المواد من الترانسكربت
            var transcriptHourMap = ExtractCourseHoursMapFromPdf(fullPath, courseCodesSet.ToList());

            // ✅ Debug
            ViewBag.CodesCount = courseCodesSet.Count;
            ViewBag.CodesPreview = string.Join(", ", courseCodesSet.OrderBy(x => x));

            // 8) TEMP studentId (مؤقت الآن)
            int studentId = 2210783;

            // 9) UPSERT transcript + include Courses
            var transcript = await _db.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            if (transcript == null)
            {
                transcript = new Transcript { StudentId = studentId };
                _db.Transcripts.Add(transcript);

                await _db.SaveChangesAsync();
                _db.Entry(transcript).Collection(t => t.Courses).Load();
            }

            // 10) Update fields
            transcript.PdfFile = $"/uploads/transcripts/{fileName}";
            transcript.ExtractedInfo = extractedText;

            if (cumulativeGpa.HasValue) transcript.Gpa = cumulativeGpa.Value;
            if (latestTermGpa.HasValue) transcript.SemesterGpa = latestTermGpa.Value;

            transcript.ExtractedCourses = courseCodesSet.Count == 0
                ? null
                : string.Join(", ", courseCodesSet.OrderBy(x => x));

            // 11) Fill TranscriptCourse
            transcript.Courses.Clear();

            if (courseCodesSet.Count > 0)
            {
                var coursesInDb = await _db.Courses
                    .Where(c => courseCodesSet.Contains(c.CourseId))
                    .ToListAsync();

                foreach (var course in coursesInDb)
                {
                    bool isUnclassified = string.IsNullOrWhiteSpace(course.RequirementCategory);
                    bool hasZeroHours = course.Hours <= 0;

                    if ((isUnclassified || hasZeroHours) &&
                        transcriptHourMap.TryGetValue(course.CourseId, out var extractedHours) &&
                        extractedHours > 0)
                    {
                        course.Hours = extractedHours;
                    }
                }

                var existingIds = coursesInDb
                    .Select(c => c.CourseId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingIds = courseCodesSet
                    .Where(id => !existingIds.Contains(id))
                    .ToList();

                foreach (var id in missingIds)
                {
                    if (id.StartsWith("FALL", StringComparison.OrdinalIgnoreCase) ||
                        id.StartsWith("SPRING", StringComparison.OrdinalIgnoreCase) ||
                        id.StartsWith("SUMMER", StringComparison.OrdinalIgnoreCase) ||
                        id.StartsWith("WINTER", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var newCourse = new Course
                    {
                        CourseId = id,
                        CourseName = id,
                        Hours = transcriptHourMap.TryGetValue(id, out var extractedHours) ? extractedHours : 0
                    };

                    _db.Courses.Add(newCourse);
                    coursesInDb.Add(newCourse);
                }

                foreach (var c in coursesInDb)
                    transcript.Courses.Add(c);
            }

            await _db.SaveChangesAsync();

            ViewBag.Success = $"Uploaded successfully for StudentId = {studentId} (updated if already existed).";
            ViewBag.DebugPreview = extractedText.Length > 250
                ? extractedText.Substring(0, 250)
                : extractedText;

            return View();
        }



        // =======================
        // Student Chat
        // =======================

        [HttpGet("/Student/Chat")]
        public IActionResult Chat()
        {
            int meetingId = 1; // مؤقتًا

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);

            if (meeting == null)
                return NotFound();

            string recordingMessage = "";

            if (meeting.LastRecordingAction == "started")
                recordingMessage = "The chat recording is started";
            else if (meeting.LastRecordingAction == "stopped")
                recordingMessage = "The chat recording is stopped";

            ViewBag.RecordingMessage = recordingMessage;

            var messages = _context.MeetingMessages
                .Where(m => m.MeetingId == meetingId)
                .OrderBy(m => m.MessageDate)
                .Select(m => new ChatMessageVM
                {
                    SenderName = (m.SenderName ?? "")
                        .Replace("(me)", "", StringComparison.OrdinalIgnoreCase)
                        .Trim(),
                    Text = m.MessageText,
                    IsFromStudent = m.SenderName != null &&
                                    m.SenderName.Trim().ToLower().Contains("lama"),
                    TimeText = m.MessageDate.HasValue ? m.MessageDate.Value.ToString("hh:mm tt") : "",
                    IsRecorded = m.IsRecorded
                })
                .ToList();

            var model = new StudentChatViewModel
            {
                AdvisorName = "DR. Amina Hasan Gamlo",
                StudentName = "Lama Alshaikh",
                IsRecordingStarted = meeting.IsRecordingStarted,
                Messages = messages
            };

            return View(model);
        }

        [HttpPost("/Student/SendMessage")]
        public IActionResult SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Redirect("/Student/Chat");

            int meetingId = 1; // مؤقتًا

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);

            if (meeting == null)
                return NotFound();

            var newMessage = new MeetingMessage
            {
                MeetingId = meetingId,
                SenderName = "Lama Alshaikh",
                MessageText = message,
                MessageDate = DateTime.Now,
                IsRecorded = meeting.IsRecordingStarted
            };

            _context.MeetingMessages.Add(newMessage);
            _context.SaveChanges();

            return Redirect("/Student/Chat");
        }

        [HttpGet("/Student/Test")]
        public IActionResult Test()
        {
            return Content("Student works");
        }




    }
}
