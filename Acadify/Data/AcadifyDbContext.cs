using Acadify.DbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
namespace Acadify.Data;

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=RAHAF\\MSSQLSERVER01;Database=AcadifySeniorProject;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicAdvisingConfirmationForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Academic__51BCB7CB2E2C4B2B");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form).WithOne(p => p.AcademicAdvisingConfirmationForm).HasConstraintName("FK_AACF_Forms");
        });

        modelBuilder.Entity<AcademicCalendar>(entity =>
        {
            entity.HasKey(e => e.CalendarId).HasName("PK__Academic__EE5496D6B086B6FE");

            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<Advisor>(entity =>
        {
            entity.HasKey(e => e.AdvisorId).HasName("PK__Advisor__D008127564E6E11D");

            entity.Property(e => e.AdvisorId).ValueGeneratedNever();

            entity.HasOne(d => d.AdvisorNavigation).WithOne(p => p.Advisor).HasConstraintName("FK_Advisor_User");
        });

        modelBuilder.Entity<Community>(entity =>
        {
            entity.HasKey(e => e.CommunityId).HasName("PK__Communit__938137ADBFBCD4A5");
        });

        modelBuilder.Entity<CommunityMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Communit__4808B8731CCA025A");

            entity.Property(e => e.MessageDate).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Community).WithMany(p => p.CommunityMessages).HasConstraintName("FK_CommunityMessages_Community");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Course__2AA84FF1642011F3");
        });

        modelBuilder.Entity<Form>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Forms__51BCB7CB3905F799");

            entity.Property(e => e.FormDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FormStatus).HasDefaultValue("Pending");

            entity.HasOne(d => d.Advisor).WithMany(p => p.Forms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Forms_Advisor");

            entity.HasOne(d => d.Student).WithMany(p => p.Forms).HasConstraintName("FK_Forms_Student");
        });

        modelBuilder.Entity<GraduationProjectEligibilityForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Graduati__51BCB7CBE8E55720");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form).WithOne(p => p.GraduationProjectEligibilityForm).HasConstraintName("FK_GPEF_Forms");
        });

        modelBuilder.Entity<GraduationStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Graduati__36257A38850E7E89");

            entity.HasOne(d => d.Student).WithOne(p => p.GraduationStatus).HasConstraintName("FK_GradStatus_Student");
        });

        modelBuilder.Entity<MatchingStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Matching__36257A38006B643B");

            entity.HasOne(d => d.Student).WithOne(p => p.MatchingStatus).HasConstraintName("FK_MatchStatus_Student");
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.MeetingId).HasName("PK__Meeting__5C5E6E64C972D919");

            entity.HasOne(d => d.Advisor).WithMany(p => p.Meetings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meeting_Advisor");

            entity.HasOne(d => d.Student).WithMany(p => p.Meetings).HasConstraintName("FK_Meeting_Student");
        });

        modelBuilder.Entity<MeetingForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__MeetingF__51BCB7CB63440374");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form).WithOne(p => p.MeetingForm).HasConstraintName("FK_MeetingForm_Forms");
        });

        modelBuilder.Entity<MeetingMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__MeetingM__4808B87355D678F8");

            entity.Property(e => e.MessageDate).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<NextSemesterCourseSelectionForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__NextSeme__51BCB7CB94209A68");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form).WithOne(p => p.NextSemesterCourseSelectionForm).HasConstraintName("FK_NSCSF_Forms");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__4BA5CE89769D8B41");

            entity.Property(e => e.Date).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Advisor).WithMany(p => p.Notifications).HasConstraintName("FK_Notif_Advisor");

            entity.HasOne(d => d.Student).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Notif_Student");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__4D11D65CB9252DAC");

            entity.Property(e => e.StudentId).ValueGeneratedNever();

            entity.HasOne(d => d.Advisor).WithMany(p => p.Students)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Student_Advisor");
        });

        modelBuilder.Entity<StudyPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__StudyPla__A2942D189C87A415");

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
                        j.HasKey("PlanId", "CourseId").HasName("PK__StudyPla__A03EA9E7CBDDBF21");
                        j.ToTable("StudyPlanCourse");
                        j.IndexerProperty<int>("PlanId").HasColumnName("planID");
                        j.IndexerProperty<string>("CourseId")
                            .HasMaxLength(30)
                            .HasColumnName("courseID");
                    });
        });

        modelBuilder.Entity<StudyPlanMatchingForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__StudyPla__51BCB7CB9BBAF066");

            entity.Property(e => e.FormId).ValueGeneratedNever();

            entity.HasOne(d => d.Form).WithOne(p => p.StudyPlanMatchingForm).HasConstraintName("FK_SPMF_Forms");
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasKey(e => e.TranscriptId).HasName("PK__Transcri__DF577D864B6963E4");

            entity.HasOne(d => d.Student).WithOne(p => p.Transcript).HasConstraintName("FK_Transcript_Student");

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
                        j.HasKey("TranscriptId", "CourseId").HasName("PK__Transcri__DDFDF979D8CBC99C");
                        j.ToTable("TranscriptCourse");
                        j.IndexerProperty<int>("TranscriptId").HasColumnName("transcriptID");
                        j.IndexerProperty<string>("CourseId")
                            .HasMaxLength(30)
                            .HasColumnName("courseID");
                    });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CDF22B76839");
        });

        modelBuilder.Entity<VwMyStudent>(entity =>
        {
            entity.ToView("vw_MyStudents");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);


    
}
