using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class StudyPlanMatchingForm
{
    public int FormId { get; set; }

    public string? GraduationStatus { get; set; }

    public int? RemainingHours { get; set; }

    public int? RequiredHours { get; set; }

    public int? EarnedHours { get; set; }

    public int? RegisteredHours { get; set; }

    public virtual Form Form { get; set; } = null!;
}
