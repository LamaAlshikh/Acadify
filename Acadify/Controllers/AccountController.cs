using Acadify.Models;
using Microsoft.AspNetCore.Mvc;

namespace projectActaify.Controllers
{
    public class AccountController : Controller
    {
        // يعرض صفحة التسجيل
        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        // يستقبل بيانات الفورم
        [HttpPost]
        public IActionResult SignUp(SignUpVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // هنا لاحقاً نحفظ البيانات في الداتابيس

            return RedirectToAction("SignUp");
        }
    }
}