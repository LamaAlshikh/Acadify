namespace Acadify.Models
{
<<<<<<< HEAD
    public class StudenthomeViewModel
    {
        // Student basic information
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;

        // Graduation status
        public int ProgressPercentage { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public int RemainingHours { get; set; }

        // Extra display
        public int CompletedHours { get; set; }
        public int TotalRequiredHours { get; set; } = 140;
    }
}
=======
    public class StudentHomeViewModel
    {
        // Student basic information
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }

        // Graduation status
        public int ProgressPercentage { get; set; }
        public string CurrentStatus { get; set; }
    }
}
>>>>>>> origin_second/linaLMversion
