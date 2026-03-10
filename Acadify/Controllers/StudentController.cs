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



        private readonly AcadifyDbContext _db;
        private readonly IWebHostEnvironment _env;

        public StudentController(AcadifyDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
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

            // Normalize spaces
            var normalized = Regex.Replace(text, @"\s+", " ").Trim();

            // ✅ Cumulative GPA (0-5.xx فقط عشان ما يلقط 131)
            // يدعم: "Cumulative GPA: 4.89" وأيضًا "4.89Cumulative GPA"
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

            // ✅ Latest Term GPA (نأخذ آخر رقم GPA يظهر بعد كلمة Term)
            // إذا ما ضبطت مع ملفات ثانية، نعدله بسهولة
            var termMatches = Regex.Matches(
                normalized,
                @"\bTerm\b.*?\b([0-5]\.\d{2})\b",
                RegexOptions.IgnoreCase);

            if (termMatches.Count > 0)
                result.LatestTermGpa = TryDec(termMatches[^1].Groups[1].Value);

            // ✅ Course codes (ACCT333, ARAB101, CPIS320, IS321 ... إلخ)
            // يسمح 2-6 حروف + 3-4 أرقام
            var allowedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "CPIS","IS","CPCS","CPIT",
    "ACCT","BUS","MRKT","ARAB",
    "STAT","ELIS","MATH","PHYS",
    "CHEM","BIO","ASTR","COMM","ISLS"
};

            foreach (Match m in Regex.Matches(normalized, @"\b([A-Z]{2,6})\s?(\d{3,4})\b"))
            {
                var prefix = m.Groups[1].Value.ToUpperInvariant();
                var number = m.Groups[2].Value;

                if (allowedPrefixes.Contains(prefix))
                {
                    result.CourseCodes.Add(prefix + number);
                }
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
            var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CPIS","IS","CPCS","CPIT","BUS","ACCT","MRKT","ARAB","STAT","ELIS","MATH","PHYS","CHEM","BIO","ASTR","COMM","ISLS"
    };

            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var doc = UglyToad.PdfPig.PdfDocument.Open(fullPath);

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

                    // CPIS320
                    foreach (var t in tokens)
                    {
                        var mm = Regex.Match(t, @"^([A-Z]{2,6})(\d{3,4})$");
                        if (mm.Success && prefixes.Contains(mm.Groups[1].Value))
                            results.Add((mm.Groups[1].Value + mm.Groups[2].Value).ToUpperInvariant());
                    }

                    // CPIS 320
                    for (int i = 0; i < tokens.Count - 1; i++)
                    {
                        var p = tokens[i].ToUpperInvariant();
                        var n = tokens[i + 1];

                        if (prefixes.Contains(p) && Regex.IsMatch(n, @"^\d{3,4}$"))
                        {
                            results.Add(p + n);
                        }
                    }
                }
            }

            return results.OrderBy(x => x).ToList();
        }










        // Student Home Page
        public IActionResult StudentHome()
        {

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

            // 4) Parse GPA فقط من النص
            var (cumulativeGpa, latestTermGpa) = ParseGpaFromTranscriptText(extractedText);

            // 5) Extract course codes من PDF layout (أفضل للجداول)
            var courseCodes = ExtractCourseCodesFromPdf(fullPath); // List<string>
            var courseCodesSet = courseCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // ✅ Debug سريع (عشان نعرف ليش فاضي لو صار)
            ViewBag.CodesCount = courseCodesSet.Count;
            ViewBag.CodesPreview = string.Join(", ", courseCodesSet.Take(30));

            // 6) TEMP studentId (مؤقت الآن)
            int studentId = await _db.Students.Select(s => s.StudentId).FirstAsync();

            // 7) UPSERT transcript + include Courses (عشان الربط)
            var transcript = await _db.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            if (transcript == null)
            {
                transcript = new Transcript { StudentId = studentId };
                _db.Transcripts.Add(transcript);

                // مهم: عشان ينولد transcriptID لو احتجناه لاحقاً
                await _db.SaveChangesAsync();

                // تأكدي collection جاهزة
                _db.Entry(transcript).Collection(t => t.Courses).Load();
            }

            // 8) Update fields
            transcript.PdfFile = $"/uploads/transcripts/{fileName}";
            transcript.ExtractedInfo = extractedText;

            // GPA (لا نكتب إلا لو منطقي)
            if (cumulativeGpa.HasValue) transcript.Gpa = cumulativeGpa.Value;
            if (latestTermGpa.HasValue) transcript.SemesterGpa = latestTermGpa.Value;

            // نخزن الأكواد كنص (اختياري)
            transcript.ExtractedCourses = courseCodesSet.Count == 0
                ? null
                : string.Join(", ", courseCodesSet);

            // 9) Fill TranscriptCourse (Many-to-Many)
            transcript.Courses.Clear();

            if (courseCodesSet.Count > 0)
            {
                // جيبي الموجودين
                var coursesInDb = await _db.Courses
                    .Where(c => courseCodesSet.Contains(c.CourseId))
                    .ToListAsync();

                // ضيفي المفقود كـ placeholder (عشان CourseName NOT NULL عندك)
                var existingIds = coursesInDb.Select(c => c.CourseId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missingIds = courseCodesSet.Where(id => !existingIds.Contains(id)).ToList();

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

            ViewBag.Success = $"Uploaded successfully for StudentId = {studentId} (updated if already existed).";
            ViewBag.DebugPreview = extractedText.Length > 250 ? extractedText.Substring(0, 250) : extractedText;

            return View();
        }




        // =======================
        // Student Chat (No JS)
        // =======================

        // GET: Student/Chat
        [HttpGet]
        public IActionResult Chat()
        {
            // مؤقت: بيانات تجريبية (بعدها تربطينها بالداتابيس)
            var model = new StudentChatViewModel
            {
                AdvisorName = "DR. Amina Hasan Gamlo",
                StudentName = "Lama Alshikh",
                IsRecordingStarted = true, // <-- تظهر جملة التسجيل
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

        // POST: Student/SendMessage
        [HttpPost]
        public IActionResult SendMessage(string message)
        {
            // لاحقاً: نحفظ الرسالة في DB + نخلي Agent يقرأها
            // حالياً: بس نرجع لصفحة الشات
            return RedirectToAction("Chat");
        }





    }
}
