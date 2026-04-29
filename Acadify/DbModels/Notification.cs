using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.DbModels;

[Table("Notification")]
[Index("StudentId", Name = "IX_Notif_StudentID")]
public partial class Notification
{
    [Key]
    [Column("notificationID")]
    public int NotificationId { get; set; }

    [Column("message")]
    public string Message { get; set; } = null!;

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("type")]
    [StringLength(60)]
    public string? Type { get; set; }

    [Column("advisorID")]
    public int? AdvisorId { get; set; }

    [Column("studentID")]
    public int? StudentId { get; set; }

    [ForeignKey("AdvisorId")]
    [InverseProperty("Notifications")]
    public virtual Advisor? Advisor { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Notifications")]
    public virtual Student? Student { get; set; }
}
