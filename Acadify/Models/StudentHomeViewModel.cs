namespace Acadify.Models
{
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
