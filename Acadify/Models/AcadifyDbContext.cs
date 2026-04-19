using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Models;

public partial class AcadifyDbContext : DbContext
{
    public AcadifyDbContext()
    {
    }

    public AcadifyDbContext(DbContextOptions<AcadifyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicAdvisingConfirmationForm> AcademicAdvisingConfirmationForms { get; set; }

    public virtual DbSet<AcademicCalendar> AcademicCalendars { get; set; }
    public virtual DbSet<AcademicCalendarEvent> AcademicCalendarEvents { get; set; }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Advisor> Advisors { get; set; }

    public virtual DbSet<AdvisorRequest> AdvisorRequests { get; set; }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicAdvisingConfirmationForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Academic__51BCB7CB0845D0EC");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form)
                .WithOne(p => p.AcademicAdvisingConfirmationForm)
                .HasConstraintName("FK_AACF_Forms");
        });

        modelBuilder.Entity<AcademicCalendar>(entity =>
        {
            entity.HasKey(e => e.CalendarId).HasName("PK__Academic__EE5496D6D3FAC23E");

            entity.Property(e => e.UploadedAt)
                  .HasDefaultValueSql("(sysdatetime())");

            entity.HasMany(e => e.Events)
                  .WithOne(e => e.AcademicCalendar)
                  .HasForeignKey(e => e.CalendarId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AcademicCalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.GregorianDate)
                  .HasColumnType("date")
                  .IsRequired();

            entity.Property(e => e.HijriDate)
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(e => e.DayAr)
                  .HasMaxLength(20);

            entity.Property(e => e.EventName)
                  .HasMaxLength(500)
                  .IsRequired();

            entity.HasOne(e => e.AcademicCalendar)
                  .WithMany(c => c.Events)
                  .HasForeignKey(e => e.CalendarId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK_Admin");

            entity.HasOne(d => d.User)
                .WithOne(p => p.Admin)
                .HasForeignKey<Admin>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Admin_User");
        });

        modelBuilder.Entity<Advisor>(entity =>
        {
            entity.HasKey(e => e.AdvisorId).HasName("PK__Advisor__D008127590DAB1D2");

            entity.HasOne(d => d.User)
                .WithOne(p => p.Advisor)
                .HasForeignKey<Advisor>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Advisor_User");
        });

        modelBuilder.Entity<AdvisorRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK_AdvisorRequest");

            entity.ToTable("AdvisorRequest");

            entity.Property(e => e.RequestId).HasColumnName("requestID");
            entity.Property(e => e.StudentId).HasColumnName("studentID");
            entity.Property(e => e.RequestedAdvisorId).HasColumnName("requestedAdvisorID");

            entity.Property(e => e.RequestedAdvisorEmail)
                .HasMaxLength(150)
                .HasColumnName("requestedAdvisorEmail");

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasColumnName("status")
                .HasDefaultValue("Pending");

            entity.Property(e => e.AdminNote)
                .HasMaxLength(300)
                .HasColumnName("adminNote");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("createdAt")
                .HasDefaultValueSql("(sysdatetime())");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updatedAt");

            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdvisorRequest_Student");

            entity.HasOne(d => d.RequestedAdvisor)
                .WithMany()
                .HasForeignKey(d => d.RequestedAdvisorId)
                .HasConstraintName("FK_AdvisorRequest_Advisor");
        });

        modelBuilder.Entity<Community>(entity =>
        {
            entity.HasKey(e => e.CommunityId).HasName("PK__Communit__938137AD670B16D6");
        });

        modelBuilder.Entity<CommunityMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Communit__4808B873563E5945");

            entity.Property(e => e.MessageDate).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Community)
                .WithMany(p => p.CommunityMessages)
                .HasConstraintName("FK_CommunityMessages_Community");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Course__2AA84FF1B61B80F1");
        });

        modelBuilder.Entity<Form>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Forms__51BCB7CBE022D5BE");

            entity.Property(e => e.FormDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FormStatus).HasDefaultValue("Pending");

            entity.HasOne(d => d.Advisor)
                .WithMany(p => p.Forms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Forms_Advisor");

            entity.HasOne(d => d.Student)
                .WithMany(p => p.Forms)
                .HasConstraintName("FK_Forms_Student");
        });

        modelBuilder.Entity<GraduationProjectEligibilityForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Graduati__51BCB7CB8F43B050");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form)
                .WithOne(p => p.GraduationProjectEligibilityForm)
                .HasConstraintName("FK_GPEF_Forms");
        });

        modelBuilder.Entity<GraduationStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Graduati__36257A381AEA7C20");

            entity.HasOne(d => d.Student)
                .WithOne(p => p.GraduationStatus)
                .HasConstraintName("FK_GradStatus_Student");
        });

        modelBuilder.Entity<MatchingStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Matching__36257A38A661A82B");

            entity.HasOne(d => d.Student)
                .WithOne(p => p.MatchingStatus)
                .HasConstraintName("FK_MatchStatus_Student");
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.MeetingId).HasName("PK__Meeting__5C5E6E6403BECCEF");

            entity.HasOne(d => d.Advisor)
                .WithMany(p => p.Meetings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meeting_Advisor");

            entity.HasOne(d => d.Student)
                .WithMany(p => p.Meetings)
                .HasConstraintName("FK_Meeting_Student");
        });

        modelBuilder.Entity<MeetingForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__MeetingF__51BCB7CBD88E3109");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form)
                .WithOne(p => p.MeetingForm)
                .HasConstraintName("FK_MeetingForm_Forms");
        });

        modelBuilder.Entity<MeetingMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__MeetingM__4808B873E059A828");

            entity.Property(e => e.MessageDate).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<NextSemesterCourseSelectionForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__NextSeme__51BCB7CB55D38C62");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form)
                .WithOne(p => p.NextSemesterCourseSelectionForm)
                .HasConstraintName("FK_NSCSF_Forms");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__4BA5CE89C66256A6");

            entity.Property(e => e.Date).HasDefaultValueSql("(sysutcdatetime())");

            entity.Property(e => e.Type)
                .HasMaxLength(100);

            entity.Property(e => e.SenderRole)
                .HasMaxLength(50);

            entity.Property(e => e.SourceType)
                .HasMaxLength(50);

            entity.HasOne(d => d.Advisor)
                .WithMany(p => p.Notifications)
                .HasConstraintName("FK_Notif_Advisor");

            entity.HasOne(d => d.Student)
                .WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Notif_Student");

            entity.HasOne(d => d.Admin)
                .WithMany(p => p.Notifications)
                .HasConstraintName("FK_Notif_Admin");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__4D11D65C76ED7B60");

            entity.Property(e => e.StudentId).ValueGeneratedNever();

            entity.HasOne(d => d.Advisor)
                .WithMany(p => p.Students)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Student_Advisor");
        });

        modelBuilder.Entity<StudyPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__StudyPla__A2942D18D26A7268");

            entity.HasMany(d => d.Courses).WithMany(p => p.Plans)
                .UsingEntity<Dictionary<string, object>>(
                    "StudyPlanCourse",
                    r => r.HasOne<Course>().WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_SPC_Course"),
                    l => l.HasOne<StudyPlan>().WithMany()
                        .HasForeignKey("PlanId")
                        .HasConstraintName("FK_SPC_StudyPlan"),
                    j =>
                    {
                        j.HasKey("PlanId", "CourseId").HasName("PK__StudyPla__A03EA9E76A3A3058");
                        j.ToTable("StudyPlanCourse");
                        j.IndexerProperty<int>("PlanId").HasColumnName("planID");
                        j.IndexerProperty<string>("CourseId")
                            .HasMaxLength(30)
                            .HasColumnName("courseID");
                    });
        });

        modelBuilder.Entity<StudyPlanMatchingForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__StudyPla__51BCB7CBEE94575E");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form)
                .WithOne(p => p.StudyPlanMatchingForm)
                .HasConstraintName("FK_SPMF_Forms");
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasKey(e => e.TranscriptId).HasName("PK__Transcri__DF577D86C5DDBA34");

            entity.HasOne(d => d.Student)
                .WithOne(p => p.Transcript)
                .HasConstraintName("FK_Transcript_Student");

            entity.HasMany(d => d.Courses).WithMany(p => p.Transcripts)
                .UsingEntity<Dictionary<string, object>>(
                    "TranscriptCourse",
                    r => r.HasOne<Course>().WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_TC_Course"),
                    l => l.HasOne<Transcript>().WithMany()
                        .HasForeignKey("TranscriptId")
                        .HasConstraintName("FK_TC_Transcript"),
                    j =>
                    {
                        j.HasKey("TranscriptId", "CourseId").HasName("PK__Transcri__DDFDF979EB9B1C79");
                        j.ToTable("TranscriptCourse");
                        j.IndexerProperty<int>("TranscriptId").HasColumnName("transcriptID");
                        j.IndexerProperty<string>("CourseId")
                            .HasMaxLength(30)
                            .HasColumnName("courseID");
                    });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CDF6994AC27");
        });

        modelBuilder.Entity<VwMyStudent>(entity =>
        {
            entity.ToView("vw_MyStudents");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}