using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("Notification")]
[Index("StudentId", Name = "IX_Notif_StudentID")]
[Index("AdvisorId", Name = "IX_Notif_AdvisorID")]
[Index("AdminId", Name = "IX_Notif_AdminID")]
public partial class Notification
{
    [Key]
    [Column("notificationID")]
    public int NotificationId { get; set; }

    [Column("message")]
    public string Message { get; set; } = null!;

    [Column("date")]
    public DateTime Date { get; set; }

    // ممكن نخليه للاسم القديم أو توصيف إضافي
    [Column("type")]
    [StringLength(100)]
    public string? Type { get; set; }

    [Column("senderRole")]
    [StringLength(50)]
    public string? SenderRole { get; set; }
    // Student / Advisor / Admin / System

    [Column("sourceType")]
    [StringLength(50)]
    public string? SourceType { get; set; }
    // Recommendation / Meeting / Chat / Form / Transcript / Calendar / Request / StudyPlan / General

    [Column("advisorID")]
    public int? AdvisorId { get; set; }

    [Column("studentID")]
    public int? StudentId { get; set; }

    [Column("adminID")]
    public int? AdminId { get; set; }

    [Column("isRead")]
    public bool IsRead { get; set; } = false;

    [ForeignKey("AdvisorId")]
    [InverseProperty("Notifications")]
    public virtual Advisor? Advisor { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Notifications")]
    public virtual Student? Student { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("Notifications")]
    public virtual Admin? Admin { get; set; }
}