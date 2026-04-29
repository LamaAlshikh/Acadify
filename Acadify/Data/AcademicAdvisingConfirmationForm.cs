using System;
using System.Collections.Generic;

namespace Acadify.Data;

public partial class AcademicAdvisingConfirmationForm
{
    public int FormId { get; set; }

    public string? StudentName { get; set; }

    public string? StudentLevel { get; set; }

    public decimal? CurrentGpa { get; set; }

    public int? CoursesCount { get; set; }

    public virtual Form Form { get; set; } = null!;
}
