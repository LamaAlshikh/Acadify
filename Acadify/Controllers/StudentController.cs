using Acadify.Models;
using Acadify.Models.StudentPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using System.Globalization;
using System.Text;
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
        private async Task AddNotificationAsync(
            string senderRole,
            string sourceType,
            string type,
            string message,
            int? studentId = null,
            int? advisorId = null,
            int? adminId = null)
        {
            _db.Notifications.Add(new Notification
            {
                SenderRole = senderRole,
                SourceType = sourceType,
                Type = type,
                Message = message,
                StudentId = studentId,
                AdvisorId = advisorId,
                AdminId = adminId,
                Date = DateTime.Now,
                IsRead = false
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
            var admins = await _db.Admins.ToListAsync();

            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    SenderRole = senderRole,
                    SourceType = sourceType,
                    Type = type,
                    Message = message,
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    AdminId = admin.AdminId,
                    Date = DateTime.Now,
                    IsRead = false
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task AddGeneratedFormsNotificationsAsync(Student student)
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
                ViewBag.StudentEmail = "";
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
            ViewBag.StudentEmail = HttpContext.Session.GetString("UserEmail") ?? "";
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
        // Select Advisor
        // =======================
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
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
                return RedirectToAction("Login", "Account");

            if (student.AdvisorId.HasValue)
                return RedirectToAction(nameof(StudentHome));

            var latestRequest = await _db.Set<AdvisorRequest>()
                .Include(r => r.RequestedAdvisor)
                    .ThenInclude(a => a!.User)
                .Where(r => r.StudentId == student.StudentId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            var advisorsQuery = _db.Advisors
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                advisorsQuery = advisorsQuery.Where(a =>
                    a.User.Name.ToLower().Contains(search) ||
                    a.User.Email.ToLower().Contains(search) ||
                    (a.Department != null && a.Department.ToLower().Contains(search)));
            }

            var advisors = await advisorsQuery
                .OrderBy(a => a.User.Name)
                .Select(a => new AdvisorCardVM
                {
                    AdvisorId = a.AdvisorId,
                    AdvisorName = a.User.Name,
                    AdvisorEmail = a.User.Email,
                    Department = a.Department
                })
                .ToListAsync();

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
                vm.PendingAdvisorEmail = latestRequest.RequestedAdvisor != null
                    ? latestRequest.RequestedAdvisor.User.Email
                    : latestRequest.RequestedAdvisorEmail;
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
                .Include(a => a.User)
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
                RequestedAdvisorEmail = advisor.User.Email,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _db.Set<AdvisorRequest>().Add(request);
            await _db.SaveChangesAsync();
            await AddNotificationToAllAdminsAsync(
                senderRole: "Student",
                sourceType: "Request",
                type: "advisor selection request",
                message: $"{student.Name} sent an advisor request to {advisor.User.Name}.",
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
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.User.Email.ToLower() == email);

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

        // =======================
        // Student Home Page
        // =======================
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

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
                return NotFound("Student not found.");

            if (!student.AdvisorId.HasValue)
                return RedirectToAction(nameof(SelectAdvisor));

            var graduationStatus = await _db.GraduationStatuses
                .FirstOrDefaultAsync(g => g.StudentId == studentId.Value);

            int totalRequiredHours = 140;
            int remainingHours = graduationStatus?.RemainingHours ?? 140;
            int completedHours = totalRequiredHours - remainingHours;

            if (completedHours < 0)
                completedHours = 0;

            var model = new StudenthomeViewModel
            {
                StudentId = student.StudentId,
                StudentName = GetStringPropertyValue(student, "Name", "StudentName", "FullName"),
                StudentEmail = HttpContext.Session.GetString("UserEmail") ?? "",
                RemainingHours = remainingHours,
                CompletedHours = completedHours,
                TotalRequiredHours = totalRequiredHours,
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

        // =======================
        // Student Chat
        // =======================
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

        // =======================
        // Community Student
        // =======================
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
    }
}