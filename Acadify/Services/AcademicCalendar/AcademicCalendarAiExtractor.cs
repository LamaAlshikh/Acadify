using Acadify.Models;
using Acadify.Services.AcademicCalendar.Interfaces;

namespace Acadify.Services.AcademicCalendar
{
    public class AcademicCalendarAiExtractor : IAcademicCalendarAiExtractor
    {
        public Task<List<AcademicCalendarEvent>> ExtractEventsFromPdfAsync(string pdfPath, int calendarId)
        {
            throw new NotImplementedException();
        }
    }
}