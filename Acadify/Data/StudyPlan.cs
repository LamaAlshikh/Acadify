using System;
using System.Collections.Generic;

namespace Acadify.Data;

public partial class StudyPlan
{
    public int PlanId { get; set; }

    public string Major { get; set; } = null!;

    public int TotalHours { get; set; }

    public string? PdfFile { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
