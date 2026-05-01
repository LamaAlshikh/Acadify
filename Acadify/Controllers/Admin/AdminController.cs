using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Acadify.Models.AdminPages;
using Acadify.Models;
using Acadify.Services.AcademicCalendar.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        #region Helpers - دوال مساعدة
        private async Task AddNotificationAsync(
            string senderRole, string sourceType, string type, string message,
            int? studentId = null, int? advisorId = null, int? adminId = null)
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

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";
        #endregion

        // الصفحة الرئيسية للمسؤول
        [HttpGet]
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        #region Manage Advisor Requests - إدارة طلبات المرشدين
        [HttpGet]
        public async Task<IActionResult> ManageAdvisorRequests()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var requests = await _db.AdvisorRequests
                .Include(r => r.Student).ThenInclude(s => s.User)
                .Include(r => r.RequestedAdvisor).ThenInclude(a => a!.User)
                .Where(r => r.Status == "Pending" || r.Status == "Updated")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Advisors = await _db.Advisors.Include(a => a.User).OrderBy(a => a.User.Name).ToListAsync();

            var vm = new ManageRequestsVM
            {
                PendingRequests = requests.Select(r => new ManageRequestsVM.RequestRow
                {
                    RequestId = r.RequestId,
                    StudentId = r.StudentId,
                    StudentName = r.Student.Name,
                    UniversityId = r.Student.User.Email,
                    RequestedAdvisorName = r.RequestedAdvisor?.User.Name ?? "غير مسجل حالياً",
                    RequestedAdvisorEmail = r.RequestedAdvisor?.User.Email ?? r.RequestedAdvisorEmail ?? "",
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
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var request = await _db.AdvisorRequests.Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null) return NotFound();

            // منطق البحث عن المرشد وتعيينه
            var advisor = await _db.Advisors.Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AdvisorId == request.RequestedAdvisorId || a.User.Email == request.RequestedAdvisorEmail);

            if (advisor == null)
            {
                TempData["RequestError"] = "المرشد المطلوب غير موجود في النظام حالياً.";
                return RedirectToAction(nameof(ManageAdvisorRequests));
            }

            request.Student.AdvisorId = advisor.AdvisorId;
            request.Status = "Approved";
            request.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            await AddNotificationAsync("Admin", "Request", "assigned", $"تم تعيين الطالب {request.Student.Name} إليك.", advisorId: advisor.AdvisorId);

            TempData["RequestSuccess"] = "تمت الموافقة على الطلب وتعيين المرشد بنجاح.";
            return RedirectToAction(nameof(ManageAdvisorRequests));
        }
        #endregion

        #region Academic Calendar - التقويم الأكاديمي (AI)
        [HttpGet]
        public IActionResult UploadAcademicCalendar() => View(new UploadAcademicCalendarModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAcademicCalendar(UploadAcademicCalendarModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (model.AcademicCalendarFile == null || Path.GetExtension(model.AcademicCalendarFile.FileName).ToLower() != ".pdf")
            {
                model.Message = "يرجى رفع ملف PDF صحيح.";
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

                var calendar = new AcademicCalendar { PdfFile = savedFileName, UploadedAt = DateTime.Now };
                _db.AcademicCalendars.Add(calendar);
                await _db.SaveChangesAsync();

                // استخراج الأحداث باستخدام الذكاء الاصطناعي
                var events = await _ai.ExtractEventsFromPdfAsync(savedPath, calendar.CalendarId);
                if (events?.Any() == true)
                {
                    _db.AcademicCalendarEvents.AddRange(events);
                    await _db.SaveChangesAsync();
                }

                model.IsSuccess = true;
                model.Message = "تم رفع التقويم واستخراج المواعيد بنجاح.";
            }
            catch (Exception ex)
            {
                model.Message = "خطأ أثناء المعالجة: " + ex.Message;
                model.IsSuccess = false;
            }
            return View(model);
        }
        #endregion
    }
}