using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Acadify.Models.Db;

[Table("Advisor")]
public partial class Advisor
{
    [Key]
    [Column("advisorID")]
    public int AdvisorId { get; set; }

    [Column("userID")]
    public int UserId { get; set; }

    [Column("department")]
    [StringLength(120)]
    public string? Department { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(Db.User.Advisor))]
    public virtual User User { get; set; } = null!;

    [InverseProperty(nameof(Db.Form.Advisor))]
    public virtual ICollection<Form> Forms { get; set; } = new List<Form>();

    [InverseProperty(nameof(Db.Meeting.Advisor))]
    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    [InverseProperty(nameof(Db.Notification.Advisor))]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty(nameof(Db.Student.Advisor))]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}