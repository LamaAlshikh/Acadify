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

    public virtual ICollection<StudyPlan> Plans { get; set; } = new List<StudyPlan>();

    public virtual ICollection<Transcript> Transcripts { get; set; } = new List<Transcript>();
}
