using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Acadify.Controllers
{
    [Route("[controller]/[action]")]
    public class GraduationProjectEligibilityController : Controller
    {
        private readonly AcadifyDbContext _context;

        public GraduationProjectEligibilityController(AcadifyDbContext context)
        {
            _context = context;
        }

        // GET: GraduationProjectEligibility/Form5?formId=5
        [HttpGet]
        public IActionResult Form5(int? formId)
        {
            int selectedFormId;

            if (formId.HasValue && formId.Value > 0)
            {
                selectedFormId = formId.Value;
            }
            else
            {
                selectedFormId = _context.Forms
                    .Where(f => f.FormType == "Form 5")   // ✅ هذا هو الصحيح عندك
                    .OrderByDescending(f => f.FormId)
                    .Select(f => f.FormId)
                    .FirstOrDefault();

                if (selectedFormId == 0)
                    return NotFound("No Form 5 record found in Forms table.");
            }

            // 1) جيبي سجل Form5 من جدول GraduationProjectEligibilityForm
            var form5 = _context.GraduationProjectEligibilityForms
                .Include(x => x.Form)
                .ThenInclude(f => f.Student)
                .FirstOrDefault(x => x.FormId == selectedFormId);

            if (form5 == null) return NotFound("Form5 not found in GraduationProjectEligibilityForm table.");

            int studentId = form5.Form.StudentId;

            // 2) جيبي Transcript للطالبة + المواد المرتبطة
            var transcript = _context.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefault(t => t.StudentId == studentId);

            // 3) تعبئة Student info للعرض
            // عدّلي اسم الخاصية حسب Student.cs عندك (Name/FullName/StudentName)
            form5.StudentName = form5.Form.Student?.Name ?? "-";
            form5.StudentId = studentId.ToString();

            // 4) لو ما فيه ترانسكربت
            if (transcript == null)
            {
                form5.CPIS351 = form5.CPIS358 = form5.CPIS323 =
                form5.CPIS360 = form5.CPIS375 = form5.CPIS342 = false;

                form5.IsEligible = false;

                // DB fields
                form5.Eligibility = "Not Eligible";
                form5.RequiredCoursesStatus = "Transcript not uploaded.";

                _context.SaveChanges();
                return View(form5);
            }

            // 5) دالة تساعدنا نفحص وجود المادة في Transcript.Courses
            bool HasCourse(string code)
            {
                string norm(string s) => (s ?? "").Replace(" ", "").Trim().ToUpper();
                var target = norm(code);

                return transcript.Courses.Any(c => norm(c.CourseId) == target);
            }

            // 6) تعبئة الصح/خطأ من الترانسكربت
            form5.CPIS351 = HasCourse("CPIS351");
            form5.CPIS358 = HasCourse("CPIS358");
            form5.CPIS323 = HasCourse("CPIS323");

            form5.CPIS360 = HasCourse("CPIS360");
            form5.CPIS375 = HasCourse("CPIS375");
            form5.CPIS342 = HasCourse("CPIS342");

            // 7) حساب الأهلية
            form5.IsEligible =
                form5.CPIS351 &&
                form5.CPIS358 &&
                form5.CPIS323 &&
                form5.CPIS360 &&
                form5.CPIS375 &&
                form5.CPIS342;

            // 8) تحديث DB fields (Eligibility + RequiredCoursesStatus)
            var missing = new List<string>();
            if (!form5.CPIS351) missing.Add("CPIS 351");
            if (!form5.CPIS358) missing.Add("CPIS 358");
            if (!form5.CPIS323) missing.Add("CPIS 323");
            if (!form5.CPIS360) missing.Add("CPIS 360");
            if (!form5.CPIS375) missing.Add("CPIS 375");
            if (!form5.CPIS342) missing.Add("CPIS 342");

            form5.Eligibility = form5.IsEligible ? "Eligible" : "Not Eligible";
            form5.RequiredCoursesStatus = missing.Any()
                ? "Missing: " + string.Join(", ", missing)
                : "All required courses are completed/registered.";

            _context.SaveChanges();

            return View(form5);
        }

        // POST: تحديث حالة الفورم (Accept / Reject / Update)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int formId, string status)
        {
            // 1) حدّث جدول Forms (FormStatus)
            var form = _context.Forms.FirstOrDefault(f => f.FormId == formId);
            if (form != null)
            {
                form.FormStatus = status;
                form.FormDate = DateTime.Now;
            }

            _context.SaveChanges();

            return RedirectToAction("Form5", new { formId });
        }
    }
}