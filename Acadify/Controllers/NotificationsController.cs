using Microsoft.AspNetCore.Mvc;

namespace Acadify.Controllers
{
    public class NotificationsController : Controller
    {
        // فقط يفتح لوحة الإشعارات
        public IActionResult Panel()
        {
            return ViewComponent("Notifications");
        }
    }
}
