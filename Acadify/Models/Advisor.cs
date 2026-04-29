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

[Table("Advisor")]
public partial class Advisor
{
    [Key]
    [Column("advisorID")]
    public int AdvisorId { get; set; }

<<<<<<< HEAD
=======
    [Column("userID")]
    public int UserId { get; set; }

>>>>>>> origin_second/لما2
    [Column("department")]
    [StringLength(120)]
    public string? Department { get; set; }

<<<<<<< HEAD
    [ForeignKey("AdvisorId")]
    [InverseProperty("Advisor")]
    public virtual User AdvisorNavigation { get; set; } = null!;
=======
    [ForeignKey("UserId")]
    [InverseProperty("Advisor")]
    public virtual User User { get; set; } = null!;
>>>>>>> origin_second/لما2

    [InverseProperty("Advisor")]
    public virtual ICollection<Form> Forms { get; set; } = new List<Form>();

    [InverseProperty("Advisor")]
    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    [InverseProperty("Advisor")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Advisor")]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
<<<<<<< HEAD
}
=======
}
>>>>>>> origin_second/لما2
