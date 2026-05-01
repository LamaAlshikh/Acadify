using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Acadify.Controllers
{
    public class WelcomeController : Controller
    {
        // عرض الصفحة الترحيبية (Welcome Page)
        public IActionResult Welcome()
        {
            return View();
        }

        // صفحة معالجة الأخطاء
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}