using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class MeetingMessage
{
    public int MessageId { get; set; }

    public int MeetingId { get; set; }

    public string SenderName { get; set; } = null!;

    public string MessageText { get; set; } = null!;

    public DateTime? MessageDate { get; set; }
    public bool IsRecorded { get; set; }
}
