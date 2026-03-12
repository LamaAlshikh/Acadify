using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Acadify.Models.AdminPages;

public class UploadAcademicCalendarModel
{
    [Required(ErrorMessage = "Please upload a PDF file.")]
    public IFormFile? AcademicCalendarFile { get; set; }

    public string? Message { get; set; }

    public string? SavedFileName { get; set; }
}