using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models.Db;

public partial class AcadifyDbContext : DbContext
{
    public AcadifyDbContext()
    {
    }

    public AcadifyDbContext(DbContextOptions<AcadifyDbContext> options)
        : base(options)
    {
    }

    // --- مجموعات البيانات (DbSets) ---
    public virtual DbSet<AcademicAdvisingConfirmationForm> AcademicAdvisingConfirmationForms { get; set; }
    public virtual DbSet<AcademicCalendar> AcademicCalendars { get; set; }
    public virtual DbSet<Advisor> Advisors { get; set; }
    public virtual DbSet<Community> Communities { get; set; }
    public virtual DbSet<CommunityMessage> CommunityMessages { get; set; }
    public virtual DbSet<Course> Courses { get; set; }
    public virtual DbSet<Form> Forms { get; set; }
    public virtual DbSet<GraduationProjectEligibilityForm> GraduationProjectEligibilityForms { get; set; }
    public virtual DbSet<GraduationStatus> GraduationStatuses { get; set; }
    public virtual DbSet<MatchingStatus> MatchingStatuses { get; set; }
    public virtual DbSet<Meeting> Meetings { get; set; }
    public virtual DbSet<MeetingForm> MeetingForms { get; set; }
    public virtual DbSet<MeetingMessage> MeetingMessages { get; set; }
    public virtual DbSet<NextSemesterCourseSelectionForm> NextSemesterCourseSelectionForms { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Student> Students { get; set; }
    public virtual DbSet<StudyPlan> StudyPlans { get; set; }
    public virtual DbSet<StudyPlanMatchingForm> StudyPlanMatchingForms { get; set; }
    public virtual DbSet<Transcript> Transcripts { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<VwMyStudent> VwMyStudents { get; set; }

    // إضافات رهف ولينا
    public virtual DbSet<StudyPlanCourse> StudyPlanCourses { get; set; }
    public virtual DbSet<CourseChoiceMonitoringForm> CourseChoiceMonitoringForms { get; set; }
    public virtual DbSet<TranscriptCourseDecision> TranscriptCourseDecisions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:AcadifyDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. StudyPlanCourse (إعداد رهف - جدول ربط مخصص)
        modelBuilder.Entity<StudyPlanCourse>(entity =>
        {
            entity.ToTable("StudyPlanCourse");
            entity.HasKey(e => new { e.PlanId, e.CourseId });
            entity.Property(e => e.PlanId).HasColumnName("planID");
            entity.Property(e => e.CourseId).HasColumnName("courseID");
            entity.Property(e => e.SemesterNo).HasColumnName("semesterNo");
            entity.Property(e => e.DisplayOrder).HasColumnName("displayOrder");
        });

        // 2. User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CDF81DA4C0D");
            entity.ToTable("User");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(255).HasColumnName("password");
        });

        // 3. Advisor
        modelBuilder.Entity<Advisor>(entity =>
        {
            entity.HasKey(e => e.AdvisorId).HasName("PK__Advisor__D0081275B285C8F3");
            entity.ToTable("Advisor");
            entity.Property(e => e.AdvisorId).ValueGeneratedNever().HasColumnName("advisorID");
            entity.Property(e => e.Department).HasMaxLength(120).HasColumnName("department");
            entity.HasOne(d => d.AdvisorNavigation).WithOne(p => p.Advisor)
                .HasForeignKey<Advisor>(d => d.AdvisorId).HasConstraintName("FK_Advisor_User");
        });

