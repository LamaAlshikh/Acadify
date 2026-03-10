using System;
using System.Collections.Generic;

namespace Acadify.Data;

public partial class Transcript
{
    public int TranscriptId { get; set; }

    public int StudentId { get; set; }

    public string? PdfFile { get; set; }

    public decimal? Gpa { get; set; }

    public decimal? SemesterGpa { get; set; }

    public string? ExtractedInfo { get; set; }

    public string? ExtractedCourses { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
