namespace Acadify.Models.AdminPages
{
    public class StudentAdvisor
    {
        public int StudentAdvisorId { get; set; }

        public int StudentId { get; set; }
        public Student? Student { get; set; }

        public int AdvisorId { get; set; }
        public Advisor? Advisor { get; set; }

        public DateTime ConnectedAt { get; set; } = DateTime.Now;
    }
}