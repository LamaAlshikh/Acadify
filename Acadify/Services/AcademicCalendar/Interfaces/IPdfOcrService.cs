namespace Acadify.Services.AcademicCalendar.Interfaces
{
    public interface IPdfOcrService
    {
        Task<string> ExtractTextByOcrAsync(string pdfPath);
<<<<<<< HEAD
=======
        Task<string> ExtractPageTextByOcrAsync(string pdfPath, int pageNumber);
>>>>>>> origin_second/لما2
    }
}