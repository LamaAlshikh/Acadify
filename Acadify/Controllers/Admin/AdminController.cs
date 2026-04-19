using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Acadify.Models.AdminPages;
using Acadify.Models;
using Acadify.Services.AcademicCalendar.Interfaces;

namespace Acadify.Controllers.Admin
{
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly AcadifyDbContext _db;
        private readonly IAcademicCalendarAiExtractor _ai;

        public AdminController(
            IWebHostEnvironment env,
            AcadifyDbContext db,
            IAcademicCalendarAiExtractor ai)
        {
            _env = env;
            _db = db;
            _ai = ai;
        }

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

        [HttpGet]
        public IActionResult AdminPagesController()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAdvisorRequestInline(int requestId, string? advisorId, string? manualAdvisorEmail)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            var request = await _db.AdvisorRequests
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                TempData["RequestError"] = "Request was not found.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            Advisor? advisor = null;

            if (!string.IsNullOrWhiteSpace(advisorId) && advisorId != "manual")
            {
                if (int.TryParse(advisorId, out int parsedAdvisorId))
                {
                    advisor = await _db.Advisors
                        .Include(a => a.User)
                        .FirstOrDefaultAsync(a => a.AdvisorId == parsedAdvisorId);
                }

                if (advisor == null)
                {
                    TempData["RequestError"] = "Selected advisor was not found.";
                    return RedirectToAction(nameof(ManageAdvisorRequests));
                }

                if (advisor.User == null)
                {
                    TempData["RequestError"] = "Advisor user data was not found.";
                    return RedirectToAction(nameof(ManageAdvisorRequests));
                }

                request.Student.AdvisorId = advisor.AdvisorId;
                request.RequestedAdvisorId = advisor.AdvisorId;
                request.RequestedAdvisorEmail = advisor.User.Email;
                request.Status = "Approved";
                request.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                await AddNotificationAsync(
                    senderRole: "Admin",
                    sourceType: "Request",
                    type: "student assigned to advisor",
                    message: $"{request.Student.Name} was assigned to you as a new student.",
                    studentId: request.Student.StudentId,
                    advisorId: advisor.AdvisorId);

                TempData["RequestSuccess"] = "Advisor request updated and approved successfully.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            if (advisorId == "manual")
            {
                if (string.IsNullOrWhiteSpace(manualAdvisorEmail))
                {
                    TempData["RequestError"] = "Please enter advisor email.";
                    return RedirectToAction(nameof(ManageAdvisorRequests));
                }

                string email = manualAdvisorEmail.Trim().ToLower();

                var existingAdvisor = await _db.Advisors
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.User.Email.ToLower() == email);

                if (existingAdvisor != null)
                {
                    request.Student.AdvisorId = existingAdvisor.AdvisorId;
                    request.RequestedAdvisorId = existingAdvisor.AdvisorId;
                    request.RequestedAdvisorEmail = existingAdvisor.User.Email;
                    request.Status = "Approved";
                    request.UpdatedAt = DateTime.Now;

                    await _db.SaveChangesAsync();

                    await AddNotificationAsync(
                        senderRole: "Admin",
                        sourceType: "Request",
                        type: "student assigned to advisor",
                        message: $"{request.Student.Name} was assigned to you as a new student.",
                        studentId: request.Student.StudentId,
                        advisorId: existingAdvisor.AdvisorId);

                    TempData["RequestSuccess"] = "Advisor request updated and approved successfully.";
                    return RedirectToAction(nameof(ManageAdvisorRequests));
                }

                request.Student.AdvisorId = null;
                request.RequestedAdvisorId = null;
                request.RequestedAdvisorEmail = email;
                request.Status = "Updated";
                request.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                TempData["RequestSuccess"] = "Advisor email updated successfully. The request is still waiting for the correct advisor.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            TempData["RequestError"] = "Please select an advisor.";
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }

        // =========================
        // Upload Study Plan
        // =========================
        [HttpGet]
        public IActionResult UploadStudyPlan()
        {
            return View(new UploadStudyPlanModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadStudyPlan(UploadStudyPlanModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.StudyPlanFile == null || model.StudyPlanFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.StudyPlanFile), "File is required.");
                return View(model);
            }

            if (Path.GetExtension(model.StudyPlanFile.FileName).ToLower() != ".pdf")
            {
                ModelState.AddModelError(nameof(model.StudyPlanFile), "Only PDF is allowed.");
                return View(model);
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "admin");
            Directory.CreateDirectory(uploadsFolder);

