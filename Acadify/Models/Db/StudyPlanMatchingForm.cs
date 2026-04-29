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

<<<<<<< HEAD
    public virtual Form Form { get; set; } = null!;
}
=======
    public int? UniversityHours { get; set; }

    public int? PrepYearHours { get; set; }

    public int? FreeCoursesHours { get; set; }

    public int? CollegeMandatoryHours { get; set; }

    public int? DeptMandatoryHours { get; set; }

    public int? DeptElectiveHours { get; set; }

    public int? TotalHours { get; set; }

    public virtual Form Form { get; set; } = null!;
}
>>>>>>> origin_second/rahafgh
