using Acadify.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Db = Acadify.Models.Db;

namespace Acadify.ViewComponents
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly Db.AcadifyDbContext _db;
        private readonly IConfiguration _configuration;

        public NotificationsViewComponent(Db.AcadifyDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var role = GetCurrentRole();

            int? studentId = GetStudentId();
            int? advisorId = GetAdvisorId();
            int? adminId = GetAdminId();

            await EnsureAdminRequestNotificationsAsync(role, adminId);
            await EnsureAcademicCalendarNotificationsAsync(role, studentId, advisorId);

            var today = DateTime.Today;

            var query = _db.Notifications
                .AsNoTracking()
                .AsQueryable();

            if (role == "Student" && studentId.HasValue)
            {
                query = query.Where(n =>
                    n.StudentId == studentId.Value &&
                    n.SourceType == "Calendar" &&
                    n.Date.Date == today);
            }
            else if (role == "Advisor" && advisorId.HasValue)
            {
                query = query.Where(n =>
                    n.AdvisorId == advisorId.Value &&
                    (
                        n.SourceType == "StudentAssigned" ||
                        (n.SourceType == "Calendar" && n.Date.Date == today)
                    ));
            }
            else if (role == "Admin" && adminId.HasValue)
            {
                var activeRequestKeys = await GetActiveAdminRequestKeysAsync();

                if (activeRequestKeys.Count == 0)
                {
                    query = query.Where(n => false);
                }
                else
                {
                    query = query.Where(n =>
                        n.AdminId == adminId.Value &&
                        n.SourceType == "Request" &&
                        n.Type != null &&
                        activeRequestKeys.Contains(n.Type));
                }
            }
            else
            {
                query = query.Where(n => false);
            }

            var dbNotifs = await query
                .OrderByDescending(n => n.Date)
                .Take(50)
                .ToListAsync();

            var notifications = dbNotifs.Select(n =>
            {
                var title = BuildTitle(n.SourceType);

                return new NotificationViewModel
                {
                    NotificationID = n.NotificationId,
                    NotificationContent = n.Message,
                    NotificationDate = n.Date,
                    NotificationType = string.IsNullOrWhiteSpace(n.SenderRole) ? "System" : n.SenderRole!,
                    Title = title,
                    SenderName = string.IsNullOrWhiteSpace(n.SenderRole) ? "System" : n.SenderRole!,
                    IsRead = n.IsRead,
                    TargetUrl = BuildTargetUrl(n.SourceType, role),
                    TimeAgo = BuildTimeText(n),
                    SourceType = string.IsNullOrWhiteSpace(n.SourceType) ? "General" : n.SourceType!,
                    Initials = BuildInitials(title)
                };
            }).ToList();

            return View(notifications);
        }

        private string GetCurrentRole()
        {
            return HttpContext.Session.GetString("UserRole") ?? "";
        }

        private int? GetStudentId()
        {
            return HttpContext.Session.GetInt32("StudentId");
        }

        private int? GetAdvisorId()
        {
            return HttpContext.Session.GetInt32("AdvisorId");
        }

        private int? GetAdminId()
        {
            return HttpContext.Session.GetInt32("AdminId");
        }

        private async Task EnsureAdminRequestNotificationsAsync(string role, int? adminId)
        {
            if (role != "Admin" || !adminId.HasValue)
                return;

            var requests = await _db.AdvisorRequests
                .Include(r => r.Student)
                .Include(r => r.RequestedAdvisor)
                    .ThenInclude(a => a!.User)
                .Where(r => r.Status == "Pending" || r.Status == "Updated")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            foreach (var request in requests)
            {
                var key = BuildAdminRequestKey(request.RequestId, request.Status);

                bool exists = await _db.Notifications.AnyAsync(n =>
                    n.AdminId == adminId.Value &&
                    n.SourceType == "Request" &&
                    n.Type == key);

                if (exists)
                    continue;

                string studentName = request.Student?.Name ?? "A student";

                string advisorName =
                    request.RequestedAdvisor?.User?.Name ??
                    request.RequestedAdvisorEmail ??
                    "the selected advisor";

                var message = $"Student Request: {studentName} requested {advisorName} as academic advisor.";

                var notification = new Db.Notification
                {
                    SenderRole = "Student",
                    SourceType = "Request",
                    Type = key,
                    Message = message,
                    AdminId = adminId.Value,
                    Date = request.CreatedAt,
                    IsRead = false
                };

                var adminEmail = await GetAdminEmailAsync(adminId.Value);

                await AddNotificationWithEmailAsync(notification, adminEmail);
            }
        }

        private async Task<List<string>> GetActiveAdminRequestKeysAsync()
        {
            return await _db.AdvisorRequests
                .Where(r => r.Status == "Pending" || r.Status == "Updated")
                .Select(r => "student-request-" + r.RequestId + "-" + r.Status)
                .ToListAsync();
        }

        private static string BuildAdminRequestKey(int requestId, string? status)
        {
            return $"student-request-{requestId}-{status ?? "Pending"}";
        }

        private async Task EnsureAcademicCalendarNotificationsAsync(string role, int? studentId, int? advisorId)
        {
            if (role != "Student" && role != "Advisor")
                return;

            if (role == "Student" && !studentId.HasValue)
                return;

            if (role == "Advisor" && !advisorId.HasValue)
                return;

            var latestCalendarId = await _db.AcademicCalendars
                .OrderByDescending(c => c.CalendarId)
                .Select(c => (int?)c.CalendarId)
                .FirstOrDefaultAsync();

            if (!latestCalendarId.HasValue)
                return;

            var today = DateTime.Today;
            var fromDate = today.AddDays(-1);
            var toDate = today.AddDays(3);

            var events = await _db.AcademicCalendarEvents
                .Where(e =>
                    e.CalendarId == latestCalendarId.Value &&
                    e.GregorianDate.Date >= fromDate &&
                    e.GregorianDate.Date <= toDate)
                .OrderBy(e => e.GregorianDate)
                .ToListAsync();

            foreach (var calendarEvent in events)
            {
                var eventDate = calendarEvent.GregorianDate.Date;
                var daysToEvent = (eventDate - today).Days;

                if (daysToEvent != 3 && daysToEvent != 0 && daysToEvent != -1)
                    continue;

                string reminderCode = daysToEvent switch
                {
                    3 => "Before3Days",
                    0 => "EventDay",
                    -1 => "After1Day",
                    _ => "General"
                };

                string notificationType = $"calendar-{calendarEvent.Id}-{reminderCode}";

                bool exists;

                if (role == "Student")
                {
                    exists = await _db.Notifications.AnyAsync(n =>
                        n.StudentId == studentId.Value &&
                        n.SourceType == "Calendar" &&
                        n.Type == notificationType);
                }
                else
                {
                    exists = await _db.Notifications.AnyAsync(n =>
                        n.AdvisorId == advisorId.Value &&
                        n.SourceType == "Calendar" &&
                        n.Type == notificationType);
                }

                if (exists)
                    continue;

                string message = BuildCalendarMessage(calendarEvent.EventName, eventDate, daysToEvent);

                var notification = new Db.Notification
                {
                    SenderRole = "System",
                    SourceType = "Calendar",
                    Type = notificationType,
                    Message = message,
                    StudentId = role == "Student" ? studentId : null,
                    AdvisorId = role == "Advisor" ? advisorId : null,
                    Date = DateTime.Now,
                    IsRead = false
                };

                var email = await GetCurrentUserEmailAsync(role, studentId, advisorId);

                await AddNotificationWithEmailAsync(notification, email);
            }
        }

        private static string BuildCalendarMessage(string eventName, DateTime eventDate, int daysToEvent)
        {
            return daysToEvent switch
            {
                3 => $"تذكير: يتبقى 3 أيام على {eventName}. تاريخ الحدث: {eventDate:dd/MM/yyyy}.",
                0 => $"تذكير: {eventName} اليوم.",
                -1 => $"تذكير: كان موعد {eventName} بالأمس. تاريخ الحدث: {eventDate:dd/MM/yyyy}.",
                _ => $"تذكير: {eventName}. تاريخ الحدث: {eventDate:dd/MM/yyyy}."
            };
        }

        private async Task AddNotificationWithEmailAsync(Db.Notification notification, string? recipientEmail)
        {
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await SendNotificationEmailAsync(
                recipientEmail,
                $"Acadify Notification - {BuildTitle(notification.SourceType)}",
                notification.Message);
        }

        private async Task<string?> GetCurrentUserEmailAsync(string role, int? studentId, int? advisorId)
        {
            if (role == "Student" && studentId.HasValue)
            {
                return await _db.Students
                    .Where(s => s.StudentId == studentId.Value)
                    .Select(s => s.User.Email)
                    .FirstOrDefaultAsync();
            }

            if (role == "Advisor" && advisorId.HasValue)
            {
                return await _db.Advisors
                    .Where(a => a.AdvisorId == advisorId.Value)
                    .Select(a => a.User.Email)
                    .FirstOrDefaultAsync();
            }

            return null;
        }

        private async Task<string?> GetAdminEmailAsync(int adminId)
        {
            return await _db.Admins
                .Where(a => a.AdminId == adminId)
                .Select(a => a.User.Email)
                .FirstOrDefaultAsync();
        }

        private async Task SendNotificationEmailAsync(string? toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var host = _configuration["Smtp:Host"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? username;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                return;
            }

            int port = 587;
            int.TryParse(_configuration["Smtp:Port"], out port);

            bool enableSsl = true;
            bool.TryParse(_configuration["Smtp:EnableSsl"], out enableSsl);

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password)
                };

                using var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                await client.SendMailAsync(message);
            }
            catch
            {
                // Email failure should not stop the notification display.
            }
        }

        private static string BuildTitle(string? sourceType)
        {
            return sourceType switch
            {
                "Request" => "Student Request",
                "StudentAssigned" => "Student Assigned",
                "Calendar" => "Academic Calendar",
                _ => "System Notification"
            };
        }

        private static string BuildTargetUrl(string? sourceType, string role)
        {
            return sourceType switch
            {
                "Request" when role == "Admin" => "/Admin/ManageAdvisorRequests",
                "Calendar" => "#",
                "StudentAssigned" => "#",
                _ => "#"
            };
        }

        private static string BuildTimeText(Db.Notification notification)
        {
            if (notification.SourceType == "Calendar")
            {
                if (!string.IsNullOrWhiteSpace(notification.Type))
                {
                    if (notification.Type.Contains("Before3Days"))
                        return "In 3 days";

                    if (notification.Type.Contains("EventDay"))
                        return "Today";

                    if (notification.Type.Contains("After1Day"))
                        return "Yesterday";
                }
            }

            return GetTimeAgo(notification.Date);
        }

        private static string GetTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;

            if (span.TotalMinutes < 1)
                return "Just now";

            if (span.TotalHours < 1)
                return $"{(int)span.TotalMinutes} min ago";

            if (span.TotalDays < 1)
                return $"{(int)span.TotalHours} hours ago";

            if (span.TotalDays < 2)
                return "Yesterday";

            return $"{(int)span.TotalDays} days ago";
        }

        private static string BuildInitials(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "SY";

            var words = Regex
                .Split(title.Trim(), @"\s+")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Take(2)
                .ToList();

            if (words.Count == 0)
                return "SY";

            return string.Join("", words.Select(w => char.ToUpper(w[0])));
        }
    }
}