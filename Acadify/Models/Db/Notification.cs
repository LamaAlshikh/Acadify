using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime Date { get; set; }

    public string? Type { get; set; }

    public int? AdvisorId { get; set; }

    public int? StudentId { get; set; }

    public virtual Advisor? Advisor { get; set; }

    public virtual Student? Student { get; set; }
}
