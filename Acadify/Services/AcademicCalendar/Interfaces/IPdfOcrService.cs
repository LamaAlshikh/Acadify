namespace Acadify.Services.AcademicCalendar.Interfaces
{
    public interface IPdfOcrService
    {
        Task<string> ExtractTextByOcrAsync(string pdfPath);
    }
}