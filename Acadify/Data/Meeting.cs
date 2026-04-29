using System;
using System.Collections.Generic;

namespace Acadify.Data;

public partial class Meeting
{
    public int MeetingId { get; set; }

    public int StudentId { get; set; }

    public int AdvisorId { get; set; }

    public string? ChatRecord { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public virtual Advisor Advisor { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
