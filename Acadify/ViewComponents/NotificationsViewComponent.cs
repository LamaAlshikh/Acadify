using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acadify.ViewComponents
{
    public class NotificationsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var role = User.IsInRole("Advisor") ? "Advisor" : "Student";

            var notifications = BuildDemoNotifications();

            foreach (var n in notifications)
            {
                n.TimeAgo = GetTimeAgo(n.NotificationDate);
            }

            var filtered = notifications
                .Where(n =>
                    n.NotificationType == "System" ||
                    (role == "Advisor" && n.NotificationType == "Student") ||
                    (role == "Student" && n.NotificationType == "Advisor")
                )
                .OrderByDescending(n => n.NotificationDate)
                .ToList();

            return View(filtered);
        }

        private List<NotificationViewModel> BuildDemoNotifications()
        {
            return new List<NotificationViewModel>
            {
                new NotificationViewModel
                {
                    NotificationType = "Student",
                    SenderName = "Lama Alshikh",
                    Title = "Student recommendation",
                    NotificationContent = "View advising updates and new recommendation.",
                    NotificationDate = DateTime.Now.AddMinutes(-15),
                    IsRead = false,
                    TargetUrl = "/GraduationProjectEligibility/Form5"
                },
                new NotificationViewModel
                {
                    NotificationType = "System",
                    SenderName = "System",
                    Title = "System recommendation",
                    NotificationContent = "New system recommendation for this activity.",
                    NotificationDate = DateTime.Now.AddHours(-2),
                    IsRead = false,
                    TargetUrl = "/Student/StudentHome"
                }
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
