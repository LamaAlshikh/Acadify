namespace Acadify.Models
{
    public class GraduationProjectEligibilityForm
    {
        public int FormId { get; set; }

        public string StudentName { get; set; }
        public string StudentId { get; set; }

        // المواد المكتملة
        public bool CPIS351 { get; set; }
        public bool CPIS358 { get; set; }
        public bool CPIS323 { get; set; }

        // المواد المسجلة أو المتوقعة
        public bool CPIS360 { get; set; }
        public bool CPIS375 { get; set; }
        public bool CPIS342 { get; set; }

        // النتيجة النهائية
        public bool IsEligible { get; set; }

        public string AdvisorComment { get; set; }
        public string FormStatus { get; set; } // Pending / Accepted / Rejected
        public DateTime CreatedDate { get; set; }
    }
}
