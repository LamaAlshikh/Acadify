using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("Notification")]
<<<<<<< HEAD
[Index("StudentId", Name = "IX_Notif_StudentID")]
[Index("AdvisorId", Name = "IX_Notif_AdvisorID")]
[Index("AdminId", Name = "IX_Notif_AdminID")]
=======
[Index(nameof(StudentId), Name = "IX_Notif_StudentID")]
[Index(nameof(AdvisorId), Name = "IX_Notif_AdvisorID")]
[Index(nameof(AdminId), Name = "IX_Notif_AdminID")]
>>>>>>> origin_second/لما2
public partial class Notification
{
    [Key]
    [Column("notificationID")]
    public int NotificationId { get; set; }

    [Column("message")]
    public string Message { get; set; } = null!;

    [Column("date")]
    public DateTime Date { get; set; }

<<<<<<< HEAD
    // ممكن نخليه للاسم القديم أو توصيف إضافي
=======
>>>>>>> origin_second/لما2
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

<<<<<<< HEAD
    [ForeignKey("AdvisorId")]
    [InverseProperty("Notifications")]
    public virtual Advisor? Advisor { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Notifications")]
    public virtual Student? Student { get; set; }

    [ForeignKey("AdminId")]
=======
    [ForeignKey(nameof(AdvisorId))]
    [InverseProperty("Notifications")]
    public virtual Advisor? Advisor { get; set; }

    [ForeignKey(nameof(StudentId))]
    [InverseProperty("Notifications")]
    public virtual Student? Student { get; set; }

    [ForeignKey(nameof(AdminId))]
>>>>>>> origin_second/لما2
    [InverseProperty("Notifications")]
    public virtual Admin? Admin { get; set; }
}