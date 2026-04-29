using Acadify.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acadify.Controllers
{
    public class AccountController : Controller
    {
        // يعرض صفحة التسجيل
        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        // يستقبل بيانات التسجيل
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(SignUpVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // هنا لاحقاً نحفظ البيانات في الداتابيس

            return RedirectToAction("SignUp");
        }

        // يعرض صفحة تسجيل الدخول
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // يستقبل بيانات تسجيل الدخول
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // مؤقتًا: تسجيل دخول تجريبي
            if (model.Email == "student@acadify.com" && model.Password == "1234")
            {
                return RedirectToAction("studentHome", "Student");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }
    }
}