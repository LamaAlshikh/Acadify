using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("GraduationProjectEligibilityForm")]
public partial class GraduationProjectEligibilityForm
{
    [Key]
    [Column("formID")]
    public int FormId { get; set; }

    [Column("eligibility")]
    [StringLength(50)]
    public string? Eligibility { get; set; }

    [Column("requiredCoursesStatus")]
    [StringLength(200)]
    public string? RequiredCoursesStatus { get; set; }

    [ForeignKey("FormId")]
    [InverseProperty("GraduationProjectEligibilityForm")]
    public virtual Form Form { get; set; } = null!;
}
