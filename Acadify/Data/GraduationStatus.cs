using System;
using System.Collections.Generic;

namespace Acadify.Data;

public partial class GraduationStatus
{
    public int StatusId { get; set; }

    public int StudentId { get; set; }

    public string Status { get; set; } = null!;

    public int RemainingHours { get; set; }

    public virtual Student Student { get; set; } = null!;
}
