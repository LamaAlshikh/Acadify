using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class AcademicCalendar
{
    public int CalendarId { get; set; }

    public string? PdfFile { get; set; }

    public DateTime UploadedAt { get; set; }
}
