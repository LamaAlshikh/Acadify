using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.DbModels;

[Table("AcademicCalendar")]
public partial class AcademicCalendar
{
    [Key]
    [Column("calendarID")]
    public int CalendarId { get; set; }

    [Column("pdfFile")]
    [StringLength(255)]
    public string? PdfFile { get; set; }

    [Column("uploadedAt")]
    public DateTime UploadedAt { get; set; }
}
