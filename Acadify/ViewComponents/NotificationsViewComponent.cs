using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
<<<<<<< HEAD
using Microsoft.EntityFrameworkCore;
=======
using System;
using System.Collections.Generic;
using System.Linq;
>>>>>>> origin_second/linaLMversion
=======
using Microsoft.EntityFrameworkCore;
>>>>>>> origin_second/لما2

namespace Acadify.ViewComponents
{
    public class NotificationsViewComponent : ViewComponent
    {
<<<<<<< HEAD
<<<<<<< HEAD
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

            if (role == "Student" && studentId.HasValue)
            {
                query = query.Where(n => n.StudentId == studentId.Value);
            }
            else if (role == "Advisor" && advisorId.HasValue)
            {
                query = query.Where(n => n.AdvisorId == advisorId.Value);
            }
            else if (role == "Admin" && adminId.HasValue)
            {
                query = query.Where(n => n.AdminId == adminId.Value);
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

            var calendarNotifications = await BuildAcademicCalendarNotificationsAsync(role);

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

        private int? GetStudentId()
        {
            var claim = HttpContext.User.FindFirst("StudentId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private int? GetAdvisorId()
        {
            var claim = HttpContext.User.FindFirst("AdvisorId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private int? GetAdminId()
        {
            var claim = HttpContext.User.FindFirst("AdminId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private string GetFutureText(int daysLeft)
        {
            if (daysLeft == 0) return "Today";
            if (daysLeft == 1) return "Tomorrow";
            return $"After {daysLeft} days";
        }

        private async Task<List<NotificationViewModel>> BuildAcademicCalendarNotificationsAsync(string role)
        {
            var result = new List<NotificationViewModel>();

            // إشعارات الكالندر تظهر فقط للطالبة والمرشد
            if (role != "Student" && role != "Advisor")
                return result;

            var latestCalendarId = await _db.AcademicCalendars
                .OrderByDescending(c => c.CalendarId)
                .Select(c => (int?)c.CalendarId)
                .FirstOrDefaultAsync();

            if (!latestCalendarId.HasValue)
                return result;

            var today = DateTime.Today;

            var events = await _db.AcademicCalendarEvents
                .Where(e => e.CalendarId == latestCalendarId.Value)
                .OrderBy(e => e.GregorianDate)
                .ToListAsync();

            foreach (var e in events)
            {
                var eventDate = e.GregorianDate.Date;
                var daysLeft = (eventDate - today).Days;

                // فقط: قبل يومين، قبل يوم، ويوم الحدث نفسه
                if (daysLeft != 2 && daysLeft != 1 && daysLeft != 0)
                    continue;

                string message;
                string timeText;

                if (daysLeft == 2)
                {
                    message = $"يتبقى يومان على {e.EventName} بتاريخ {eventDate:dd/MM/yyyy}.";
                    timeText = "After 2 days";
                }
                else if (daysLeft == 1)
                {
                    message = $"غدًا {e.EventName} بتاريخ {eventDate:dd/MM/yyyy}.";
                    timeText = "Tomorrow";
                }
                else
                {
                    message = $"اليوم {e.EventName}.";
                    timeText = "Today";
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
                    TimeAgo = timeText,
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
                "Request" => role == "Admin" ? "/Admin/ManageAdvisorRequests" : "/Student/StudentHome",
                "StudyPlan" => "/Admin/UploadStudyPlan",
                _ => "/Notifications/Panel"
=======
        public IViewComponentResult Invoke()
=======
        private readonly AcadifyDbContext _db;

        public NotificationsViewComponent(AcadifyDbContext db)
>>>>>>> origin_second/لما2
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

            if (role == "Student" && studentId.HasValue)
            {
                query = query.Where(n => n.StudentId == studentId.Value);
            }
            else if (role == "Advisor" && advisorId.HasValue)
            {
                query = query.Where(n => n.AdvisorId == advisorId.Value);
            }
            else if (role == "Admin" && adminId.HasValue)
            {
                query = query.Where(n => n.AdminId == adminId.Value);
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
                TargetUrl = BuildTargetUrl(n.SourceType, role, n),
                TimeAgo = GetTimeAgo(n.Date),
                SourceType = string.IsNullOrWhiteSpace(n.SourceType) ? "General" : n.SourceType!
            }).ToList();

            var calendarNotifications = await BuildAcademicCalendarNotificationsAsync(role);

            var all = dbNotifications
                .Concat(calendarNotifications)
                .OrderByDescending(n => n.NotificationDate)
                .ToList();

            return View(all);
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

        private async Task<List<NotificationViewModel>> BuildAcademicCalendarNotificationsAsync(string role)
        {
            var result = new List<NotificationViewModel>();

            if (role != "Student" && role != "Advisor")
                return result;

            var latestCalendarId = await _db.AcademicCalendars
                .OrderByDescending(c => c.CalendarId)
                .Select(c => (int?)c.CalendarId)
                .FirstOrDefaultAsync();

            if (!latestCalendarId.HasValue)
                return result;

            var today = DateTime.Today;

            var events = await _db.AcademicCalendarEvents
                .Where(e => e.CalendarId == latestCalendarId.Value)
                .OrderBy(e => e.GregorianDate)
                .ToListAsync();

            foreach (var e in events)
            {
                var eventDate = e.GregorianDate.Date;
                var daysLeft = (eventDate - today).Days;

                if (daysLeft != 2 && daysLeft != 1 && daysLeft != 0)
                    continue;

                string message;
                string timeText;

                if (daysLeft == 2)
                {
                    message = $"يتبقى يومان على {e.EventName} بتاريخ {eventDate:dd/MM/yyyy}.";
                    timeText = "After 2 days";
                }
<<<<<<< HEAD
>>>>>>> origin_second/linaLMversion
=======
                else if (daysLeft == 1)
                {
                    message = $"غدًا {e.EventName} بتاريخ {eventDate:dd/MM/yyyy}.";
                    timeText = "Tomorrow";
                }
                else
                {
                    message = $"اليوم {e.EventName}.";
                    timeText = "Today";
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
                    TimeAgo = timeText,
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
                "Community" => "Community notification",
                _ => "System notification"
>>>>>>> origin_second/لما2
            };
        }

        private string BuildTargetUrl(string? sourceType, string role, Notification notification)
        {
            return sourceType switch
            {
                "Recommendation" => role == "Student" ? "/Student/StudentHome" : "/Advisor/AdvisorHome",

                "Meeting" => role == "Advisor"
                    ? "/Advisor/Meetings"
                    : "/Student/Meeting",

                "Chat" => role == "Advisor"
                    ? "/Advisor/CommunityAdvisor"
                    : "/Student/CommunityStudent",

                "Community" => role == "Advisor"
                    ? "/Advisor/CommunityAdvisor"
                    : "/Student/CommunityStudent",

                "Form" => BuildFormTargetUrl(role, notification),

                "Transcript" => role == "Advisor"
                    ? "/Advisor/AdvisorHome"
                    : role == "Admin"
                        ? "/Admin/ManageAdvisorRequests"
                        : "/Student/StudentHome",

                "Calendar" => "/Notifications/Panel",

                "Request" => "/Notifications/Panel",

                "StudyPlan" => "/Admin/UploadStudyPlan",

                _ => "/Notifications/Panel"
            };
        }

        private string BuildFormTargetUrl(string role, Notification notification)
        {
            if (role == "Admin")
                return "/Admin/ManageAdvisorRequests";

            if (role == "Advisor")
            {
                if (notification.StudentId.HasValue)
                    return $"/Advisor/StudentForms?studentId={notification.StudentId.Value}";

                return "/Advisor/AdvisorHome";
            }

            return "/Student/StudentHome";
        }

        private string GetTimeAgo(DateTime date)
        {
            var span = DateTime.Now - date;

            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} hours ago";
            if (span.TotalDays < 2) return "Yesterday";
<<<<<<< HEAD
<<<<<<< HEAD
            return $"{(int)span.TotalDays} days ago";
        }
    }
}
=======

            return $"{(int)span.TotalDays} days ago";
        }
    }
}
>>>>>>> origin_second/linaLMversion
=======
            return $"{(int)span.TotalDays} days ago";
        }
    }
}
>>>>>>> origin_second/لما2
