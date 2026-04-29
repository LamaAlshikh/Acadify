using Acadify.Models;
using Acadify.Services;
using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
=======
using System.IO;
using Acadify.Models.Db;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

>>>>>>> origin_second/rahafgh

namespace Acadify.Controllers
{
    public class StudentController : Controller
    {
<<<<<<< HEAD
        private readonly AcadifyDbContext _db;
        private readonly IWebHostEnvironment _env;
=======

        private readonly ITranscriptParserService _transcriptParserService;
        private readonly IRecommendationEngineService _recommendationEngineService;
        private readonly ITranscriptAiParserService _transcriptAiParserService;


        private readonly AcadifyDbContext _context;




        private readonly AcadifyDbContext _db;
        private readonly IWebHostEnvironment _env;

        public StudentController(AcadifyDbContext db, IWebHostEnvironment env, AcadifyDbContext context,
            ITranscriptParserService transcriptParserService,
            IRecommendationEngineService recommendationEngineService,
            ITranscriptAiParserService transcriptAiParserService
            )
        {
            _db = db;
            _env = env;
            _context = context;
            _transcriptParserService = transcriptParserService;
            _recommendationEngineService = recommendationEngineService;
            _transcriptAiParserService = transcriptAiParserService;
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










        // Student Home Page
        public IActionResult StudentHome()
        {
            // مؤقتًا: القيمة جاية من الإيجنت
            // لاحقًا: تستبدل بقيمة من Database
            int progressFromAgent = 80;
>>>>>>> origin_second/rahafgh

        public StudentController(AcadifyDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // =========================
        // Helpers
        // =========================

        private int? GetCurrentStudentId()
        {
            return HttpContext.Session.GetInt32("StudentId");
        }

        private async Task<int?> GetAdvisorIdForStudentAsync(int studentId)
        {
            return await _db.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.AdvisorId)
                .FirstOrDefaultAsync();
        }

        // load student name + email for sidebar
        private async Task LoadStudentSidebarDataAsync()
        {
            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
            {
                studentId = 2209009; // temporary for testing
            }

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                ViewBag.StudentName = "Student";
                ViewBag.StudentEmail = "";
                return;
            }

            ViewBag.StudentName = GetStringPropertyValue(student, "Name", "StudentName", "FullName");
            ViewBag.StudentEmail = GetStringPropertyValue(student, "Email", "StudentEmail", "UniversityEmail");
        }

        private static string GetStringPropertyValue(object obj, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var prop = obj.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj)?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return string.Empty;
        }

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

        private static (decimal? cumulativeGpa, decimal? lastTermGpa) ParseGpaFromTranscriptText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (null, null);

            var normalized = Regex.Replace(text, @"\s+", " ").Trim();

            decimal? cumulative = null;

            var m1 = Regex.Match(
                normalized,
                @"Cumulative\s*GPA\s*[:\-]?\s*([0-5]\.\d{2})",
                RegexOptions.IgnoreCase);

            if (m1.Success)
                cumulative = TryDec(m1.Groups[1].Value);

            if (!cumulative.HasValue)
            {
                var m3 = Regex.Match(
                    normalized,
                    @"\b\d+\s+([0-5]\.\d{2})\s*Cumulative\s*Total",
                    RegexOptions.IgnoreCase);

                if (m3.Success)
                    cumulative = TryDec(m3.Groups[1].Value);
            }

            decimal? term = null;

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
            var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CPIS","IS","CPCS","CPIT","BUS","ACCT","MRKT","ARAB",
                "STAT","ELIS","MATH","PHYS","CHEM","BIO","ASTR","COMM","ISLS"
            };

            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var doc = PdfDocument.Open(fullPath);

