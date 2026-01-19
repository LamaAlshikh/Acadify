using Acadify.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acadify.Controllers
{
    public class StudentController : Controller
    {
        // Student Home Page
        public IActionResult StudentHome()
        {
            // مؤقتًا: القيمة جاية من الإيجنت
            // لاحقًا: تستبدل بقيمة من Database
            int progressFromAgent = 80;

            var model = new StudentHomeViewModel
            {
                StudentName = "lama alshikh",
                StudentEmail = "lalshikh@stu.kau.edu.sa",
                ProgressPercentage = progressFromAgent,
                CurrentStatus = GetStatus(progressFromAgent)
            };

            return View(model);
        }

        // تحديد حالة الطالبة بناءً على نسبة التقدم
        private string GetStatus(int progress)
        {
            if (progress <= 30)
                return "Beginning";

            if (progress <= 70)
                return "Has Remaining Courses";

            return "Near Graduation";
        }
    }
}
