using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Acadify.Controllers
{
    public class GraduationProjectEligibilityController : Controller
    {
        // GET: عرض الفورم
        public IActionResult Form5()
        {
            // بيانات افتراضية (حالياً)
            var model = new GraduationProjectEligibilityForm
            {
                FormId = 5,
                StudentName = "Lama Zaki Alshikh",
                StudentId = "22190123",

                CPIS351 = true,
                CPIS358 = true,
                CPIS323 = true,

                CPIS360 = false,
                CPIS375 = false,
                CPIS342 = true,

                FormStatus = "Pending",
                CreatedDate = DateTime.Now
            };

            // تحديد الأهلية
            model.IsEligible =
                model.CPIS351 &&
                model.CPIS358 &&
                model.CPIS323 &&
                model.CPIS360 &&
                model.CPIS375 &&
                model.CPIS342;

            return View(model);
        }

        // POST: تحديث حالة الفورم (Accept / Reject / Update)
        [HttpPost]
        public IActionResult UpdateStatus(int formId, string status)
        {
            /*
            // 🔗 ربط الداتابيس (معلّق حالياً)
            using (var context = new AcadifyDbContext())
            {
                var form = context.GraduationProjectEligibilityForms
                                  .FirstOrDefault(f => f.FormId == formId);

                if (form != null)
                {
                    form.FormStatus = status;
                    context.SaveChanges();
                }
            }
            */

            // حالياً بدون تخزين
            return RedirectToAction("Form5");
        }
    }
}
