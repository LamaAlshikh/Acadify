<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> origin_second/لما2
﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

[Table("GraduationProjectEligibilityForm")]
public partial class GraduationProjectEligibilityForm
<<<<<<< HEAD
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
=======
>>>>>>> origin_second/لما2
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

<<<<<<< HEAD
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
=======
    [ForeignKey("FormId")]
    [InverseProperty("GraduationProjectEligibilityForm")]
    public virtual Form Form { get; set; } = null!;
>>>>>>> origin_second/لما2
}
