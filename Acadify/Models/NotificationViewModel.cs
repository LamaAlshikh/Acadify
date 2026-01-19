using System;

namespace Acadify.Models
{
    // يمثل الإشعار + بيانات العرض
    public class NotificationViewModel
    {
        // من الدايقرام
        public int NotificationID { get; set; }
        public string NotificationContent { get; set; } = string.Empty;
        public DateTime NotificationDate { get; set; }
        public string TimeAgo { get; set; }
        public string NotificationType { get; set; } = "System";
        // System / Student / Advisor

        // إضافات للواجهة (مسموح لأنها تخدم العرض)
        public string Title { get; set; } = string.Empty;      // عنوان الإشعار
        public string SenderName { get; set; } = "System";     // المرسل
        public bool IsRead { get; set; } = false;              // غير مقروء
        public string TargetUrl { get; set; } = "#";           // المكان اللي يودّي له السهم
    }
}
