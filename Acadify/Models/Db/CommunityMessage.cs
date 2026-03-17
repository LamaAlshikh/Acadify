using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class CommunityMessage
{
    public int MessageId { get; set; }

    public int CommunityId { get; set; }

    public string SenderName { get; set; } = null!;

    public string MessageText { get; set; } = null!;

    public DateTime? MessageDate { get; set; }

    public virtual Community Community { get; set; } = null!;
}
