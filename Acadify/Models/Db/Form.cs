using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace Acadify.Models.Db;



public partial class Form
{

    [InverseProperty("Form")]
    public virtual CourseChoiceMonitoringForm? CourseChoiceMonitoringForm { get; set; }
    public int FormId { get; set; }

    public int StudentId { get; set; }

    public int AdvisorId { get; set; }

    public string FormType { get; set; } = null!;

    public DateTime FormDate { get; set; }

    public string FormStatus { get; set; } = null!;

    public string? AdvisorNotes { get; set; }

    public bool AutoFilled { get; set; }

    public bool? AdvisorConfirmation { get; set; }

    public virtual AcademicAdvisingConfirmationForm? AcademicAdvisingConfirmationForm { get; set; }

    public virtual Advisor Advisor { get; set; } = null!;

    public virtual GraduationProjectEligibilityForm? GraduationProjectEligibilityForm { get; set; }

    public virtual MeetingForm? MeetingForm { get; set; }

    public virtual NextSemesterCourseSelectionForm? NextSemesterCourseSelectionForm { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual StudyPlanMatchingForm? StudyPlanMatchingForm { get; set; }


}
