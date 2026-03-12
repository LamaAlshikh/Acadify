using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class Community
{
    public int CommunityId { get; set; }

    public string CommunityName { get; set; } = null!;

    public virtual ICollection<CommunityMessage> CommunityMessages { get; set; } = new List<CommunityMessage>();
}
