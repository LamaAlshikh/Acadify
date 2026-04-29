<<<<<<< HEAD
<<<<<<< HEAD
﻿using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
=======
﻿using Microsoft.AspNetCore.Mvc;
>>>>>>> origin_second/linaLMversion
=======
﻿using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
>>>>>>> origin_second/لما2

namespace Acadify.Controllers
{
    public class NotificationsController : Controller
    {
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> origin_second/لما2
        private readonly AcadifyDbContext _db;

        public NotificationsController(AcadifyDbContext db)
        {
            _db = db;
        }

<<<<<<< HEAD
=======
        // فقط يفتح لوحة الإشعارات
>>>>>>> origin_second/linaLMversion
=======
>>>>>>> origin_second/لما2
        public IActionResult Panel()
        {
            return ViewComponent("Notifications");
        }
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> origin_second/لما2

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

<<<<<<< HEAD
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
=======
        public async Task AddNotificationAsync(
>>>>>>> origin_second/لما2
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

<<<<<<< HEAD
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
=======
        public async Task AddNotificationToAllAdminsAsync(
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

        public async Task AddNotificationToAdvisorOrAdminsAsync(
            int studentId,
            string senderRole,
            string sourceType,
            string type,
            string message)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return;

            if (student.AdvisorId.HasValue)
            {
                await AddNotificationAsync(
                    senderRole: senderRole,
                    sourceType: sourceType,
                    type: type,
                    message: message,
                    studentId: studentId,
                    advisorId: student.AdvisorId.Value);
            }
            else
            {
                await AddNotificationToAllAdminsAsync(
                    senderRole: senderRole,
                    sourceType: sourceType,
                    type: type,
                    message: message,
                    studentId: studentId);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RecommendationToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Recommendation",
                type: "initial recommendation",
>>>>>>> origin_second/لما2
                message: message,
                studentId: studentId);

            return Ok();
        }

<<<<<<< HEAD
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
=======
        [HttpPost]
        public async Task<IActionResult> MeetingStudentToAdvisor(int studentId, string message)
        {
            var advisorId = await _db.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.AdvisorId)
                .FirstOrDefaultAsync();

            if (!advisorId.HasValue)
                return BadRequest("Advisor was not found for this student.");
>>>>>>> origin_second/لما2

            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Meeting",
                type: "meeting request",
                message: message,
<<<<<<< HEAD
                advisorId: advisorId.Value);
=======
                advisorId: advisorId.Value,
                studentId: studentId);
>>>>>>> origin_second/لما2

            return Ok();
        }

<<<<<<< HEAD
        // المرشد -> الطالبة
=======
>>>>>>> origin_second/لما2
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

<<<<<<< HEAD
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
=======
        [HttpPost]
        public async Task<IActionResult> CommunityStudentToAdvisor(int studentId, string message)
        {
            var advisorId = await _db.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.AdvisorId)
                .FirstOrDefaultAsync();

            if (!advisorId.HasValue)
                return BadRequest("Advisor was not found for this student.");

            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Community",
                type: "community activity",
                message: message,
                advisorId: advisorId.Value,
                studentId: studentId);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> StudentAccountCreatedToAdmins(int studentId, string message)
        {
            await AddNotificationToAllAdminsAsync(
                senderRole: "System",
                sourceType: "Request",
                type: "new student account",
                message: message,
                studentId: studentId);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> StudentAssignedToAdvisor(int studentId, int advisorId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Admin",
                sourceType: "Request",
                type: "student assigned to advisor",
                message: message,
                studentId: studentId,
>>>>>>> origin_second/لما2
                advisorId: advisorId);

            return Ok();
        }

<<<<<<< HEAD
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
=======
        [HttpPost]
        public async Task<IActionResult> TranscriptUploadedToAdvisorOrAdmin(int studentId, string message)
        {
            await AddNotificationToAdvisorOrAdminsAsync(
                studentId: studentId,
                senderRole: "Student",
                sourceType: "Transcript",
                type: "transcript uploaded",
                message: message);
>>>>>>> origin_second/لما2

            return Ok();
        }

<<<<<<< HEAD
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
=======
        [HttpPost]
        public async Task<IActionResult> GeneratedFormsToAdvisorOrAdmin(int studentId, string message)
        {
            await AddNotificationToAdvisorOrAdminsAsync(
                studentId: studentId,
                senderRole: "System",
                sourceType: "Form",
                type: "generated form",
                message: message);
>>>>>>> origin_second/لما2

            return Ok();
        }

<<<<<<< HEAD
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
=======
        [HttpPost]
        public async Task<IActionResult> Form2StudentActionToAdvisor(int studentId, string message)
        {
            var advisorId = await _db.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.AdvisorId)
                .FirstOrDefaultAsync();

            if (!advisorId.HasValue)
                return BadRequest("Advisor was not found for this student.");

            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Form",
                type: "form2 student action",
                message: message,
                advisorId: advisorId.Value,
                studentId: studentId);
>>>>>>> origin_second/لما2

            return Ok();
        }

<<<<<<< HEAD
        // الأدمن -> الطالبة
        [HttpPost]
        public async Task<IActionResult> RequestDecisionToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Admin",
                sourceType: "Request",
                type: "request decision",
=======
        [HttpPost]
        public async Task<IActionResult> Form2AdvisorActionToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Advisor",
                sourceType: "Form",
                type: "form2 advisor action",
>>>>>>> origin_second/لما2
                message: message,
                studentId: studentId);

            return Ok();
        }

<<<<<<< HEAD
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
=======
        [HttpPost]
        public async Task<IActionResult> StudyPlanUploadedToAdmin(string message)
        {
            await AddNotificationToAllAdminsAsync(
                senderRole: "System",
                sourceType: "StudyPlan",
                type: "study plan uploaded",
                message: message);
>>>>>>> origin_second/لما2

            return Ok();
        }
    }
<<<<<<< HEAD
}
=======
    }
}
>>>>>>> origin_second/linaLMversion
=======
}
>>>>>>> origin_second/لما2
