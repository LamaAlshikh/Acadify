using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class GraduationProjectEligibilityForm
{
    public int FormId { get; set; }

    public string? Eligibility { get; set; }

    public string? RequiredCoursesStatus { get; set; }

    public virtual Form Form { get; set; } = null!;
}
