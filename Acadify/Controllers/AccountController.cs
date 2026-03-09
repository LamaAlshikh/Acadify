using Acadify.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acadify.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

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