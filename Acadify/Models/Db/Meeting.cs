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
<<<<<<< HEAD
=======
    public string? ChatSummary { get; set; }

>>>>>>> origin_second/rahafgh
=======
    public string? ChatSummary { get; set; }

>>>>>>> origin_second/linaLMversion

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
<<<<<<< HEAD
<<<<<<< HEAD
=======
=======
>>>>>>> origin_second/linaLMversion
    public bool IsRecordingStarted { get; set; }

    public string? LastRecordingAction { get; set; }
    public DateTime? RecordingStartedAt { get; set; }
    public DateTime? RecordingStoppedAt { get; set; }
<<<<<<< HEAD
>>>>>>> origin_second/rahafgh
=======
>>>>>>> origin_second/linaLMversion

    public virtual Advisor Advisor { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
<<<<<<< HEAD
<<<<<<< HEAD
=======

>>>>>>> origin_second/rahafgh
=======

>>>>>>> origin_second/linaLMversion
}
