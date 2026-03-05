using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class Advisor
{
    public int AdvisorId { get; set; }

    public string? Department { get; set; }

    public virtual User AdvisorNavigation { get; set; } = null!;

    public virtual ICollection<Form> Forms { get; set; } = new List<Form>();

    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
