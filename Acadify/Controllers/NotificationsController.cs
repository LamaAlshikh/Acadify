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
        public async Task<IActionResult> RecommendationToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Recommendation",
                type: "system recommendation",
                message: message,
                studentId: studentId);

            return Ok();
        }

        // الطالبة -> المرشد (قبول / رفض / تعديل)
        [HttpPost]
        public async Task<IActionResult> RecommendationStudentActionToAdvisor(int advisorId, int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Recommendation",
                type: "student recommendation action",
                message: message,
                advisorId: advisorId);

            return Ok();
        }

        // المرشد -> الطالبة (رفض / تعديل / قرار)
        [HttpPost]
        public async Task<IActionResult> RecommendationAdvisorActionToStudent(int studentId, int advisorId, string message)
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
        public async Task<IActionResult> MeetingStudentToAdvisor(int advisorId, int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Meeting",
                type: "meeting request",
                message: message,
                advisorId: advisorId);

            return Ok();
        }

        // المرشد -> الطالبة
        [HttpPost]
        public async Task<IActionResult> MeetingAdvisorToStudent(int studentId, int advisorId, string message)
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

        // إشعار محادثة يظهر للطالبة وللمرشد
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

        // النظام -> المرشد عند اكتمال الفورم
        [HttpPost]
        public async Task<IActionResult> FormCompletedToAdvisor(int advisorId, string message)
        {
            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Form",
                type: "form completed",
                message: message,
                advisorId: advisorId);

            return Ok();
        }

        // =========================
        // TRANSCRIPT
        // =========================

        // النظام -> الطالبة
        [HttpPost]
        public async Task<IActionResult> TranscriptUploadedToStudent(int studentId, string message)
        {
            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Transcript",
                type: "transcript uploaded",
                message: message,
                studentId: studentId);

            return Ok();
        }

        // =========================
        // CALENDAR
        // =========================

        // النظام -> الأدمن عند رفع الكالندر
        [HttpPost]
        public async Task<IActionResult> CalendarUploadedToAdmin(int adminId, string message)
        {
            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "Calendar",
                type: "calendar uploaded",
                message: message,
                adminId: adminId);

            return Ok();
        }

        // =========================
        // REQUEST
        // =========================

        // الطالبة -> الأدمن عند اختيار المرشدة
        [HttpPost]
        public async Task<IActionResult> RequestToAdmin(int studentId, int adminId, string message)
        {
            await AddNotificationAsync(
                senderRole: "Student",
                sourceType: "Request",
                type: "advisor request",
                message: message,
                adminId: adminId);

            return Ok();
        }

        // الأدمن -> الطالبة بقرار الطلب
        [HttpPost]
        public async Task<IActionResult> RequestDecisionToStudent(int studentId, int adminId, string message)
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

        // النظام -> الأدمن عند رفع الخطة
        [HttpPost]
        public async Task<IActionResult> StudyPlanUploadedToAdmin(int adminId, string message)
        {
            await AddNotificationAsync(
                senderRole: "System",
                sourceType: "StudyPlan",
                type: "study plan uploaded",
                message: message,
                adminId: adminId);

            return Ok();
        }

        // =========================
        // GENERAL SYSTEM
        // =========================

        [HttpPost]
        public async Task<IActionResult> AddSystem(
            string sourceType,
            string type,
            string message,
            int? studentId,
            int? advisorId,
            int? adminId)
        {
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