            foreach (var page in doc.GetPages())
            {
                var words = page.GetWords().ToList();

                var lineGroups = words
                    .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                    .OrderByDescending(g => g.Key);

                foreach (var line in lineGroups)
                {
                    var tokens = line
                        .OrderBy(w => w.BoundingBox.Left)
                        .Select(w => w.Text.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    foreach (var t in tokens)
                    {
                        var mm = Regex.Match(t, @"^([A-Z]{2,6})(\d{3,4})$");
                        if (mm.Success && prefixes.Contains(mm.Groups[1].Value))
                            results.Add((mm.Groups[1].Value + mm.Groups[2].Value).ToUpperInvariant());
                    }

                    for (int i = 0; i < tokens.Count - 1; i++)
                    {
                        var p = tokens[i].ToUpperInvariant();
                        var n = tokens[i + 1];

                        if (prefixes.Contains(p) && Regex.IsMatch(n, @"^\d{3,4}$"))
                            results.Add(p + n);
                    }
                }
            }

            return results.OrderBy(x => x).ToList();
        }

        private async Task<int> CreateNewForm5ForStudentAsync(int studentId)
        {
            var advisorId = await GetAdvisorIdForStudentAsync(studentId);

            if (!advisorId.HasValue || advisorId.Value <= 0)
                throw new InvalidOperationException("No advisor is assigned to this student.");

            var newForm5 = new Form
            {
                StudentId = studentId,
                AdvisorId = advisorId.Value,
                FormType = "Form 5",
                FormDate = DateTime.Now,
                FormStatus = "Pending",
                AdvisorNotes = null,
                AutoFilled = true,
                AdvisorConfirmation = null
            };

            _db.Forms.Add(newForm5);
            await _db.SaveChangesAsync();

            var details = new GraduationProjectEligibilityForm
            {
                FormId = newForm5.FormId,
                Eligibility = null,
                RequiredCoursesStatus = null
            };

            _db.GraduationProjectEligibilityForms.Add(details);
            await _db.SaveChangesAsync();

            return newForm5.FormId;
        }

        private int CalculateProgressPercentage(int remainingHours, int totalRequiredHours)
        {
            if (remainingHours <= 0)
                return 100;

            if (remainingHours <= 3)
                return 99;

            int completedHours = totalRequiredHours - remainingHours;

            double percentage = ((double)completedHours / totalRequiredHours) * 100;

            int roundedToTens = ((int)Math.Floor(percentage / 10.0)) * 10;

            if (roundedToTens < 10 && completedHours > 0)
                roundedToTens = 10;

            if (roundedToTens > 90)
                roundedToTens = 90;

            return roundedToTens;
        }

        private string CalculateCurrentStatus(int remainingHours)
        {
            if (remainingHours <= 0)
                return "Graduated";

            if (remainingHours <= 3)
                return "Near Graduation";

            return "Has Remaining Courses";
        }

        // =======================
        // Student Home Page
        // =======================
        [HttpGet]
        public async Task<IActionResult> StudentHome()
        {
            ViewData["Title"] = "Student Home";
            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
            {
                studentId = 2209009; // temporary for testing
            }

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                return NotFound("Student not found.");
            }

            var graduationStatus = await _db.GraduationStatuses
                .FirstOrDefaultAsync(g => g.StudentId == studentId.Value);

            int totalRequiredHours = 140;
            int remainingHours = graduationStatus?.RemainingHours ?? 140;

            var model = new StudenthomeViewModel
            {
                StudentName = GetStringPropertyValue(student, "Name", "StudentName", "FullName"),
                StudentEmail = GetStringPropertyValue(student, "Email", "StudentEmail", "UniversityEmail"),
                ProgressPercentage = CalculateProgressPercentage(remainingHours, totalRequiredHours),
                CurrentStatus = CalculateCurrentStatus(remainingHours)
            };

