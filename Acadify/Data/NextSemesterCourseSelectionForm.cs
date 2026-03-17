using System;
using System.Collections.Generic;

namespace Acadify.Data;

public partial class NextSemesterCourseSelectionForm
{
    public int FormId { get; set; }

    public string? RecommendedCourses { get; set; }

    public int? RecommendedHours { get; set; }

    public string? TrackChoice { get; set; }

    public string? GpaChange { get; set; }

    public string? PrerequisiteViolation { get; set; }

    public virtual Form Form { get; set; } = null!;
}
