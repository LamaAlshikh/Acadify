using System.Collections.Generic;

namespace Acadify.Models
{
    public class Form4ViewModel
    {
        // ======================
        // Header (البيانات الأساسية)
        // ======================
        public string StudentName { get; set; } = "";
        public string StudentId { get; set; } = "";
        public string AcademicYear { get; set; } = "2024";

        // ======================
        // Hours (حسب نص الفورم)
        // ======================
        public int EarnedHours { get; set; } = 0;        // ساعات مكتسبة
        public int RegisteredHours { get; set; } = 0;    // مسجلة للفصل الحالي (من نسخة لما)

        // الساعات التفصيلية (للمطابقة مع الخطة)
        public int UniversityReqHours { get; set; } = 26;
        public int PrepYearReqHours { get; set; } = 15;
        public int FreeCoursesHours { get; set; } = 9;
        public int CollegeMandatoryHours { get; set; } = 24;
        public int DeptMandatoryHours { get; set; } = 57;
        public int DeptElectiveHours { get; set; } = 9;

        // مجموع الساعات الكلي حسب الخطة
        public int TotalHours { get; set; } = 140;
        public string GraduationTermText { get; set; } = "الفصل الدراسي الاول 2024";

        // ======================
        // Notes (الملحوظات الأربعة في الفورم الورقي)
        // ======================
        public string Note1 { get; set; } = "";
        public string Note2 { get; set; } = "";
        public string Note3 { get; set; } = "";
        public string Note4 { get; set; } = "";

        // ======================
        // Advisor & Management (إضافات لينا ولما)
        // ======================
        public string AdvisorNameLabel { get; set; } = "المرشدة الأكاديمية للطالبة";
        public string AdvisorName { get; set; } = "";
        public string AdvisorSignature { get; set; } = "";
        public string AdvisorNotes { get; set; } = "";

        // حالة الطلب (Draft / Sent / Approved)
        public string Status { get; set; } = "Draft";

        // ======================
        // Course Decisions (إضافات نسخة لينا المهمة للربط)
        // ======================
        // قائمة المواد التي تحتاج قرار (معادلة، إقرار، إلخ)
        public List<Form4CourseDecisionItemVM> PendingCourses { get; set; } = new();

        // خيارات المواد المتاحة في الخطة للاختيار منها
        public List<PlanCourseOptionVM> PlanCourseOptions { get; set; } = new();
    }
}