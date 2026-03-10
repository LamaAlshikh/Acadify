using Acadify.Models;

namespace Acadify.Services.AcademicCalendar.Interfaces
{
    public interface IAcademicCalendarAiExtractor
    {
        Task<List<AcademicCalendarEvent>> ExtractEventsFromPdfAsync(string pdfPath, int calendarId);
    }
}