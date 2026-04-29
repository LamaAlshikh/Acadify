using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class MeetingForm
{
    public int FormId { get; set; }
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======
    public int? MeetingId { get; set; }
>>>>>>> origin_second/rahafgh
=======
    public int? MeetingId { get; set; }
>>>>>>> origin_second/linaLMversion
=======
>>>>>>> origin_second/لما2

    public DateTime? MeetingStart { get; set; }

    public DateTime? MeetingEnd { get; set; }

    public string? MeetingPurpose { get; set; }

    public string? MeetingNotes { get; set; }

    public string? ReferralReason { get; set; }

    public string? ReferredTo { get; set; }

    public string? StudentActions { get; set; }

    public string? AdvisorActions { get; set; }

    public virtual Form Form { get; set; } = null!;
}
