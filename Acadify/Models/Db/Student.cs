using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class Student
{
    public int StudentId { get; set; }

    public string Name { get; set; } = null!;

    public string? Major { get; set; }

    public string? Level { get; set; }

    public int CompletedHours { get; set; }

    public int? CohortYear { get; set; }

    public int? AdvisorId { get; set; }

    // العلاقات الملاحية (Navigation Properties)
    public virtual Advisor? Advisor { get; set; }

    public virtual ICollection<Form> Forms { get; set; } = new List<Form>();

    public virtual GraduationStatus? GraduationStatus { get; set; }

    public virtual MatchingStatus? MatchingStatus { get; set; }

    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Transcript? Transcript { get; set; }

    // تم دمج إضافة لينا لتمكين قرارات المواد الأكاديمية
    public virtual ICollection<TranscriptCourseDecision> TranscriptCourseDecisions { get; set; } = new List<TranscriptCourseDecision>();
}