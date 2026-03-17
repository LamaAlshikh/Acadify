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
        // عدد ساعات مكتسبة + مسجلة للفصل الحالي
        public int EarnedHours { get; set; } = 0;        // مكتسبة
        public int RegisteredHours { get; set; } = 0;    // مسجلة للفصل الحالي

        // الساعات التفصيلية (القيم اللي في الفورم)
        public int UniversityReqHours { get; set; } = 26;        // متطلبات جامعة
        public int PrepYearReqHours { get; set; } = 15;          // متطلبات السنة التحضيرية
        public int FreeCoursesHours { get; set; } = 9;           // مواد حرة
        public int CollegeMandatoryHours { get; set; } = 24;     // متطلبات الكلية الإجبارية
        public int DeptMandatoryHours { get; set; } = 57;        // مواد القسم الإجبارية
        public int DeptElectiveHours { get; set; } = 9;          // مواد القسم الاختيارية

        // مجموع الساعات حسب الخطة
        public int TotalHours { get; set; } = 140;

        // نص التخرج (مثال: "الفصل الدراسي الاول 2024")
        public string GraduationTermText { get; set; } = "الفصل الدراسي الاول 2024";

        // ======================
        // Notes (ملحوظات إن وجدت)
        // ======================
        public string Note1 { get; set; } = "";
        public string Note2 { get; set; } = "";
        public string Note3 { get; set; } = "";
        public string Note4 { get; set; } = "";

        // ======================
        // Advisor Footer
        // ======================
        public string AdvisorNameLabel { get; set; } = "المرشدة الأكاديمية للطالبة";
        public string AdvisorName { get; set; } = "";
        public string AdvisorSignature { get; set; } = "";

        // ======================
        // Workflow
        // ======================
        public string Status { get; set; } = "Draft"; // Draft / Sent
        public string AdvisorNotes { get; set; } = "";
    }
}