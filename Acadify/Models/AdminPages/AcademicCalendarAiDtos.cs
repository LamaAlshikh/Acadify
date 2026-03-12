namespace Acadify.Models.AdminPages
{
    public class AcademicCalendarAiResult
    {
        public List<AcademicCalendarAiEvent> Events { get; set; } = new();
    }

    public class AcademicCalendarAiEvent
    {
        public string Event { get; set; } = "";

        // optional
        public string? Day_Ar { get; set; }
        public string? Hijri_Date { get; set; }
        public string? Gregorian_Date { get; set; }
    }
}