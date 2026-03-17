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
        // =========================[HttpGet]
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
            catch
            {
                model.Message = "An error occurred while uploading the academic calendar.";
                model.IsSuccess = false;
            }

            return View(model);
        }

        // =========================
        // Manage Advisor Requests
        // =========================
        [HttpGet]
        public IActionResult ManageAdvisorRequests()
        {
            var vm = new ManageRequestsVM
            {
                PendingRequests = new List<ManageRequestsVM.RequestRow>
                {
                    new ManageRequestsVM.RequestRow
                    {
                        RequestId = 1,
                        StudentName = "Lama Alshikh",
                        UniversityId = "214000123",
                        RequestedAdvisorName = "Dr. Amina Hassan",
                        RequestedAdvisorEmail = "aminah@kau.edu.sa",
                        Status = "Pending",
                        CreatedAt = DateTime.Now.AddDays(-1)
                    },
                    new ManageRequestsVM.RequestRow
                    {
                        RequestId = 2,
                        StudentName = "Sara Ahmed",
                        UniversityId = "214000456",
                        RequestedAdvisorName = "Dr. Hind Hassan",
                        RequestedAdvisorEmail = "hind@kau.edu.sa",
                        Status = "Pending",
                        CreatedAt = DateTime.Now.AddHours(-6)
                    }
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveAdvisorRequest(int requestId)
        {
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }

        [HttpGet]
        public IActionResult CorrectAdvisorRequest(int requestId)
        {
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectAdvisorRequest(int requestId)
        {
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }
    }
}