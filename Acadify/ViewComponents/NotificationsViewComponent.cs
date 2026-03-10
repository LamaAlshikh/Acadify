using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            int? studentId = null;
            int? advisorId = null;
            int? adminId = null;

            var sClaim = HttpContext.User.FindFirst("StudentId")?.Value;
            if (int.TryParse(sClaim, out var sid))
                studentId = sid;

            var aClaim = HttpContext.User.FindFirst("AdvisorId")?.Value;
            if (int.TryParse(aClaim, out var aid))
                advisorId = aid;

            var adminClaim = HttpContext.User.FindFirst("AdminId")?.Value;
            if (int.TryParse(adminClaim, out var admid))
                adminId = admid;

            var query = _db.Notifications.AsNoTracking().AsQueryable();

            if (role == "Student" && studentId.HasValue)
            {
                query = query.Where(n => n.StudentId == studentId);
            }
            else if (role == "Advisor" && advisorId.HasValue)
            {
                query = query.Where(n => n.AdvisorId == advisorId);
            }
            else if (role == "Admin" && adminId.HasValue)
            {
                query = query.Where(n => n.AdminId == adminId);
            }
            else
            {
                query = query.Where(n => false);
            }

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
                TargetUrl = BuildTargetUrl(n.SourceType, role),
                TimeAgo = GetTimeAgo(n.Date),
                SourceType = string.IsNullOrWhiteSpace(n.SourceType) ? "General" : n.SourceType!
            }).ToList();

            var calendarNotifications = await BuildAcademicCalendarNotificationsAsync();

            var all = dbNotifications
                .Concat(calendarNotifications)
                .OrderByDescending(n => n.NotificationDate)
                .ToList();

            return View(all);
        }

        private string GetCurrentRole()
        {
            if (User.IsInRole("Admin")) return "Admin";
            if (User.IsInRole("Advisor")) return "Advisor";
            return "Student";
        }
        private string GetFutureText(int daysLeft)
{
    if (daysLeft == 0) return "Today";
    if (daysLeft == 1) return "Tomorrow";
    return $"After {daysLeft} days";
}

        private async Task<List<NotificationViewModel>> BuildAcademicCalendarNotificationsAsync()
        {
            var result = new List<NotificationViewModel>();

            var latestCalendarId = await _db.AcademicCalendars
                .OrderByDescending(c => c.CalendarId)
                .Select(c => (int?)c.CalendarId)
                .FirstOrDefaultAsync();

            if (!latestCalendarId.HasValue)
                return result;

            var today = DateTime.Today;

            // بداية الحدث قبل يومين
            int beforeStartDays = 2;

            // نهاية الحدث قبل 3 أيام
            int beforeEndDays = 3;

            var events = await _db.AcademicCalendarEvents
                .Where(e => e.CalendarId == latestCalendarId.Value)
                .OrderByDescending(e => e.GregorianDate)
                .ToListAsync();

            foreach (var e in events)
            {
                var eventDate = e.GregorianDate.Date;
                var daysLeft = (eventDate - today).Days;

                // لا نعرض الأحداث التي انتهت
                if (daysLeft < 0)
                    continue;

                var isEndEvent = e.EventName.Contains("نهاية");
                var allowedDays = isEndEvent ? beforeEndDays : beforeStartDays;

                // لا نعرض إلا إذا دخل الحدث في فترة التنبيه
                if (daysLeft > allowedDays)
                    continue;

                string message;

                if (daysLeft == 0)
                {
                    message = $"اليوم {e.EventName}.";
                }
                else
                {
                    message = $"يتبقى {daysLeft} يوم على {e.EventName} بتاريخ {eventDate:dd/MM/yyyy}.";
                }

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
                    TimeAgo = GetFutureText(daysLeft),
                    SourceType = "Calendar"
                });
            }

            return result
                .OrderByDescending(n => n.NotificationDate)
                .ToList();
        }
        private string BuildTitle(string? sourceType)
        {
            return sourceType switch
            {
                "Recommendation" => "Recommendation notification",
                "Meeting" => "Meeting notification",
                "Chat" => "New message",
                "Form" => "Form notification",
                "Transcript" => "Transcript notification",
                "Calendar" => "Academic calendar",
                "Request" => "Request notification",
                "StudyPlan" => "Study plan notification",
                _ => "System notification"
            };
        }

        private string BuildTargetUrl(string? sourceType, string role)
        {
            return sourceType switch
            {
                "Recommendation" => role == "Student" ? "/Student/StudentHome" : "/Forms",
                "Meeting" => "/Meeting",
                "Chat" => "/Community",
                "Form" => "/Forms",
                "Transcript" => "/Student/StudentHome",
                "Calendar" => "/Notifications/Panel",
                "Request" => role == "Admin" ? "/Admin" : "/Student/StudentHome",
                "StudyPlan" => "/Admin",
                _ => "/Notifications/Panel"
            };
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