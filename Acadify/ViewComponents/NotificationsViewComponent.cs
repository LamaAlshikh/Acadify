using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Acadify.ViewComponents
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly AcadifyDbContext _db;

        public NotificationsViewComponent(AcadifyDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var role = GetCurrentRole();
            int? studentId = GetStudentId();
            int? advisorId = GetAdvisorId();
            int? adminId = GetAdminId();

            var query = _db.Notifications.AsNoTracking().AsQueryable();

            // تصفية الإشعارات بناءً على هوية المستخدم ودوره
            if (role == "Student" && studentId.HasValue)
                query = query.Where(n => n.StudentId == studentId.Value);
            else if (role == "Advisor" && advisorId.HasValue)
                query = query.Where(n => n.AdvisorId == advisorId.Value);
            else if (role == "Admin" && adminId.HasValue)
                query = query.Where(n => n.AdminId == adminId.Value);
            else
                query = query.Where(n => false); // لا توجد نتائج إذا لم يتطابق الدور

            var dbNotifs = await query
                .OrderByDescending(n => n.Date)
                .Take(50)
                .ToListAsync();

            var dbNotifications = dbNotifs.Select(n => new NotificationViewModel
            {
                NotificationID = n.NotificationId,
                NotificationContent = n.Message,
                NotificationDate = n.Date,
                NotificationType = string.IsNullOrWhiteSpace(n.SenderRole) ? "System" : n.SenderRole!,
                Title = BuildTitle(n.SourceType),
                SenderName = string.IsNullOrWhiteSpace(n.SenderRole) ? "System" : n.SenderRole!,
                IsRead = n.IsRead,
                TargetUrl = BuildTargetUrl(n.SourceType, role, n),
                TimeAgo = GetTimeAgo(n.Date),
                SourceType = string.IsNullOrWhiteSpace(n.SourceType) ? "General" : n.SourceType!
            }).ToList();

            // دمج إشعارات التقويم الأكاديمي
            var calendarNotifications = await BuildAcademicCalendarNotificationsAsync(role);
            var allNotifications = dbNotifications
                .Concat(calendarNotifications)
                .OrderByDescending(n => n.NotificationDate)
                .ToList();

            return View(allNotifications);
        }

        // --- دالات المساعدة لجلب البيانات من الـ Session ---
        private string GetCurrentRole() => HttpContext.Session.GetString("UserRole") ?? "";
        private int? GetStudentId() => HttpContext.Session.GetInt32("StudentId");
        private int? GetAdvisorId() => HttpContext.Session.GetInt32("AdvisorId");
        private int? GetAdminId() => HttpContext.Session.GetInt32("AdminId");

        private async Task<List<NotificationViewModel>> BuildAcademicCalendarNotificationsAsync(string role)
        {
            var result = new List<NotificationViewModel>();
            if (role != "Student" && role != "Advisor") return result;

            var latestCalendarId = await _db.AcademicCalendars
                .OrderByDescending(c => c.CalendarId)
                .Select(c => (int?)c.CalendarId)
                .FirstOrDefaultAsync();

            if (!latestCalendarId.HasValue) return result;

            var today = DateTime.Today;
            var events = await _db.AcademicCalendarEvents
                .Where(e => e.CalendarId == latestCalendarId.Value)
                .OrderBy(e => e.GregorianDate)
                .ToListAsync();

            foreach (var e in events)
            {
                var eventDate = e.GregorianDate.Date;
                var daysLeft = (eventDate - today).Days;

                if (daysLeft < 0 || daysLeft > 2) continue;

                string message = daysLeft switch
                {
                    2 => $"يتبقى يومان على {e.EventName} بتاريخ {eventDate:dd/MM/yyyy}.",
                    1 => $"غدًا {e.EventName} بتاريخ {eventDate:dd/MM/yyyy}.",
                    _ => $"اليوم {e.EventName}."
                };

                result.Add(new NotificationViewModel
                {
                    NotificationID = 0,
                    NotificationContent = message,
                    NotificationDate = eventDate,
                    NotificationType = "System",
                    Title = "Academic calendar",
                    SenderName = "System",
                    IsRead = false,
                    TargetUrl = "/Notifications/Panel",
                    TimeAgo = daysLeft == 0 ? "Today" : (daysLeft == 1 ? "Tomorrow" : "After 2 days"),
                    SourceType = "Calendar"
                });
            }
            return result;
        }

        private string BuildTitle(string? sourceType) => sourceType switch
        {
            "Recommendation" => "Recommendation notification",
            "Meeting" => "Meeting notification",
            "Chat" or "Community" => "New message",
            "Form" => "Form notification",
            "Transcript" => "Transcript notification",
            "Calendar" => "Academic calendar",
            "Request" => "Request notification",
            "StudyPlan" => "Study plan notification",
            _ => "System notification"
        };

        private string BuildTargetUrl(string? sourceType, string role, Notification n) => sourceType switch
        {
            "Recommendation" => role == "Student" ? "/Student/StudentHome" : "/Advisor/AdvisorHome",
            "Meeting" => role == "Advisor" ? "/Advisor/Meetings" : "/Student/Meeting",
            "Chat" or "Community" => role == "Advisor" ? "/Advisor/CommunityAdvisor" : "/Student/CommunityStudent",
            "Form" => BuildFormTargetUrl(role, n),
            "Transcript" => role == "Advisor" ? "/Advisor/AdvisorHome" : (role == "Admin" ? "/Admin/ManageAdvisorRequests" : "/Student/StudentHome"),
            "Calendar" or "Request" => "/Notifications/Panel",
            "StudyPlan" => "/Admin/UploadStudyPlan",
            _ => "/Notifications/Panel"
        };

        private string BuildFormTargetUrl(string role, Notification n)
        {
            if (role == "Admin") return "/Admin/ManageAdvisorRequests";
            if (role == "Advisor" && n.StudentId.HasValue)
                return $"/Advisor/StudentForms?studentId={n.StudentId.Value}";
            return "/Student/StudentHome";
        }

        private string GetTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} hours ago";
            if (span.TotalDays < 2) return "Yesterday";
            return $"{(int)span.TotalDays} days ago";
        }
    }
}