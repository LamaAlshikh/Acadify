using Acadify.Models;

namespace Acadify.ViewModels
{
    public class GraduationProjectEligibilityFormVM
    {
        public int FormId { get; set; }

        public string StudentName { get; set; } = "-";
        public string StudentId { get; set; } = "-";

        public bool CPIS351 { get; set; }
        public bool CPIS358 { get; set; }
        public bool CPIS323 { get; set; }
        public bool CPIS380 { get; set; }
        public bool CPIS357 { get; set; }
        public bool CPIS342 { get; set; }

        public bool IsEligible { get; set; }

        public string? Eligibility { get; set; }
        public string? RequiredCoursesStatus { get; set; }

        public bool IsHistoryView { get; set; }
        public bool IsEditMode { get; set; }

        public Form Form { get; set; } = null!;
    }
}