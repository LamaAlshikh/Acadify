using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Acadify.Models;
using Db = Acadify.Models.Db;
using Acadify.Models.StudentPages;
using Acadify.Services;

using UglyToad.PdfPig;

namespace Acadify.Controllers
{
    public class StudentController : Controller
    {
        private readonly Db.AcadifyDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ITranscriptParserService _transcriptParserService;
        private readonly IRecommendationEngineService _recommendationEngineService;
        private readonly ITranscriptAiParserService _transcriptAiParserService;

        public StudentController(
            Db.AcadifyDbContext db,
            IWebHostEnvironment env,
            ITranscriptParserService transcriptParserService,
            IRecommendationEngineService recommendationEngineService,
            ITranscriptAiParserService transcriptAiParserService)
        {
            _db = db;
            _env = env;
            _transcriptParserService = transcriptParserService;
            _recommendationEngineService = recommendationEngineService;
            _transcriptAiParserService = transcriptAiParserService;
        }

        // =========================
        // Notification helpers
        // =========================
        private async Task AddNotificationAsync(
            string senderRole,
            string sourceType,
            string type,
            string message,
            int? studentId = null,
            int? advisorId = null,
            int? adminId = null)
        {
            _db.Notifications.Add(new Db.Notification
            {
                Type = type,
                Message = message,
                StudentId = studentId,
                AdvisorId = advisorId,
                Date = DateTime.Now
            });

            await _db.SaveChangesAsync();
        }

        private async Task AddNotificationToAllAdminsAsync(
            string senderRole,
            string sourceType,
            string type,
            string message,
            int? studentId = null,
            int? advisorId = null)
        {
            _db.Notifications.Add(new Db.Notification
            {
                Type = type,
                Message = message,
                StudentId = studentId,
                AdvisorId = advisorId,
                Date = DateTime.Now
            });

            await _db.SaveChangesAsync();
        }

        private async Task AddGeneratedFormsNotificationsAsync(Db.Student student)
        {
            var messages = new List<string>
            {
                "Academic Advising Confirmation form is generated from the transcript.",
                "Next Semester Course Selection form is generated from the transcript.",
                "Meeting Record Form is generated from the transcript.",
                "Study Plan Matching form is generated from the transcript.",
                "Graduation Project Eligibility form is generated from the transcript."
            };

            foreach (var msg in messages)
            {
                if (student.AdvisorId.HasValue)
                {
                    await AddNotificationAsync(
                        senderRole: "System",
                        sourceType: "Form",
                        type: "generated form",
                        message: $"{student.Name}: {msg}",
                        studentId: student.StudentId,
                        advisorId: student.AdvisorId.Value);
                }
                else
                {
                    await AddNotificationToAllAdminsAsync(
                        senderRole: "System",
                        sourceType: "Form",
                        type: "generated form",
                        message: $"{student.Name}: {msg}",
                        studentId: student.StudentId);
                }
            }
        }

        // =========================
        // General helpers
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

        private async Task LoadStudentSidebarDataAsync()
        {
            int? studentId = GetCurrentStudentId();

            if (!studentId.HasValue)
            {
                ViewBag.StudentName = "Student";
                ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail") ?? "";
                return;
            }

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                ViewBag.StudentName = "Student";
                ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail") ?? "";
                return;
            }

            ViewBag.StudentName = GetStringPropertyValue(student, "Name", "StudentName", "FullName");
            ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail")
                ?? GetStringPropertyValue(student, "Email", "StudentEmail", "UniversityEmail");
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

        // =========================
        // Transcript PDF helpers
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

            if (!cumMatch.Success)
            {
                cumMatch = Regex.Match(
                    normalized,
                    @"\b\d+\s+([0-5]\.\d{2})\s*Cumulative\s*Total",
                    RegexOptions.IgnoreCase);
            }

            if (cumMatch.Success)
                result.CumulativeGpa = TryDec(cumMatch.Groups[1].Value);

            var termMatches = Regex.Matches(
                normalized,
                @"\bTerm\b.*?\b([0-5]\.\d{2})\b",
                RegexOptions.IgnoreCase);

