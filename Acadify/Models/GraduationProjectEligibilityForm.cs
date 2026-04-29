<<<<<<< HEAD
﻿using System;
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
=======
﻿namespace Acadify.Models
{
    public class GraduationProjectEligibilityForm
    {
        public int FormId { get; set; }

        public string StudentName { get; set; }
        public string StudentId { get; set; }

        // المواد المكتملة
        public bool CPIS351 { get; set; }
        public bool CPIS358 { get; set; }
        public bool CPIS323 { get; set; }

        // المواد المسجلة أو المتوقعة
        public bool CPIS360 { get; set; }
        public bool CPIS375 { get; set; }
        public bool CPIS342 { get; set; }

        // النتيجة النهائية
        public bool IsEligible { get; set; }

        public string AdvisorComment { get; set; }
        public string FormStatus { get; set; } // Pending / Accepted / Rejected
        public DateTime CreatedDate { get; set; }
    }
>>>>>>> origin_second/linaLMversion
}
