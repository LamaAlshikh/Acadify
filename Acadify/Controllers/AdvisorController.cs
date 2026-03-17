using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

namespace Acadify.Controllers
{
    public class AdvisorController : Controller
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





        // GET: Advisor/Form3
        [HttpGet]
        public IActionResult Form3()
        {
            return View();
        }

        // POST: Advisor/SendForm3
        [HttpPost]
        public IActionResult SendForm3()
        {
            // لاحقاً: تحديث حالة الفورم في DB + إرسال للجنة
            TempData["Success"] = "Form 3 sent successfully.";
            return RedirectToAction("Form3");
        }

        // POST: Advisor/AddNotesForm3
        [HttpPost]
        public IActionResult AddNotesForm3(string notes)
        {
            // لاحقاً: حفظ الملاحظات في DB
            TempData["Success"] = "Notes saved successfully.";
            return RedirectToAction("Form3");
        }

        // GET: Advisor/Form3History
        [HttpGet]
        public IActionResult Form3History()
        {
            // لاحقاً: صفحة تاريخ الفورم
            return View();
        }




        private const string Form4SessionKey = "Form4Draft";

        // ==============================
        // GET: Advisor/Form4
        // ==============================
        [HttpGet]
        public IActionResult Form4()
        {
            var json = HttpContext.Session.GetString(Form4SessionKey);

            Form4ViewModel model;

            if (!string.IsNullOrEmpty(json))
            {
                model = JsonSerializer.Deserialize<Form4ViewModel>(json)
                        ?? CreateNewForm4();
            }
            else
            {
                model = CreateNewForm4();
            }

            return View(model);
        }

        // ==============================
        // POST: Save Draft
        // ==============================
        [HttpPost]
        public IActionResult SaveForm4(Form4ViewModel model)
        {
            model.Status = "Draft";
            SaveForm4ToSession(model);

            TempData["Success"] = "Form 4 saved successfully.";
            return RedirectToAction("Form4");
        }

        // ==============================
        // POST: Send To Committee
        // ==============================
        [HttpPost]
        public IActionResult SendForm4(Form4ViewModel model)
        {
            model.Status = "Sent";
            SaveForm4ToSession(model);

            TempData["Success"] = "Form 4 sent successfully.";
            return RedirectToAction("Form4");
        }

        // ==============================
        // Session Save Helper
        // ==============================
        private void SaveForm4ToSession(Form4ViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            HttpContext.Session.SetString(Form4SessionKey, json);
        }

        // ==============================
        // Create Default Form (تجريبي)
        // ==============================
        private Form4ViewModel CreateNewForm4()
        {
            return new Form4ViewModel
            {
                // بيانات الطالبة
                StudentName = "Lama Alshikh",
                StudentId = "000000000",
                AcademicYear = "2024",

                // الساعات المكتسبة والمسجلة
                EarnedHours = 129,
                RegisteredHours = 11,

                // تفاصيل الساعات حسب الفورم الرسمي
                UniversityReqHours = 26,
                PrepYearReqHours = 15,
                FreeCoursesHours = 9,
                CollegeMandatoryHours = 24,
                DeptMandatoryHours = 57,
                DeptElectiveHours = 9,

                TotalHours = 140,
                GraduationTermText = "الفصل الدراسي الاول 2024",

                // ملاحظات (مثل ملف الوورد)
                Note1 = "BUS220 و BUS320 تعادل إدارة المنظمات BUS 232",
                Note2 = "BUS230 و BUS335 تعادل إدارة المنظمات BUS 232",
                Note3 = "مادة التسويق تعادل BUS 232",
                Note4 = "تم احتساب 11 ساعة ضمن الفصل القادم",

                AdvisorNameLabel = "المرشدة الأكاديمية للطالبة",
                AdvisorName = "Dr. Amina Gamlo",
                AdvisorSignature = "",

                Status = "Draft"
            };


        }
}
}