        // 4. Student
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__4D11D65C8C8F6A12");
            entity.ToTable("Student");
            entity.Property(e => e.StudentId).ValueGeneratedNever().HasColumnName("studentID");
            entity.Property(e => e.Major).HasMaxLength(120).HasColumnName("major");
            entity.Property(e => e.Name).HasMaxLength(120);
            entity.HasOne(d => d.Advisor).WithMany(p => p.Students)
                .HasForeignKey(d => d.AdvisorId).OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_Student_Advisor");
        });

        // 5. Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Course__2AA84FF1285E5A7F");
            entity.ToTable("Course");
            entity.Property(e => e.CourseId).HasMaxLength(30).HasColumnName("courseID");
            entity.Property(e => e.CourseName).HasMaxLength(200).HasColumnName("courseName");
            entity.Property(e => e.RequirementCategory).HasMaxLength(50).HasColumnName("RequirementCategory");
            entity.Property(e => e.Hours).HasColumnName("hours");
            entity.Property(e => e.Prerequisite).HasMaxLength(200).HasColumnName("prerequisite");
        });

        // 6. Forms (الجدول الرئيسي)
        modelBuilder.Entity<Form>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Forms__51BCB7CB3C44586F");
            entity.Property(e => e.FormId).HasColumnName("formID");
            entity.Property(e => e.FormStatus).HasMaxLength(60).HasDefaultValue("Pending").HasColumnName("formStatus");
            entity.Property(e => e.FormType).HasMaxLength(80).HasColumnName("formType");
            entity.Property(e => e.FormDate).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("formDate");

            entity.HasOne(d => d.Advisor).WithMany(p => p.Forms).HasForeignKey(d => d.AdvisorId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.Student).WithMany(p => p.Forms).HasForeignKey(d => d.StudentId).HasConstraintName("FK_Forms_Student");
        });

        // 7. StudyPlanMatchingForm
        modelBuilder.Entity<StudyPlanMatchingForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__StudyPla__51BCB7CBBA7C5B63");
            entity.ToTable("StudyPlanMatchingForm");
            entity.Property(e => e.FormId).ValueGeneratedNever().HasColumnName("formID");
            entity.Property(e => e.UniversityHours).HasColumnName("universityHours");
            entity.Property(e => e.PrepYearHours).HasColumnName("prepYearHours");
            entity.Property(e => e.FreeCoursesHours).HasColumnName("freeCoursesHours");
            entity.Property(e => e.CollegeMandatoryHours).HasColumnName("collegeMandatoryHours");
            entity.Property(e => e.DeptMandatoryHours).HasColumnName("deptMandatoryHours");
            entity.Property(e => e.DeptElectiveHours).HasColumnName("deptElectiveHours");
            entity.Property(e => e.TotalHours).HasColumnName("totalHours");
            entity.HasOne(d => d.Form).WithOne(p => p.StudyPlanMatchingForm).HasForeignKey<StudyPlanMatchingForm>(d => d.FormId);
        });

        // 8. CourseChoiceMonitoringForm (رهف)
        modelBuilder.Entity<CourseChoiceMonitoringForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK_CourseChoiceMonitoringForm");
            entity.ToTable("CourseChoiceMonitoringForm");
            entity.Property(e => e.FormId).ValueGeneratedNever().HasColumnName("formID");
            entity.Property(e => e.SelectedCoursesJson).HasColumnName("selectedCoursesJson");
            entity.HasOne(d => d.Form).WithOne(p => p.CourseChoiceMonitoringForm).HasForeignKey<CourseChoiceMonitoringForm>(d => d.FormId);
        });

        // 9. TranscriptCourseDecision (لينا)
        modelBuilder.Entity<TranscriptCourseDecision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DecisionType).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.HasOne(d => d.Student).WithMany(p => p.TranscriptCourseDecisions).HasForeignKey(d => d.StudentId);
            entity.HasOne(d => d.TranscriptCourse).WithMany().HasForeignKey(d => d.TranscriptCourseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.EquivalentCourse).WithMany().HasForeignKey(d => d.EquivalentCourseId).OnDelete(DeleteBehavior.Restrict);
        });

        // 10. Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__4BA5CE8975CAE89B");
            entity.ToTable("Notification");
            entity.Property(e => e.NotificationId).HasColumnName("notificationID");
            entity.Property(e => e.Date).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("date");
            entity.HasOne(d => d.Student).WithMany(p => p.Notifications).HasForeignKey(d => d.StudentId).OnDelete(DeleteBehavior.Cascade);
        });

        // 11. View: VwMyStudent
        modelBuilder.Entity<VwMyStudent>(entity =>
        {
            entity.HasNoKey().ToView("vw_MyStudents");
            entity.Property(e => e.StudentId).HasColumnName("studentID");
            entity.Property(e => e.StudentName).HasMaxLength(120).HasColumnName("studentName");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}