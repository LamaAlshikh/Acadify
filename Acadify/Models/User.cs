<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
=======
﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
>>>>>>> origin_second/لما2

namespace Acadify.Models;

[Table("User")]
<<<<<<< HEAD
[Index("Email", Name = "UQ__User__AB6E6164942EBD22", IsUnique = true)]
=======
>>>>>>> origin_second/لما2
public partial class User
{
    [Key]
    [Column("userID")]
    public int UserId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("email")]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [Column("password")]
    [StringLength(255)]
<<<<<<< HEAD
    [InverseProperty("User")]
    public virtual Admin? Admin { get; set; }
    public string Password { get; set; } = null!;

    [InverseProperty("AdvisorNavigation")]
    public virtual Advisor? Advisor { get; set; }
}
=======
    public string Password { get; set; } = null!;

    [InverseProperty("User")]
    public virtual Admin? Admin { get; set; }

    [InverseProperty("User")]
    public virtual Advisor? Advisor { get; set; }

    [InverseProperty("User")]
    public virtual Student? Student { get; set; }
}
>>>>>>> origin_second/لما2