            if (termMatches.Count > 0)
            {
                result.LatestTermGpa = TryDec(termMatches[^1].Groups[1].Value);
            }
            else
            {
                var tail = normalized.Length > 2500 ? normalized[^2500..] : normalized;
                var nums = Regex.Matches(tail, @"\b[0-5]\.\d{2}\b")
                    .Select(x => TryDec(x.Value))
                    .Where(x => x.HasValue && x != result.CumulativeGpa)
                    .Select(x => x!.Value)
                    .ToList();

                if (nums.Any())
                    result.LatestTermGpa = nums.Max();
            }

            var blockedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FALL", "SPRING", "SUMMER", "WINTER", "TERM", "PAGE"
            };

            var courseRegex = new Regex(@"(?<![A-Z0-9])([A-Z]{2,6})\s*[-]?\s*(\d{3,4})(?!\d)");

            foreach (Match m in courseRegex.Matches(normalized))
            {
                var prefix = m.Groups[1].Value.ToUpperInvariant();
                if (blockedPrefixes.Contains(prefix))
                    continue;

                result.CourseCodes.Add(prefix + m.Groups[2].Value);
            }

            return result;
        }

        private static (decimal? cumulativeGpa, decimal? lastTermGpa) ParseGpaFromTranscriptText(string text)
        {
            var parsed = ParseTranscriptText(text);
            return (parsed.CumulativeGpa, parsed.LatestTermGpa);
        }

        private static decimal? TryDec(string s)
        {
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                return d;

            return null;
        }

        private static List<string> ExtractCourseCodesFromPdf(string fullPath)
        {
            var blockedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FALL", "SPRING", "SUMMER", "WINTER", "TERM", "PAGE"
            };

            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var doc = PdfDocument.Open(fullPath);

            foreach (var page in doc.GetPages())
            {
                var words = page.GetWords().ToList();

                foreach (var word in words)
                {
                    var text = word.Text.Trim().ToUpperInvariant();
                    var m = Regex.Match(text, @"^([A-Z]{2,6})[-]?(\d{3,4})$");
                    if (m.Success)
                    {
                        var prefix = m.Groups[1].Value;
                        if (!blockedPrefixes.Contains(prefix))
                            results.Add(prefix + m.Groups[2].Value);
                    }
                }

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
                        if (Regex.IsMatch(tokens[i], @"^[A-Z]{2,6}$") &&
                            Regex.IsMatch(tokens[i + 1], @"^\d{3,4}$"))
                        {
                            if (!blockedPrefixes.Contains(tokens[i]))
                                results.Add(tokens[i] + tokens[i + 1]);
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
                if (normalized.Contains(item.Key))
                    results.Add(item.Value);
            }

            return results.OrderBy(x => x).ToList();
        }

        private static Dictionary<string, int> ExtractCourseHoursMapFromPdf(string fullPath, List<string>? targetCourseIds)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using var doc = PdfDocument.Open(fullPath);

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
                        if (targetCourseIds != null && !targetCourseIds.Contains(courseId))
                            continue;

                        if (result.ContainsKey(courseId))
                            continue;

                        if (TryFindCourseOnLine(tokens, courseId, out var codeIndex) &&
                            TryExtractHourNearCourse(tokens, codeIndex, out var hours))
                        {
                            result[courseId] = hours;
                        }
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
                    lines.Add(new List<UglyToad.PdfPig.Content.Word> { word });
                else
                    existingLine.Add(word);
            }

            return lines;
        }

        private static List<string> ExtractCourseIdsFromTokens(List<string> tokens)
        {
            var result = new List<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                var m = Regex.Match(tokens[i], @"^([A-Z]{2,6})[-]?(\d{3,4})$");
                if (m.Success)
                {
                    result.Add(m.Groups[1].Value + m.Groups[2].Value);
                    continue;
                }

                if (i < tokens.Count - 1 &&
                    Regex.IsMatch(tokens[i], @"^[A-Z]{2,6}$") &&
                    Regex.IsMatch(tokens[i + 1], @"^\d{3,4}$"))
                {
                    result.Add(tokens[i] + tokens[i + 1]);
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
                var m = Regex.Match(tokens[i], @"^([A-Z]{2,6})[-]?(\d{3,4})$");
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
                    Regex.IsMatch(tokens[i], @"^[A-Z]{2,6}$") &&
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

        // =========================
        // Student Home
        // =========================
        [HttpGet]
        public async Task<IActionResult> StudentHome()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Student Home";
            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (student == null)
                return NotFound("Student not found.");

            if (!student.AdvisorId.HasValue)
                return RedirectToAction(nameof(SelectAdvisor));

            var graduationStatus = await _db.GraduationStatuses
                .FirstOrDefaultAsync(g => g.StudentId == studentId.Value);

            int totalRequiredHours = 140;
            int remainingHours = graduationStatus?.RemainingHours ?? totalRequiredHours;
            int completedHours = Math.Max(0, totalRequiredHours - remainingHours);

            var model = new StudentHomeViewModel
            {
                StudentId = student.StudentId,
                StudentName = GetStringPropertyValue(student, "Name", "StudentName", "FullName"),
                StudentEmail = HttpContext.Session.GetString("UserEmail")
                    ?? GetStringPropertyValue(student, "Email", "StudentEmail", "UniversityEmail"),
                RemainingHours = remainingHours,
                CompletedHours = completedHours,
                TotalRequiredHours = totalRequiredHours,
                ProgressPercentage = CalculateProgressPercentage(remainingHours, totalRequiredHours),
                CurrentStatus = CalculateCurrentStatus(remainingHours)
            };

            return View(model);
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

        // =========================
        // Select Advisor
        // =========================
     
        [HttpGet]
        public async Task<IActionResult> SelectAdvisor(string? search)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Select Advisor";
            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
                return RedirectToAction("Login", "Account");

            if (student.AdvisorId.HasValue)
                return RedirectToAction(nameof(StudentHome));

            var latestRequest = await _db.Set<AdvisorRequest>()
                .Include(r => r.RequestedAdvisor)
                .Where(r => r.StudentId == student.StudentId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            var advisorsFromDb = await _db.Advisors
                .OrderBy(a => a.AdvisorId)
                .ToListAsync();

            var advisors = advisorsFromDb
                .Select(a => new AdvisorCardVM
                {
                    AdvisorId = a.AdvisorId,
                    AdvisorName = GetStringPropertyValue(a, "Name", "AdvisorName", "FullName"),
                    AdvisorEmail = GetStringPropertyValue(a, "Email", "AdvisorEmail", "UniversityEmail"),
                    Department = GetStringPropertyValue(a, "Department")
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                advisors = advisors
                    .Where(a =>
                        (!string.IsNullOrWhiteSpace(a.AdvisorName) && a.AdvisorName.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(a.AdvisorEmail) && a.AdvisorEmail.ToLower().Contains(search)) ||
                        (!string.IsNullOrWhiteSpace(a.Department) && a.Department.ToLower().Contains(search)))
                    .ToList();
            }

            var vm = new SelectAdvisorVM
            {
                StudentName = student.Name,
                Advisors = advisors,
                SearchTerm = search ?? string.Empty
            };

            if (latestRequest != null && latestRequest.Status == "Pending")
            {
                vm.HasPendingRequest = true;
                vm.PendingStatus = latestRequest.Status;
                vm.PendingAdvisorEmail = latestRequest.RequestedAdvisorEmail;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAdvisorSelection(int advisorId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (student == null)
                return RedirectToAction("Login", "Account");

            if (student.AdvisorId.HasValue)
                return RedirectToAction(nameof(StudentHome));

            var advisor = await _db.Advisors
                .Include(a => a.AdvisorNavigation)
                .FirstOrDefaultAsync(a => a.AdvisorId == advisorId);

            if (advisor == null)
            {
                TempData["AdvisorError"] = "Selected advisor was not found.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            var hasPending = await _db.Set<AdvisorRequest>()
                .AnyAsync(r => r.StudentId == student.StudentId && r.Status == "Pending");

            if (hasPending)
            {
                TempData["AdvisorError"] = "You already have a pending request.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            var request = new AdvisorRequest
            {
                StudentId = student.StudentId,
                RequestedAdvisorId = advisor.AdvisorId,
                RequestedAdvisorEmail = advisor.AdvisorNavigation.Email,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _db.Set<AdvisorRequest>().Add(request);
            await _db.SaveChangesAsync();

            await AddNotificationToAllAdminsAsync(
                senderRole: "Student",
                sourceType: "Request",
                type: "advisor selection request",
                message: $"{student.Name} sent an advisor request to {advisor.AdvisorNavigation.Name}.",
                studentId: student.StudentId);

            TempData["AdvisorSuccess"] = "Your advisor request has been sent to the admin.";
            return RedirectToAction(nameof(SelectAdvisor));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitManualAdvisorEmail(SelectAdvisorVM model)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
            if (student == null)
                return RedirectToAction("Login", "Account");

            if (student.AdvisorId.HasValue)
                return RedirectToAction(nameof(StudentHome));

            if (string.IsNullOrWhiteSpace(model.ManualAdvisorEmail))
            {
                TempData["AdvisorError"] = "Please enter the advisor email.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            var hasPending = await _db.Set<AdvisorRequest>()
                .AnyAsync(r => r.StudentId == student.StudentId && r.Status == "Pending");

            if (hasPending)
            {
                TempData["AdvisorError"] = "You already have a pending request.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            string email = model.ManualAdvisorEmail.Trim().ToLower();

            var advisor = await _db.Advisors
                .Include(a => a.AdvisorNavigation)
                .FirstOrDefaultAsync(a => a.AdvisorNavigation.Email.ToLower() == email);

            var request = new AdvisorRequest
            {
                StudentId = student.StudentId,
                RequestedAdvisorId = advisor?.AdvisorId,
                RequestedAdvisorEmail = email,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _db.Set<AdvisorRequest>().Add(request);
            await _db.SaveChangesAsync();

            await AddNotificationToAllAdminsAsync(
                senderRole: "Student",
                sourceType: "Request",
                type: "manual advisor request",
                message: $"{student.Name} sent a manual advisor request for {email}.",
                studentId: student.StudentId);

            TempData["AdvisorSuccess"] = "Your request has been sent to the admin for review.";
            return RedirectToAction(nameof(SelectAdvisor));
        }

        // =========================
        // Upload Transcript
        // =========================
        [HttpGet]
        public async Task<IActionResult> UploadTranscript()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Upload Transcript";
            await LoadStudentSidebarDataAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTranscript(IFormFile transcriptFile)
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

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

            int? studentIdSession = GetCurrentStudentId();
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

            var parsedTranscript = ParseTranscriptText(extractedText);
            var (cumulativeGpa, latestTermGpa) = ParseGpaFromTranscriptText(extractedText);

            var pdfCourseCodes = ExtractCourseCodesFromPdf(fullPath);
            var titleBasedCodes = ExtractCourseCodesByKnownTitles(extractedText);

            var courseCodesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var code in parsedTranscript.CourseCodes.Concat(pdfCourseCodes).Concat(titleBasedCodes))
            {
                var cleanCode = code?.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(cleanCode))
                    courseCodesSet.Add(cleanCode);
            }

            courseCodesSet.RemoveWhere(code =>
                string.IsNullOrWhiteSpace(code) ||
                code.StartsWith("FALL", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("SPRING", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("SUMMER", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("WINTER", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("TERM", StringComparison.OrdinalIgnoreCase) ||
                code.StartsWith("PAGE", StringComparison.OrdinalIgnoreCase));

            var transcriptHourMap = ExtractCourseHoursMapFromPdf(fullPath, courseCodesSet.ToList());

            ViewBag.DebugStudentId = studentId;
            ViewBag.DebugOriginalFileName = transcriptFile.FileName;
            ViewBag.CodesCount = courseCodesSet.Count;
            ViewBag.CodesPreview = string.Join(", ", courseCodesSet.OrderBy(x => x));
            ViewBag.DebugPreview = extractedText.Length > 1000 ? extractedText[..1000] : extractedText;

            var transcript = await _db.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == studentId);

            if (transcript == null)
            {
                transcript = new Db.Transcript { StudentId = studentId };
                _db.Transcripts.Add(transcript);
                await _db.SaveChangesAsync();
                await _db.Entry(transcript).Collection(t => t.Courses).LoadAsync();
            }

            transcript.PdfFile = $"/uploads/transcripts/{savedFileName}";
            transcript.ExtractedInfo = extractedText;

            if (cumulativeGpa.HasValue)
                transcript.Gpa = cumulativeGpa.Value;
            else if (parsedTranscript.CumulativeGpa.HasValue)
                transcript.Gpa = parsedTranscript.CumulativeGpa.Value;

            if (latestTermGpa.HasValue)
                transcript.SemesterGpa = latestTermGpa.Value;
            else if (parsedTranscript.LatestTermGpa.HasValue)
                transcript.SemesterGpa = parsedTranscript.LatestTermGpa.Value;

            transcript.ExtractedCourses = courseCodesSet.Count == 0
                ? null
                : string.Join(", ", courseCodesSet.OrderBy(x => x));

            transcript.Courses.Clear();

            if (courseCodesSet.Count > 0)
            {
                var coursesInDb = await _db.Courses
                    .Where(c => courseCodesSet.Contains(c.CourseId.Trim().ToUpper()))
                    .ToListAsync();

                foreach (var course in coursesInDb)
                {
                    var normalizedCourseId = course.CourseId.Trim().ToUpperInvariant();
                    bool isUnclassified = string.IsNullOrWhiteSpace(course.RequirementCategory);
                    bool hasZeroHours = course.Hours <= 0;

                    if ((isUnclassified || hasZeroHours) &&
                        transcriptHourMap.TryGetValue(normalizedCourseId, out var extractedHours) &&
                        extractedHours > 0)
                    {
                        course.Hours = extractedHours;
                    }
                }

                var existingIds = coursesInDb
                    .Select(c => c.CourseId.Trim().ToUpperInvariant())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missingIds = courseCodesSet
                    .Where(id => !existingIds.Contains(id))
                    .ToList();

                foreach (var id in missingIds)
                {
                    var newCourse = new Db.Course
                    {
                        CourseId = id,
                        CourseName = id,
                        Hours = transcriptHourMap.TryGetValue(id, out var extractedHours) ? extractedHours : 0
                    };

                    _db.Courses.Add(newCourse);
                    coursesInDb.Add(newCourse);
                }

                foreach (var course in coursesInDb)
                    transcript.Courses.Add(course);
            }

            await _db.SaveChangesAsync();

            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Recommendation",
                type: "initial recommendation",
                message: "Your initial recommendation is ready after transcript upload.",
                studentId: student.StudentId);

            if (student.AdvisorId.HasValue)
            {
                await AddNotificationAsync(
                    senderRole: "Student",
                    sourceType: "Transcript",
                    type: "transcript uploaded",
                    message: $"{student.Name} uploaded the transcript.",
                    studentId: student.StudentId,
                    advisorId: student.AdvisorId.Value);
            }
            else
            {
                await AddNotificationToAllAdminsAsync(
                    senderRole: "Student",
                    sourceType: "Transcript",
                    type: "transcript uploaded",
                    message: $"{student.Name} uploaded the transcript and is waiting for advisor assignment.",
                    studentId: student.StudentId);
            }

            await AddGeneratedFormsNotificationsAsync(student);

            if (!student.AdvisorId.HasValue)
                return RedirectToAction(nameof(SelectAdvisor));

            return RedirectToAction(nameof(StudentHome));
        }

        // =========================
        // Student Chat
        // =========================
        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Chat";
            await LoadStudentSidebarDataAsync();

            var model = new StudentChatViewModel
            {
                AdvisorName = "DR. Amina Hasan Gamlo",
                StudentName = ViewBag.StudentName ?? "Student",
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
            return RedirectToAction(nameof(Chat));
        }

        // =========================
        // Community Student
        // =========================
        [HttpGet]
        public async Task<IActionResult> CommunityStudent()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

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
                    new CommunityMemberVM { Name = "DR.Amina Gamlo", ImagePath = "~/images/user.png" },
                    new CommunityMemberVM { Name = "Lina Alrwaily", ImagePath = "~/images/user.png" },
                    new CommunityMemberVM { Name = "Rahaf Alghamdi", ImagePath = "~/images/user.png" },
                    new CommunityMemberVM { Name = "Rahaf Alzahrani", ImagePath = "~/images/user.png" }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult SendStudentMessage([FromBody] SendStudentMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { success = false, message = "Message is empty." });

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

        // =========================
        // Course Recommendation
        // =========================
        [HttpGet]
        public async Task<IActionResult> CourseRecommendation()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login", "Account");

            await LoadStudentSidebarDataAsync();

            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
                return NotFound("Student not found.");

            ViewBag.StudentName = student.Name;
            ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail") ?? "student@kau.edu.sa";
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

            var model = new CourseRecommendationViewModel
            {
                StudentName = student.Name,
                StudentId = student.StudentId,
                Gpa = transcript?.Gpa,
                SemesterGpa = transcript?.SemesterGpa,
                Selected = selected,
                Cards = cards,
                FreeElectiveCourse1 = HttpContext.Session.GetString("FreeElectiveCourse1") ?? "",
                FreeElectiveCourse2 = HttpContext.Session.GetString("FreeElectiveCourse2") ?? "",
                FreeElectiveCourse3 = HttpContext.Session.GetString("FreeElectiveCourse3") ?? ""
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult SelectRecommendedCourse(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return RedirectToAction(nameof(CourseRecommendation));

            var course = _db.Courses.FirstOrDefault(c => c.CourseId == courseId);
            if (course == null)
                return RedirectToAction(nameof(CourseRecommendation));

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
            return RedirectToAction(nameof(CourseRecommendation));
        }

        [HttpPost]
        public IActionResult RemoveRecommendedCourse(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return RedirectToAction(nameof(CourseRecommendation));

            var selectedJson = HttpContext.Session.GetString("SelectedRecommendedCourses");
            var selected = string.IsNullOrEmpty(selectedJson)
                ? new List<SelectedCourseVM>()
                : JsonSerializer.Deserialize<List<SelectedCourseVM>>(selectedJson) ?? new List<SelectedCourseVM>();

            selected = selected.Where(c => c.CourseId != courseId).ToList();
            HttpContext.Session.SetString("SelectedRecommendedCourses", JsonSerializer.Serialize(selected));

            return RedirectToAction(nameof(CourseRecommendation));
        }

        [HttpPost]
        public IActionResult SwapRecommendedCourse(string courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return RedirectToAction(nameof(CourseRecommendation));

            var cardsJson = HttpContext.Session.GetString("RecommendedCards");
            var cards = string.IsNullOrEmpty(cardsJson)
                ? new List<CourseCardVM>()
                : JsonSerializer.Deserialize<List<CourseCardVM>>(cardsJson) ?? new List<CourseCardVM>();

            if (!cards.Any())
                return RedirectToAction(nameof(CourseRecommendation));

            var currentCard = cards.FirstOrDefault(c => c.CourseId == courseId);
            if (currentCard == null)
                return RedirectToAction(nameof(CourseRecommendation));

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

            return RedirectToAction(nameof(CourseRecommendation));
        }

        [HttpPost]
        public async Task<IActionResult> SendCourseRecommendationToAdvisor()
        {
            int? studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return RedirectToAction("Login", "Account");

            var student = await _db.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found.";
                return RedirectToAction(nameof(CourseRecommendation));
            }

            int? advisorId = student.AdvisorId;
            if (!advisorId.HasValue)
            {
                TempData["ErrorMessage"] = "No advisor is assigned to this student.";
                return RedirectToAction(nameof(SelectAdvisor));
            }

            var selectedJson = HttpContext.Session.GetString("SelectedRecommendedCourses");
            var selectedCourses = string.IsNullOrEmpty(selectedJson)
                ? new List<SelectedCourseVM>()
                : JsonSerializer.Deserialize<List<SelectedCourseVM>>(selectedJson) ?? new List<SelectedCourseVM>();

            int advisedHours = selectedCourses.Sum(x => x.Hours);
            string extractedInfo = student.Transcript?.ExtractedInfo ?? string.Empty;
            string currentSemester = ExtractLastAcademicTerm(extractedInfo);
            int earnedHours = student.Transcript?.Courses?.Sum(c => c.Hours) ?? 0;

            var latestForm = await _db.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == student.StudentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            Db.Form form;
            Db.CourseChoiceMonitoringForm form2;

            if (latestForm == null)
            {
                form = new Db.Form
                {
                    StudentId = student.StudentId,
                    AdvisorId = advisorId.Value,
                    FormType = "Form 2",
                    FormDate = DateTime.Now,
                    FormStatus = "Sent",
                    AdvisorNotes = null,
                    AutoFilled = true,
                    AdvisorConfirmation = null
                };

                _db.Forms.Add(form);
                await _db.SaveChangesAsync();

                form2 = new Db.CourseChoiceMonitoringForm
                {
                    FormId = form.FormId
                };

                _db.CourseChoiceMonitoringForms.Add(form2);
            }
            else
            {
                form = latestForm;
                form2 = latestForm.CourseChoiceMonitoringForm ?? new Db.CourseChoiceMonitoringForm
                {
                    FormId = form.FormId
                };

                if (latestForm.CourseChoiceMonitoringForm == null)
                    _db.CourseChoiceMonitoringForms.Add(form2);
            }

            form.FormDate = DateTime.Now;
            form.FormStatus = "Sent";

            form2.Semester = currentSemester;
            form2.ComingSemester = GetNextSemester(currentSemester);
            form2.RunningCreditHours = earnedHours;
            form2.AdvisedCreditHours = advisedHours;
            form2.Level = CalculateLevelFromHours(earnedHours);
            form2.DropSubjects = ExtractDropSubjects(extractedInfo);
            form2.ICSubjects = ExtractICSubjects(extractedInfo);
            form2.IpSubjects = ExtractIPSubjects(extractedInfo);
            form2.SelectedCoursesJson = JsonSerializer.Serialize(selectedCourses);

            await _db.SaveChangesAsync();

            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Recommendation",
                type: "course recommendation sent",
                message: $"{student.Name} sent course recommendations for advisor review.",
                studentId: student.StudentId,
                advisorId: advisorId.Value);

            TempData["Success"] = "Course recommendation sent successfully.";
            return RedirectToAction(nameof(CourseRecommendation));
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
                int? studentId = GetCurrentStudentId();
                if (!studentId.HasValue)
                    return RedirectToAction("Login", "Account");

                var student = await _db.Students
                    .Include(s => s.Transcript)
                    .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction(nameof(CourseRecommendation));
                }

                int planId = 1;

                var parserCourses = await _transcriptParserService.ParseTranscriptAsync(transcriptFile)
                    ?? new List<TranscriptCourseItem>();

                var aiCourses = await _transcriptAiParserService.ParseTranscriptAsync(transcriptFile)
                    ?? new List<TranscriptCourseItem>();

                var validCourseIds = await _db.Set<Db.StudyPlanCourse>()
                    .Where(x => x.PlanId == planId)
                    .Select(x => x.CourseId)
                    .ToListAsync();

                var validCourseIdSet = validCourseIds
                    .Select(NormalizeCourseId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

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

                HttpContext.Session.Remove("RecommendedCards");
                HttpContext.Session.Remove("SelectedRecommendedCourses");

                HttpContext.Session.SetString("RecommendedCards", JsonSerializer.Serialize(cards));
                HttpContext.Session.SetString("SelectedRecommendedCourses", JsonSerializer.Serialize(new List<SelectedCourseVM>()));

                TempData[cards.Any() ? "Success" : "InfoMessage"] = cards.Any()
                    ? "Transcript uploaded successfully."
                    : "No confirmed recommendations were found from the transcript.";

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

            return matches[matches.Count - 1].Value.ToUpperInvariant();
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

            courseId = courseId.Trim().ToUpperInvariant();
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

        private static string NormalizeGrade(string? grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
                return string.Empty;

            return grade.Trim().ToUpperInvariant().Replace(" ", "");
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

        private async Task<int> CreateNewForm5ForStudentAsync(int studentId)
        {
            var advisorId = await GetAdvisorIdForStudentAsync(studentId);

            if (!advisorId.HasValue || advisorId.Value <= 0)
                throw new InvalidOperationException("No advisor is assigned to this student.");

            var newForm5 = new Db.Form
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

            var details = new Db.GraduationProjectEligibilityForm
            {
                FormId = newForm5.FormId,
                Eligibility = null,
                RequiredCoursesStatus = null
            };

            _db.GraduationProjectEligibilityForms.Add(details);
            await _db.SaveChangesAsync();

            return newForm5.FormId;
        }
    }
}
