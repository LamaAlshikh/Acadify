using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class Course
{
    public string CourseId { get; set; } = null!;

    public string CourseName { get; set; } = null!;

    public int Hours { get; set; }

    public string? Prerequisite { get; set; }

    public string? GraduationRequirement { get; set; }
<<<<<<< HEAD
<<<<<<< HEAD

<<<<<<< HEAD
=======
    public string? RequirementCategory { get; set; }
>>>>>>> origin_second/rahafgh
=======
    public string? RequirementCategory { get; set; }

>>>>>>> origin_second/linaLMversion
=======

>>>>>>> origin_second/لما2
    public virtual ICollection<StudyPlan> Plans { get; set; } = new List<StudyPlan>();

    public virtual ICollection<Transcript> Transcripts { get; set; } = new List<Transcript>();
}
