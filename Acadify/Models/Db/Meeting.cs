using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class Meeting
{
    public int MeetingId { get; set; }

    public int StudentId { get; set; }

    public int AdvisorId { get; set; }

    public string? ChatRecord { get; set; }
<<<<<<< HEAD
=======
    public string? ChatSummary { get; set; }

>>>>>>> origin_second/rahafgh

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
<<<<<<< HEAD
=======
    public bool IsRecordingStarted { get; set; }

    public string? LastRecordingAction { get; set; }
    public DateTime? RecordingStartedAt { get; set; }
    public DateTime? RecordingStoppedAt { get; set; }
>>>>>>> origin_second/rahafgh

    public virtual Advisor Advisor { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
<<<<<<< HEAD
=======

>>>>>>> origin_second/rahafgh
}
