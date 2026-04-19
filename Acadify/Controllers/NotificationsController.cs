using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly AcadifyDbContext _db;

        public NotificationsController(AcadifyDbContext db)
        {
            _db = db;
        }

        public IActionResult Panel()
        {
            return ViewComponent("Notifications");
        }

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

        public async Task AddNotificationAsync(
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
                message: message,
                studentId: studentId);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MeetingStudentToAdvisor(int studentId, string message)
        {
            var advisorId = await _db.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.AdvisorId)
                .FirstOrDefaultAsync();

            if (!advisorId.HasValue)
                return BadRequest("Advisor was not found for this student.");

            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Meeting",
                type: "meeting request",
                message: message,
                advisorId: advisorId.Value,
                studentId: studentId);

            return Ok();
        }

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
                advisorId: advisorId);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> TranscriptUploadedToAdvisorOrAdmin(int studentId, string message)
        {
            await AddNotificationToAdvisorOrAdminsAsync(
                studentId: studentId,
                senderRole: "Student",
                sourceType: "Transcript",
                type: "transcript uploaded",
                message: message);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedFormsToAdvisorOrAdmin(int studentId, string message)
        {
            await AddNotificationToAdvisorOrAdminsAsync(
                studentId: studentId,
                senderRole: "System",
                sourceType: "Form",
                type: "generated form",
                message: message);

            return Ok();
        }

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

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Form2AdvisorActionToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Advisor",
                sourceType: "Form",
                type: "form2 advisor action",
                message: message,
                studentId: studentId);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> StudyPlanUploadedToAdmin(string message)
        {
            await AddNotificationToAllAdminsAsync(
                senderRole: "System",
                sourceType: "StudyPlan",
                type: "study plan uploaded",
                message: message);

            return Ok();
        }
    }
}