            return View(model);
        }

        // =======================
        // Upload Transcript Page
        // =======================
        [HttpGet]
        public async Task<IActionResult> UploadTranscript()
        {
            ViewData["Title"] = "Upload Transcript";
            await LoadStudentSidebarDataAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTranscript(IFormFile transcriptFile)
        {
            ViewData["Title"] = "Upload Transcript";
            await LoadStudentSidebarDataAsync();

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

            var studentIdSession = GetCurrentStudentId();

            if (!studentIdSession.HasValue)
            {
                ViewBag.Error = "Student session is not found. Please login again.";
                return View();
            }

            int studentId = studentIdSession.Value;

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                ViewBag.Error = "Student record is not found in the database.";
                return View();
            }

            var advisorId = await GetAdvisorIdForStudentAsync(studentId);
            if (!advisorId.HasValue || advisorId.Value <= 0)
            {
                ViewBag.Error = "No advisor is assigned to this student.";
                return View();
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "transcripts");
            Directory.CreateDirectory(uploadsFolder);

            var savedFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsFolder, savedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await transcriptFile.CopyToAsync(stream);
            }

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

            ViewBag.DebugStudentId = studentId;
            ViewBag.DebugOriginalFileName = transcriptFile.FileName;
            ViewBag.DebugPreview = extractedText.Length > 1000
                ? extractedText.Substring(0, 1000)
                : extractedText;

            var (cumulativeGpa, latestTermGpa) = ParseGpaFromTranscriptText(extractedText);

            var courseCodes = ExtractCourseCodesFromPdf(fullPath);
            var courseCodesSet = courseCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

            ViewBag.CodesCount = courseCodesSet.Count;
            ViewBag.CodesPreview = string.Join(", ", courseCodesSet.Take(30));

            var transcript = await _db.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            if (transcript == null)
            {
                transcript = new Transcript
                {
                    StudentId = studentId
                };

                _db.Transcripts.Add(transcript);
                await _db.SaveChangesAsync();
                await _db.Entry(transcript).Collection(t => t.Courses).LoadAsync();
            }

            transcript.PdfFile = $"/uploads/transcripts/{savedFileName}";
            transcript.ExtractedInfo = extractedText;

            if (cumulativeGpa.HasValue)
                transcript.Gpa = cumulativeGpa.Value;

            if (latestTermGpa.HasValue)
                transcript.SemesterGpa = latestTermGpa.Value;

            transcript.ExtractedCourses = courseCodesSet.Count == 0
                ? null
                : string.Join(", ", courseCodesSet);

            transcript.Courses.Clear();

            if (courseCodesSet.Count > 0)
            {
                var coursesInDb = await _db.Courses
                    .Where(c => courseCodesSet.Contains(c.CourseId))
                    .ToListAsync();

                var existingIds = coursesInDb
                    .Select(c => c.CourseId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingIds = courseCodesSet
                    .Where(id => !existingIds.Contains(id))
                    .ToList();

                foreach (var id in missingIds)
                {
                    var newCourse = new Course
                    {
                        CourseId = id,
                        CourseName = id,
                        Hours = 0
                    };

                    _db.Courses.Add(newCourse);
                    coursesInDb.Add(newCourse);
                }

                foreach (var c in coursesInDb)
                    transcript.Courses.Add(c);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(StudentHome));
        }

        // =======================
        // Student Chat
        // =======================
        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            ViewData["Title"] = "Chat";
            await LoadStudentSidebarDataAsync();

            var model = new StudentChatViewModel
            {
                AdvisorName = "DR. Amina Hasan Gamlo",
                StudentName = "Lama Alshikh",
                IsRecordingStarted = true,
                Messages = new List<ChatMessageVM>
                {
                    new ChatMessageVM
                    {
                        SenderName = "Lama Alshikh (me)",
                        Text = "السلام عليكم دكتورة، أبغى اطلب اجتماع بخصوص حذف مادة CPIS-352",
                        IsFromStudent = true
                    },
                    new ChatMessageVM
                    {
                        SenderName = "Amina Gamlo",
                        Text = "وعليكم السلام ورحمة الله وبركاته، تمام. متى تبغي نسوي الاجتماع؟",
                        IsFromStudent = false
                    },
                    new ChatMessageVM
                    {
                        SenderName = "Lama Alshikh (me)",
                        Text = "تمام",
                        IsFromStudent = true
                    },
                    new ChatMessageVM
                    {
                        SenderName = "Amina Gamlo",
                        Text = "ايش السبب اللي خلاك تبغي تحذف المادة هذي؟",
                        IsFromStudent = false
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult SendMessage(string message)
        {
            return RedirectToAction("Chat");
        }

        /* =========================================================
                          CommunityStudent
           ========================================================= */
        [HttpGet]
        public async Task<IActionResult> CommunityStudent()
        {
            ViewData["Title"] = "Community Student";
            await LoadStudentSidebarDataAsync();

            var model = new CommunityStudentVM
            {
                Messages = new List<CommunityMessageVM>
                {
                    new CommunityMessageVM
                    {
                        SenderName = "Lina Alrwaily",
                        SenderInitials = "LA",
                        MessageText = "السلام عليكم دكتورة أمينة",
                        IsCurrentUserMessage = false,
                        BubbleColorClass = "msg-blue"
                    },
                    new CommunityMessageVM
                    {
                        SenderName = "Lina Alrwaily",
                        SenderInitials = "LA",
                        MessageText = "هل اقدر أنزل مادة تطوير برمجيات الترم الجاي؟",
                        IsCurrentUserMessage = false,
                        BubbleColorClass = "msg-blue"
                    },
                    new CommunityMessageVM
                    {
                        SenderName = "Rahaf Alghamdi",
                        SenderInitials = "RA",
                        MessageText = "ايوا دكتورة حتى انا",
                        IsCurrentUserMessage = false,
                        BubbleColorClass = "msg-pink"
                    },
                    new CommunityMessageVM
                    {
                        SenderName = "Amina Gamlo",
                        SenderInitials = "AG",
                        MessageText = "و عليكم السلام و رحمة الله و بركاته\nليش ما تبغو تنزلوها هذا الترم؟",
                        IsCurrentUserMessage = false,
                        BubbleColorClass = "msg-purple"
                    },
                    new CommunityMessageVM
                    {
                        SenderName = "Lama Alshaikh (me)",
                        SenderInitials = "LA",
                        MessageText = "عندي استفسار بخصوص التدريب",
                        IsCurrentUserMessage = true,
                        BubbleColorClass = "msg-indigo"
                    }
                },

                Members = new List<CommunityMemberVM>
                {
                    new CommunityMemberVM
                    {
                        Name = "DR.Amina Gamlo",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Lina Alrwaily",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Rahaf Alghamdi",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Rahaf Alzahrani",
                        ImagePath = "~/images/user.png"
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult SendStudentMessage([FromBody] SendStudentMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Message is empty." });
            }

            return Json(new
            {
                success = true,
                text = request.Message.Trim()
            });
        }

        public class SendStudentMessageRequest
        {
            public string Message { get; set; } = string.Empty;
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



            /*
             * var courseCodesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

               foreach (var code in parsedTranscript.CourseCodes)
               courseCodesSet.Add(code);

               foreach (var code in pdfCourseCodes)
               courseCodesSet.Add(code);

               foreach (var code in titleBasedCodes)
               courseCodesSet.Add(code);
             */

            var courseCodesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var code in parsedTranscript.CourseCodes)
            {
                var cleanCode = code?.Trim().ToUpper();
                if (!string.IsNullOrWhiteSpace(cleanCode))
                    courseCodesSet.Add(cleanCode);
            }

            foreach (var code in pdfCourseCodes)
            {
                var cleanCode = code?.Trim().ToUpper();
                if (!string.IsNullOrWhiteSpace(cleanCode))
                    courseCodesSet.Add(cleanCode);
            }

            foreach (var code in titleBasedCodes)
            {
                var cleanCode = code?.Trim().ToUpper();
                if (!string.IsNullOrWhiteSpace(cleanCode))
                    courseCodesSet.Add(cleanCode);
            }


            // 7) تنظيف الأكواد غير الصحيحة
            courseCodesSet.RemoveWhere(code =>
                string.IsNullOrWhiteSpace(code) ||
                code.StartsWith("FALL", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("SPRING", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("SUMMER", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("WINTER", StringComparison.OrdinalIgnoreCase));

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

                /*
                 * var coursesInDb = await _db.Courses
                      .Where(c => courseCodesSet.Contains(c.CourseId))
                      .ToListAsync();
                 */
                var coursesInDb = await _db.Courses
                   .Where(c => courseCodesSet.Contains(c.CourseId.Trim().ToUpper()))
                   .ToListAsync();


                /*
                 * var existingIds = coursesInDb
                    .Select(c => c.CourseId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                 */
                var existingIds = coursesInDb
                   .Select(c => c.CourseId.Trim().ToUpper())
                   .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingIds = courseCodesSet
                    .Where(id => !existingIds.Contains(id))
                    .ToList();

                /*
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
                        Hours = 0
                    };

                    _db.Courses.Add(newCourse);
                    coursesInDb.Add(newCourse);
                }
                */

                foreach (var id in missingIds)
                {
                    if (id.StartsWith("FALL", StringComparison.OrdinalIgnoreCase) ||
                        id.StartsWith("SPRING", StringComparison.OrdinalIgnoreCase) ||
                        id.StartsWith("SUMMER", StringComparison.OrdinalIgnoreCase) ||
                        id.StartsWith("WINTER", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // ممكن تحطين هذا للديبغ فقط
                    Console.WriteLine($"Course not found in DB: {id}");
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


        private string ExtractLastAcademicTerm(string? extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "";

            var matches = Regex.Matches(
                extractedInfo,
                @"\b(FALL|SPRING|SUMMER|WINTER)\s+\d{4}/\d{4}\b",
                RegexOptions.IgnoreCase);

            if (matches.Count == 0)
                return "";

            return matches[matches.Count - 1].Value.ToUpper();
        }

        private string GetNextSemester(string currentSemester)
        {
            if (string.IsNullOrWhiteSpace(currentSemester))
                return "";

            if (currentSemester.Contains("FALL", StringComparison.OrdinalIgnoreCase))
                return "SPRING";
            if (currentSemester.Contains("SPRING", StringComparison.OrdinalIgnoreCase))
                return "SUMMER";
            if (currentSemester.Contains("SUMMER", StringComparison.OrdinalIgnoreCase))
                return "FALL";

            return "";
        }

        private string CalculateLevelFromHours(int earnedHours)
        {
            if (earnedHours <= 0) return "";
            if (earnedHours < 35) return "Level 1-2";
            if (earnedHours < 70) return "Level 3-4";
            if (earnedHours < 105) return "Level 5-6";
            return "Level 7-8";
        }


        private string ExtractDropSubjects(string extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "";

            var lines = extractedInfo
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var dropSubjects = lines
                .Where(l =>
                    l.Contains("drop", StringComparison.OrdinalIgnoreCase) ||
                    l.Contains("withdraw", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return dropSubjects.Any() ? string.Join(", ", dropSubjects) : "";
        }

        private string ExtractICSubjects(string extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "";

            var lines = extractedInfo
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var icSubjects = lines
                .Where(l => l.Contains("IC", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return icSubjects.Any() ? string.Join(", ", icSubjects) : "";
        }

        private string ExtractIPSubjects(string extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "";

            var lines = extractedInfo
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var ipSubjects = lines
                .Where(l => l.Contains("IP", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return ipSubjects.Any() ? string.Join(", ", ipSubjects) : "";
        }



      
        [HttpGet]
        public IActionResult CourseRecommendation()
        {
            int studentId = 2210783;

            var student = _db.Students
                .Include(s => s.Transcript)
                .ThenInclude(t => t.Courses)
                .FirstOrDefault(s => s.StudentId == studentId);

            if (student == null)
                return NotFound("Student not found.");

            ViewBag.StudentName = student.Name;
            ViewBag.StudentEmail = "student@kau.edu.sa";
            ViewBag.ActivePage = "CourseRecommendation";

            var transcript = student.Transcript;

            var selectedJson = HttpContext.Session.GetString("SelectedRecommendedCourses");
            var selected = string.IsNullOrEmpty(selectedJson)
                ? new List<SelectedCourseVM>()
                : JsonSerializer.Deserialize<List<SelectedCourseVM>>(selectedJson) ?? new List<SelectedCourseVM>();

            var cardsJson = HttpContext.Session.GetString("RecommendedCards");
            var cards = string.IsNullOrEmpty(cardsJson)
                ? new List<CourseCardVM>()
                : JsonSerializer.Deserialize<List<CourseCardVM>>(cardsJson) ?? new List<CourseCardVM>();

            var freeElectiveCourse1 = HttpContext.Session.GetString("FreeElectiveCourse1") ?? "";
            var freeElectiveCourse2 = HttpContext.Session.GetString("FreeElectiveCourse2") ?? "";
            var freeElectiveCourse3 = HttpContext.Session.GetString("FreeElectiveCourse3") ?? "";


            var model = new CourseRecommendationViewModel
            {
                StudentName = student.Name,
                StudentId = student.StudentId,
                Gpa = transcript?.Gpa,
                SemesterGpa = transcript?.SemesterGpa,
                Selected = selected,
                Cards = cards,
                FreeElectiveCourse1 = freeElectiveCourse1,
                FreeElectiveCourse2 = freeElectiveCourse2,
                FreeElectiveCourse3 = freeElectiveCourse3
            };

            return View(model);
        }






        [HttpPost]
        public IActionResult SelectRecommendedCourse(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return RedirectToAction("CourseRecommendation");

            var course = _db.Courses.FirstOrDefault(c => c.CourseId == courseId);
            if (course == null)
                return RedirectToAction("CourseRecommendation");

            var selectedJson = HttpContext.Session.GetString("SelectedRecommendedCourses");
            var selected = string.IsNullOrEmpty(selectedJson)
                ? new List<SelectedCourseVM>()
                : JsonSerializer.Deserialize<List<SelectedCourseVM>>(selectedJson) ?? new List<SelectedCourseVM>();

            if (!selected.Any(c => c.CourseId == courseId))
            {
                selected.Add(new SelectedCourseVM
                {
                    CourseId = course.CourseId,
                    Hours = course.Hours
                });
            }

            HttpContext.Session.SetString("SelectedRecommendedCourses", JsonSerializer.Serialize(selected));

            return RedirectToAction("CourseRecommendation");
        }

        [HttpPost]
        public IActionResult RemoveRecommendedCourse(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return RedirectToAction("CourseRecommendation");

            var selectedJson = HttpContext.Session.GetString("SelectedRecommendedCourses");
            var selected = string.IsNullOrEmpty(selectedJson)
                ? new List<SelectedCourseVM>()
                : JsonSerializer.Deserialize<List<SelectedCourseVM>>(selectedJson) ?? new List<SelectedCourseVM>();

            selected = selected.Where(c => c.CourseId != courseId).ToList();

            HttpContext.Session.SetString("SelectedRecommendedCourses", JsonSerializer.Serialize(selected));

            return RedirectToAction("CourseRecommendation");
        }

        [HttpPost]
        public IActionResult SwapRecommendedCourse(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return RedirectToAction("CourseRecommendation");

            var cardsJson = HttpContext.Session.GetString("RecommendedCards");
            var cards = string.IsNullOrEmpty(cardsJson)
                ? new List<CourseCardVM>()
                : JsonSerializer.Deserialize<List<CourseCardVM>>(cardsJson) ?? new List<CourseCardVM>();

            if (!cards.Any())
                return RedirectToAction("CourseRecommendation");

            var currentCard = cards.FirstOrDefault(c => c.CourseId == courseId);
            if (currentCard == null)
                return RedirectToAction("CourseRecommendation");

            var usedIds = cards.Select(c => c.CourseId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var replacement = _db.Courses
                .Where(c => !usedIds.Contains(c.CourseId) && c.CourseId != courseId)
                .OrderBy(c => c.CourseId)
                .FirstOrDefault();

            if (replacement != null)
            {
                currentCard.CourseId = replacement.CourseId;
                currentCard.CourseName = replacement.CourseName;
                currentCard.Hours = replacement.Hours;
                currentCard.IsCompleted = false;
                currentCard.CanTake = true;
                currentCard.IsSelected = false;
                currentCard.IsDisabled = false;

                HttpContext.Session.SetString("RecommendedCards", JsonSerializer.Serialize(cards));
            }

            return RedirectToAction("CourseRecommendation");
        }

        [HttpPost]
        public async Task<IActionResult> SendCourseRecommendationToAdvisor()
        {
            int studentId = 2210783; // مؤقتًا
            int advisorId = 1;       // مؤقتًا

            var selectedJson = HttpContext.Session.GetString("SelectedRecommendedCourses");
            var selectedCourses = string.IsNullOrEmpty(selectedJson)
                ? new List<SelectedCourseVM>()
                : JsonSerializer.Deserialize<List<SelectedCourseVM>>(selectedJson) ?? new List<SelectedCourseVM>();

            int advisedHours = selectedCourses.Sum(x => x.Hours);

            var latestForm = await _db.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            Form form;
            CourseChoiceMonitoringForm form2;

            if (latestForm == null)
            {
                form = new Form
                {
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    FormType = "Form 2",
                    FormDate = DateTime.Now,
                    FormStatus = "Sent",
                    AdvisorNotes = null,
                    AutoFilled = true,
                    AdvisorConfirmation = null
                };

                _db.Forms.Add(form);
                await _db.SaveChangesAsync();

                form2 = new CourseChoiceMonitoringForm
                {
                    FormId = form.FormId
                };

                _db.CourseChoiceMonitoringForms.Add(form2);
            }
            else
            {
                form = latestForm;
                form2 = latestForm.CourseChoiceMonitoringForm ?? new CourseChoiceMonitoringForm
                {
                    FormId = form.FormId
                };

                if (latestForm.CourseChoiceMonitoringForm == null)
                    _db.CourseChoiceMonitoringForms.Add(form2);
            }

            form.FormDate = DateTime.Now;
            form.FormStatus = "Sent";

            form2.Semester = "";
            form2.ComingSemester = "";
            form2.RunningCreditHours = 0;
            form2.AdvisedCreditHours = advisedHours;
            form2.Level = "";
            form2.DropSubjects = "";
            form2.ICSubjects = "";
            form2.IpSubjects = "";
            form2.SelectedCoursesJson = JsonSerializer.Serialize(selectedCourses);

            await _db.SaveChangesAsync();

            TempData["Success"] = "Course recommendation sent successfully.";
            return RedirectToAction("CourseRecommendation");
        }



        [HttpPost]
        public async Task<IActionResult> UploadTranscriptForRecommendation(IFormFile transcriptFile)
        {
            if (transcriptFile == null || transcriptFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please upload a transcript file.";
                return RedirectToAction(nameof(CourseRecommendation));
            }

            try
            {
                int studentId = 2210783;

                var student = await _db.Students
                    .Include(s => s.Transcript)
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction(nameof(CourseRecommendation));
                }

                // بدليه لاحقًا إلى student.PlanId إذا صار موجود عندك
                int planId = 1;

                // 1) استخراج من الـ parser العادي
                var parserCourses = await _transcriptParserService.ParseTranscriptAsync(transcriptFile)
                                   ?? new List<TranscriptCourseItem>();

                // 2) استخراج من الـ AI parser
                var aiCourses = await _transcriptAiParserService.ParseTranscriptAsync(transcriptFile)
                               ?? new List<TranscriptCourseItem>();

                // 3) جيبي فقط المواد المعتمدة في الخطة
                var validCourseIds = await _db.Set<StudyPlanCourse>()
                    .Where(x => x.PlanId == planId)
                    .Select(x => x.CourseId)
                    .ToListAsync();

                var validCourseIdSet = validCourseIds
                    .Select(NormalizeCourseId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // 4) دمج وتنظيف النتائج
                var transcriptCourses = MergeTranscriptCourses(
                    parserCourses,
                    aiCourses,
                    validCourseIdSet);

                TempData["ParsedCoursesDebug"] = transcriptCourses.Count == 0
                    ? "No merged courses found."
                    : string.Join(" | ", transcriptCourses.Select(x => $"{x.CourseId}-{x.Grade}-{x.IsPassed}"));

                if (!transcriptCourses.Any())
                {
                    HttpContext.Session.Remove("RecommendedCards");
                    HttpContext.Session.Remove("SelectedRecommendedCourses");

                    TempData["ErrorMessage"] = "No valid courses were extracted from the transcript.";
                    return RedirectToAction(nameof(CourseRecommendation));
                }

                // 5) التوصيات المؤكدة فقط
                var recommendations = await _recommendationEngineService
                    .GenerateRecommendationsAsync(planId, transcriptCourses);

                var cards = (recommendations ?? new List<RecommendedCourseVm>())
                    .OrderBy(x => x.SemesterNo)
                    .ThenBy(x => x.DisplayOrder)
                    .Take(4)
                    .Select(x => new CourseCardVM
                    {
                        CourseId = x.CourseId,
                        CourseName = x.CourseName,
                        Hours = x.Hours,
                        Prerequisite = null,
                        IsCompleted = false,
                        CanTake = true,
                        IsSelected = false,
                        IsDisabled = false,
                        Status = "Recommended"
                    })
                    .ToList();

                // 6) تنظيف القديم وحفظ الجديد
                HttpContext.Session.Remove("RecommendedCards");
                HttpContext.Session.Remove("SelectedRecommendedCourses");

                HttpContext.Session.SetString(
                    "RecommendedCards",
                    JsonSerializer.Serialize(cards));

                HttpContext.Session.SetString(
                    "SelectedRecommendedCourses",
                    JsonSerializer.Serialize(new List<SelectedCourseVM>()));

                if (!cards.Any())
                {
                    TempData["InfoMessage"] = "No confirmed recommendations were found from the transcript.";
                }
                else
                {
                    TempData["Success"] = "Transcript uploaded successfully.";
                }

                return RedirectToAction(nameof(CourseRecommendation));
            }
            catch (Exception ex)
            {
                HttpContext.Session.Remove("RecommendedCards");
                HttpContext.Session.Remove("SelectedRecommendedCourses");

                TempData["ErrorMessage"] = $"An error occurred while processing the transcript: {ex.Message}";
                return RedirectToAction(nameof(CourseRecommendation));
            }
        }



        [HttpPost]
        public IActionResult SaveManualElectiveCourses(
            string freeElectiveCourse1,
            string freeElectiveCourse2,
            string freeElectiveCourse3)
        {
            HttpContext.Session.SetString("FreeElectiveCourse1", freeElectiveCourse1?.Trim() ?? "");
            HttpContext.Session.SetString("FreeElectiveCourse2", freeElectiveCourse2?.Trim() ?? "");
            HttpContext.Session.SetString("FreeElectiveCourse3", freeElectiveCourse3?.Trim() ?? "");

            TempData["Success"] = "Manual elective courses saved.";
            return RedirectToAction(nameof(CourseRecommendation));
        }





        private static List<TranscriptCourseItem> MergeTranscriptCourses(
    List<TranscriptCourseItem> parserCourses,
    List<TranscriptCourseItem> aiCourses,
    HashSet<string> validCourseIds)
        {
            var all = new List<TranscriptCourseItem>();

            if (parserCourses != null)
                all.AddRange(parserCourses);

            if (aiCourses != null)
                all.AddRange(aiCourses);

            var merged = all
                .Where(x => x != null)
                .Where(x => !string.IsNullOrWhiteSpace(x.CourseId))
                .Select(x => new TranscriptCourseItem
                {
                    CourseId = NormalizeCourseId(x.CourseId),
                    Grade = NormalizeGrade(x.Grade),
                    IsPassed = x.IsPassed || IsPassingGrade(x.Grade)
                })
                .Where(x => validCourseIds.Contains(x.CourseId))
                .GroupBy(x => x.CourseId)
                .Select(g =>
                {
                    var passed = g.FirstOrDefault(x => x.IsPassed);
                    if (passed != null)
                        return passed;

                    var withGrade = g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Grade));
                    return withGrade ?? g.First();
                })
                .OrderBy(x => x.CourseId)
                .ToList();

            return merged;
        }

        private static string NormalizeCourseId(string? courseId)
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
                var match = System.Text.RegularExpressions.Regex.Match(courseId, @"^([A-Z]{4})(\d{3})$");
                if (match.Success)
                    return $"{match.Groups[1].Value}-{match.Groups[2].Value}";
            }

            return courseId;
        }

        private static string NormalizeGrade(string? grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
                return string.Empty;

            return grade.Trim().ToUpper().Replace(" ", "");
        }

        private static bool IsPassingGrade(string? grade)
        {
            var normalized = NormalizeGrade(grade);

            return normalized == "A+" ||
                   normalized == "A" ||
                   normalized == "B+" ||
                   normalized == "B" ||
                   normalized == "C+" ||
                   normalized == "C" ||
                   normalized == "D+" ||
                   normalized == "D" ||
                   normalized == "P";
        }



































    }
<<<<<<< HEAD
}
=======
}


>>>>>>> origin_second/rahafgh