            var savedName = $"StudyPlan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(uploadsFolder, savedName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                model.StudyPlanFile.CopyTo(stream);
            }

            model.SavedFileName = savedName;
            model.Message = "Study Plan uploaded successfully.";
            return View(model);
        }

        // =========================
        // Upload Academic Calendar
        // =========================
        [HttpGet]
        public IActionResult UploadAcademicCalendar()
        {
            return View(new UploadAcademicCalendarModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAcademicCalendar(UploadAcademicCalendarModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Message = "Please check the required fields.";
                model.IsSuccess = false;
                return View(model);
            }

            if (model.AcademicCalendarFile == null || model.AcademicCalendarFile.Length == 0)
            {
                model.Message = "Please choose a PDF file first.";
                model.IsSuccess = false;
                return View(model);
            }

            var ext = Path.GetExtension(model.AcademicCalendarFile.FileName).ToLower();
            if (ext != ".pdf")
            {
                model.Message = "Only PDF files are allowed.";
                model.IsSuccess = false;
                return View(model);
            }

            try
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "academic-calendar");
                Directory.CreateDirectory(folder);

                var savedFileName = $"{Guid.NewGuid():N}.pdf";
                var savedPath = Path.Combine(folder, savedFileName);

                using (var stream = new FileStream(savedPath, FileMode.Create))
                {
                    await model.AcademicCalendarFile.CopyToAsync(stream);
                }

                var calendar = new AcademicCalendar
                {
                    PdfFile = savedFileName,
                    UploadedAt = DateTime.Now
                };

                _db.AcademicCalendars.Add(calendar);
                await _db.SaveChangesAsync();

                var events = await _ai.ExtractEventsFromPdfAsync(savedPath, calendar.CalendarId);

                if (events != null && events.Count > 0)
                {
                    _db.AcademicCalendarEvents.AddRange(events);
                    await _db.SaveChangesAsync();
                }

                model.Message = "Academic calendar uploaded successfully.";
                model.IsSuccess = true;
            }
            catch (Exception ex)
            {
                model.Message = ex.ToString();
                model.IsSuccess = false;
            }

            return View(model);
        }

        // =========================
        // Manage Advisor Requests
        // =========================
        [HttpGet]
        public async Task<IActionResult> ManageAdvisorRequests()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            var requests = await _db.AdvisorRequests
                .Include(r => r.Student)
                    .ThenInclude(s => s.User)
                .Include(r => r.RequestedAdvisor)
                    .ThenInclude(a => a!.User)
                .Where(r => r.Status == "Pending" || r.Status == "Updated")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var advisors = await _db.Advisors
                .Include(a => a.User)
                .OrderBy(a => a.User.Name)
                .ToListAsync();

            ViewBag.Advisors = advisors;

            var vm = new ManageRequestsVM
            {
                PendingRequests = requests.Select(r => new ManageRequestsVM.RequestRow
                {
                    RequestId = r.RequestId,
                    StudentId = r.StudentId,
                    RequestedAdvisorId = r.RequestedAdvisorId,
                    StudentName = r.Student.Name,
                    UniversityId = r.Student.User.Email,
                    RequestedAdvisorName = r.RequestedAdvisor != null
                        ? r.RequestedAdvisor.User.Name
                        : "Not registered yet",
                    RequestedAdvisorEmail = r.RequestedAdvisor != null
                        ? r.RequestedAdvisor.User.Email
                        : (r.RequestedAdvisorEmail ?? ""),
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAdvisorRequest(int requestId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            var request = await _db.AdvisorRequests
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                TempData["RequestError"] = "Request was not found.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            Advisor? advisor = null;

            if (request.RequestedAdvisorId.HasValue)
            {
                advisor = await _db.Advisors
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.AdvisorId == request.RequestedAdvisorId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.RequestedAdvisorEmail))
            {
                string requestedEmail = request.RequestedAdvisorEmail.Trim().ToLower();

                advisor = await _db.Advisors
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.User.Email.ToLower() == requestedEmail);
            }

            if (advisor == null)
            {
                TempData["RequestError"] = "Advisor was not found. Please update the request first.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            if (advisor.User == null)
            {
                TempData["RequestError"] = "Advisor user data was not found.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            if (request.Student == null)
            {
                TempData["RequestError"] = "Student data was not found.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            request.Student.AdvisorId = advisor.AdvisorId;
            request.RequestedAdvisorId = advisor.AdvisorId;
            request.RequestedAdvisorEmail = advisor.User.Email;
            request.Status = "Approved";
            request.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            await AddNotificationAsync(
                senderRole: "Admin",
                sourceType: "Request",
                type: "student assigned to advisor",
                message: $"{request.Student.Name} was assigned to you as a new student.",
                studentId: request.Student.StudentId,
                advisorId: advisor.AdvisorId);

            TempData["RequestSuccess"] = "Advisor request approved successfully.";
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }

        [HttpGet]
        public async Task<IActionResult> CorrectAdvisorRequest(int requestId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            var request = await _db.Set<AdvisorRequest>()
                .Include(r => r.Student)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
                return RedirectToAction(nameof(ManageAdvisorRequests));

            var advisors = await _db.Advisors
                .Include(a => a.User)
                .OrderBy(a => a.User.Name)
                .ToListAsync();

            ViewBag.RequestId = request.RequestId;
            ViewBag.StudentName = request.Student.Name;
            ViewBag.Advisors = advisors;

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CorrectAdvisorRequest(int requestId, int advisorId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            var request = await _db.AdvisorRequests
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
            {
                TempData["RequestError"] = "Request was not found.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            var advisor = await _db.Advisors
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdvisorId == advisorId);

            if (advisor == null)
            {
                TempData["RequestError"] = "Advisor was not found.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            if (advisor.User == null)
            {
                TempData["RequestError"] = "Advisor user data was not found.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            if (request.Student == null)
            {
                TempData["RequestError"] = "Student data was not found.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            request.Student.AdvisorId = advisor.AdvisorId;
            request.RequestedAdvisorId = advisor.AdvisorId;
            request.RequestedAdvisorEmail = advisor.User.Email;
            request.Status = "Approved";
            request.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            await AddNotificationAsync(
                senderRole: "Admin",
                sourceType: "Request",
                type: "student assigned to advisor",
                message: $"{request.Student.Name} was assigned to you as a new student.",
                studentId: request.Student.StudentId,
                advisorId: advisor.AdvisorId);

            TempData["RequestSuccess"] = "Advisor request updated and approved successfully.";
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAdvisorRequest(int requestId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            var request = await _db.Set<AdvisorRequest>()
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null)
                return RedirectToAction(nameof(ManageAdvisorRequests));

            request.Status = "Rejected";
            request.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["RequestSuccess"] = "Advisor request rejected.";
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }
    }
}