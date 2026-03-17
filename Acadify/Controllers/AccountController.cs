using Acadify.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acadify.Controllers
{
    public class AccountController : Controller
    {
        // يعرض صفحة التسجيل
        [HttpGet]
        public IActionResult Login()
        public IActionResult SignUp()
        {
            return View();
        }

        // يستقبل بيانات الفورم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        public IActionResult SignUp(SignUpVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // مؤقتًا: تسجيل دخول تجريبي
            if (model.Email == "student@acadify.com" && model.Password == "1234")
            {
                return RedirectToAction("studentHome", "Student");
            }
            // هنا لاحقاً نحفظ البيانات في الداتابيس

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
            return RedirectToAction("SignUp");
        }
    }
}