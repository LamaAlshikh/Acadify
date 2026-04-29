using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.DbModels;

[Table("Student")]
[Index("AdvisorId", Name = "IX_Student_AdvisorID")]
public partial class Student
{
    [Key]
    [Column("studentID")]
    public int StudentId { get; set; }

    [StringLength(120)]
    public string Name { get; set; } = null!;

    [Column("major")]
    [StringLength(120)]
    public string? Major { get; set; }

    [Column("level")]
    [StringLength(50)]
    public string? Level { get; set; }

    [Column("completedHours")]
    public int CompletedHours { get; set; }

    [Column("cohortYear")]
    public int? CohortYear { get; set; }

    [Column("advisorID")]
    public int? AdvisorId { get; set; }

    [ForeignKey("AdvisorId")]
    [InverseProperty("Students")]
    public virtual Advisor? Advisor { get; set; }

    [InverseProperty("Student")]
    public virtual ICollection<Form> Forms { get; set; } = new List<Form>();

    [InverseProperty("Student")]
    public virtual GraduationStatus? GraduationStatus { get; set; }

    [InverseProperty("Student")]
    public virtual MatchingStatus? MatchingStatus { get; set; }

    [InverseProperty("Student")]
    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    [InverseProperty("Student")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Student")]
    public virtual Transcript? Transcript { get; set; }
}
