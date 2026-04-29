<<<<<<< HEAD
﻿using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
=======
﻿using Microsoft.AspNetCore.Mvc;
>>>>>>> origin_second/linaLMversion

namespace Acadify.Controllers
{
    public class NotificationsController : Controller
    {
<<<<<<< HEAD
        private readonly AcadifyDbContext _db;

        public NotificationsController(AcadifyDbContext db)
        {
            _db = db;
        }

=======
        // فقط يفتح لوحة الإشعارات
>>>>>>> origin_second/linaLMversion
        public IActionResult Panel()
        {
            return ViewComponent("Notifications");
        }
<<<<<<< HEAD

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notif = await _db.Notifications.FindAsync(id);
            if (notif == null)
                return NotFound();

            notif.IsRead = true;
            await _db.SaveChangesAsync();

            return Ok();
        }

        private int? GetCurrentStudentId()
        {
            return HttpContext.Session.GetInt32("StudentId");
        }

        private int? GetCurrentAdvisorId()
        {
            return HttpContext.Session.GetInt32("AdvisorId");
        }

        private int? GetCurrentAdminId()
        {
            return HttpContext.Session.GetInt32("AdminId");
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

        // =========================
        // RECOMMENDATION
        // =========================

        // النظام -> الطالبة
        [HttpPost]
        public async Task<IActionResult> RecommendationToStudent(string message)
        {
            var studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return BadRequest("Student session not found.");

            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Recommendation",
                type: "system recommendation",
                message: message,
                studentId: studentId.Value);

            return Ok();
        }

        // الطالبة -> المرشد
        [HttpPost]
        public async Task<IActionResult> RecommendationStudentActionToAdvisor(string message)
        {
            var advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return BadRequest("Advisor session not found.");

            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Recommendation",
                type: "student recommendation action",
                message: message,
                advisorId: advisorId.Value);

            return Ok();
        }

        // المرشد -> الطالبة
        [HttpPost]
        public async Task<IActionResult> RecommendationAdvisorActionToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Advisor",
                sourceType: "Recommendation",
                type: "advisor recommendation action",
                message: message,
                studentId: studentId);

            return Ok();
        }

        // =========================
        // MEETING
        // =========================

        // الطالبة -> المرشد
        [HttpPost]
        public async Task<IActionResult> MeetingStudentToAdvisor(string message)
        {
            var advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return BadRequest("Advisor session not found.");

            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Meeting",
                type: "meeting request",
                message: message,
                advisorId: advisorId.Value);

            return Ok();
        }

        // المرشد -> الطالبة
        [HttpPost]
        public async Task<IActionResult> MeetingAdvisorToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Advisor",
                sourceType: "Meeting",
                type: "meeting response",
                message: message,
                studentId: studentId);

            return Ok();
        }

        // =========================
        // CHAT
        // =========================

        [HttpPost]
        public async Task<IActionResult> ChatToStudentAndAdvisor(int studentId, int advisorId, string senderRole, string message)
        {
            await AddNotificationAsync(
                senderRole: senderRole,
                sourceType: "Chat",
                type: "new chat",
                message: message,
                studentId: studentId);

            await AddNotificationAsync(
                senderRole: senderRole,
                sourceType: "Chat",
                type: "new chat",
                message: message,
                advisorId: advisorId);

            return Ok();
        }

        // =========================
        // FORM
        // =========================

        [HttpPost]
        public async Task<IActionResult> FormCompletedToAdvisor(string message)
        {
            var advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return BadRequest("Advisor session not found.");

            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Form",
                type: "form completed",
                message: message,
                advisorId: advisorId.Value);

            return Ok();
        }

        // =========================
        // TRANSCRIPT
        // =========================

        [HttpPost]
        public async Task<IActionResult> TranscriptUploadedToStudent(string message)
        {
            var studentId = GetCurrentStudentId();
            if (!studentId.HasValue)
                return BadRequest("Student session not found.");

            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Transcript",
                type: "transcript uploaded",
                message: message,
                studentId: studentId.Value);

            return Ok();
        }

        // =========================
        // CALENDAR
        // =========================

        [HttpPost]
        public async Task<IActionResult> CalendarUploadedToAdmin(string message)
        {
            var adminId = GetCurrentAdminId();
            if (!adminId.HasValue)
                return BadRequest("Admin session not found.");

            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Calendar",
                type: "calendar uploaded",
                message: message,
                adminId: adminId.Value);

            return Ok();
        }

        // =========================
        // REQUEST
        // =========================

        // الطالبة -> الأدمن
        [HttpPost]
        public async Task<IActionResult> RequestToAdmin(string message)
        {
            var adminId = GetCurrentAdminId();
            if (!adminId.HasValue)
                return BadRequest("Admin session not found.");

            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Request",
                type: "advisor request",
                message: message,
                adminId: adminId.Value);

            return Ok();
        }

        // الأدمن -> الطالبة
        [HttpPost]
        public async Task<IActionResult> RequestDecisionToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Admin",
                sourceType: "Request",
                type: "request decision",
                message: message,
                studentId: studentId);

            return Ok();
        }

        // =========================
        // STUDY PLAN
        // =========================

        [HttpPost]
        public async Task<IActionResult> StudyPlanUploadedToAdmin(string message)
        {
            var adminId = GetCurrentAdminId();
            if (!adminId.HasValue)
                return BadRequest("Admin session not found.");

            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "StudyPlan",
                type: "study plan uploaded",
                message: message,
                adminId: adminId.Value);

            return Ok();
        }

        // =========================
        // GENERAL SYSTEM
        // =========================

        [HttpPost]
        public async Task<IActionResult> AddSystem(
            string sourceType,
            string type,
            string message)
        {
            var studentId = GetCurrentStudentId();
            var advisorId = GetCurrentAdvisorId();
            var adminId = GetCurrentAdminId();

            await AddNotificationAsync(
                senderRole: "System",
                sourceType: sourceType,
                type: type,
                message: message,
                studentId: studentId,
                advisorId: advisorId,
                adminId: adminId);

            return Ok();
        }
    }
}
=======
    }
}
>>>>>>> origin_second/linaLMversion
