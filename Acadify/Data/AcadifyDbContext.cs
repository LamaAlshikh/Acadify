using System;
using System.Collections.Generic;
using Acadify.DbModels;
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
        => optionsBuilder.UseSqlServer("Server=DESKTOP-6CDP97K;Database=AcadifySeniorProject;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicAdvisingConfirmationForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Academic__51BCB7CB6E5C6130");

            entity.ToTable("AcademicAdvisingConfirmationForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");
            entity.Property(e => e.CoursesCount).HasColumnName("coursesCount");
            entity.Property(e => e.CurrentGpa)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("currentGPA");
            entity.Property(e => e.StudentLevel)
                .HasMaxLength(50)
                .HasColumnName("studentLevel");
            entity.Property(e => e.StudentName)
                .HasMaxLength(120)
                .HasColumnName("studentName");

            entity.HasOne(d => d.Form).WithOne(p => p.AcademicAdvisingConfirmationForm)
                .HasForeignKey<AcademicAdvisingConfirmationForm>(d => d.FormId)
                .HasConstraintName("FK_AACF_Forms");
        });

        modelBuilder.Entity<AcademicCalendar>(entity =>
        {
            entity.HasKey(e => e.CalendarId).HasName("PK__Academic__EE5496D6FF4451B8");

            entity.ToTable("AcademicCalendar");

            entity.Property(e => e.CalendarId).HasColumnName("calendarID");
            entity.Property(e => e.PdfFile)
                .HasMaxLength(255)
                .HasColumnName("pdfFile");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("uploadedAt");
        });

        modelBuilder.Entity<Advisor>(entity =>
        {
            entity.HasKey(e => e.AdvisorId).HasName("PK__Advisor__D0081275C928F858");

            entity.ToTable("Advisor");

            entity.Property(e => e.AdvisorId)
                .ValueGeneratedNever()
                .HasColumnName("advisorID");
            entity.Property(e => e.Department)
                .HasMaxLength(120)
                .HasColumnName("department");

            entity.HasOne(d => d.AdvisorNavigation).WithOne(p => p.Advisor)
                .HasForeignKey<Advisor>(d => d.AdvisorId)
                .HasConstraintName("FK_Advisor_User");
        });

        modelBuilder.Entity<Community>(entity =>
        {
            entity.HasKey(e => e.CommunityId).HasName("PK__Communit__938137AD008A3538");

            entity.ToTable("Community");

            entity.Property(e => e.CommunityId).HasColumnName("communityID");
            entity.Property(e => e.CommunityName)
                .HasMaxLength(100)
                .HasColumnName("communityName");
        });

        modelBuilder.Entity<CommunityMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Communit__4808B8739E5C70DA");

            entity.Property(e => e.MessageId).HasColumnName("messageID");
            entity.Property(e => e.CommunityId).HasColumnName("communityID");
            entity.Property(e => e.MessageDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("messageDate");
            entity.Property(e => e.MessageText).HasColumnName("messageText");
            entity.Property(e => e.SenderName)
                .HasMaxLength(120)
                .HasColumnName("senderName");

            entity.HasOne(d => d.Community).WithMany(p => p.CommunityMessages)
                .HasForeignKey(d => d.CommunityId)
                .HasConstraintName("FK_CommunityMessages_Community");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Course__2AA84FF1031AEAD5");

            entity.ToTable("Course");

            entity.Property(e => e.CourseId)
                .HasMaxLength(30)
                .HasColumnName("courseID");
            entity.Property(e => e.CourseName)
                .HasMaxLength(200)
                .HasColumnName("courseName");
            entity.Property(e => e.GraduationRequirement).HasMaxLength(200);
            entity.Property(e => e.Hours).HasColumnName("hours");
            entity.Property(e => e.Prerequisite)
                .HasMaxLength(200)
                .HasColumnName("prerequisite");
        });

        modelBuilder.Entity<Form>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Forms__51BCB7CB99F24B2A");

            entity.HasIndex(e => e.AdvisorId, "IX_Forms_AdvisorID");

            entity.HasIndex(e => e.StudentId, "IX_Forms_StudentID");

            entity.Property(e => e.FormId).HasColumnName("formID");
            entity.Property(e => e.AdvisorId).HasColumnName("advisorID");
            entity.Property(e => e.AdvisorNotes).HasColumnName("advisorNotes");
            entity.Property(e => e.AutoFilled).HasColumnName("autoFilled");
            entity.Property(e => e.FormDate)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("formDate");
            entity.Property(e => e.FormStatus)
                .HasMaxLength(60)
                .HasDefaultValue("Pending")
                .HasColumnName("formStatus");
            entity.Property(e => e.FormType)
                .HasMaxLength(80)
                .HasColumnName("formType");
            entity.Property(e => e.StudentId).HasColumnName("studentID");

            entity.HasOne(d => d.Advisor).WithMany(p => p.Forms)
                .HasForeignKey(d => d.AdvisorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Forms_Advisor");

            entity.HasOne(d => d.Student).WithMany(p => p.Forms)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Forms_Student");
        });

        modelBuilder.Entity<GraduationProjectEligibilityForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__Graduati__51BCB7CB3D2DDA25");

            entity.ToTable("GraduationProjectEligibilityForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");
            entity.Property(e => e.Eligibility)
                .HasMaxLength(50)
                .HasColumnName("eligibility");
            entity.Property(e => e.RequiredCoursesStatus)
                .HasMaxLength(200)
                .HasColumnName("requiredCoursesStatus");

            entity.HasOne(d => d.Form).WithOne(p => p.GraduationProjectEligibilityForm)
                .HasForeignKey<GraduationProjectEligibilityForm>(d => d.FormId)
                .HasConstraintName("FK_GPEF_Forms");
        });

        modelBuilder.Entity<GraduationStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Graduati__36257A38301E1DDF");

            entity.ToTable("GraduationStatus");

            entity.HasIndex(e => e.StudentId, "UQ__Graduati__4D11D65DC07B0AB5").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("statusID");
            entity.Property(e => e.RemainingHours).HasColumnName("remainingHours");
            entity.Property(e => e.Status)
                .HasMaxLength(80)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("studentID");

            entity.HasOne(d => d.Student).WithOne(p => p.GraduationStatus)
                .HasForeignKey<GraduationStatus>(d => d.StudentId)
                .HasConstraintName("FK_GradStatus_Student");
        });

        modelBuilder.Entity<MatchingStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__Matching__36257A389C0573CC");

            entity.ToTable("MatchingStatus");

            entity.HasIndex(e => e.StudentId, "UQ__Matching__4D11D65D0EC0C3AD").IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("statusID");
            entity.Property(e => e.Status)
                .HasMaxLength(80)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("studentID");

            entity.HasOne(d => d.Student).WithOne(p => p.MatchingStatus)
                .HasForeignKey<MatchingStatus>(d => d.StudentId)
                .HasConstraintName("FK_MatchStatus_Student");
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.MeetingId).HasName("PK__Meeting__5C5E6E64052350D3");

            entity.ToTable("Meeting");

            entity.Property(e => e.MeetingId).HasColumnName("meetingID");
            entity.Property(e => e.AdvisorId).HasColumnName("advisorID");
            entity.Property(e => e.ChatRecord).HasColumnName("chatRecord");
            entity.Property(e => e.EndTime).HasColumnName("endTime");
            entity.Property(e => e.StartTime).HasColumnName("startTime");
            entity.Property(e => e.StudentId).HasColumnName("studentID");

            entity.HasOne(d => d.Advisor).WithMany(p => p.Meetings)
                .HasForeignKey(d => d.AdvisorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meeting_Advisor");

            entity.HasOne(d => d.Student).WithMany(p => p.Meetings)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Meeting_Student");
        });

        modelBuilder.Entity<MeetingForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__MeetingF__51BCB7CB180238FB");

            entity.ToTable("MeetingForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");
            entity.Property(e => e.AdvisorActions).HasColumnName("advisorActions");
            entity.Property(e => e.MeetingEnd).HasColumnName("meetingEnd");
            entity.Property(e => e.MeetingNotes).HasColumnName("meetingNotes");
            entity.Property(e => e.MeetingPurpose)
                .HasMaxLength(200)
                .HasColumnName("meetingPurpose");
            entity.Property(e => e.MeetingStart).HasColumnName("meetingStart");
            entity.Property(e => e.ReferralReason)
                .HasMaxLength(200)
                .HasColumnName("referralReason");
            entity.Property(e => e.ReferredTo)
                .HasMaxLength(200)
                .HasColumnName("referredTo");
            entity.Property(e => e.StudentActions).HasColumnName("studentActions");

            entity.HasOne(d => d.Form).WithOne(p => p.MeetingForm)
                .HasForeignKey<MeetingForm>(d => d.FormId)
                .HasConstraintName("FK_MeetingForm_Forms");
        });

        modelBuilder.Entity<MeetingMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__MeetingM__4808B87309EDB2AA");

            entity.Property(e => e.MessageId).HasColumnName("messageID");
            entity.Property(e => e.MeetingId).HasColumnName("meetingID");
            entity.Property(e => e.MessageDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("messageDate");
            entity.Property(e => e.MessageText).HasColumnName("messageText");
            entity.Property(e => e.SenderName)
                .HasMaxLength(120)
                .HasColumnName("senderName");
        });

        modelBuilder.Entity<NextSemesterCourseSelectionForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__NextSeme__51BCB7CB9E364679");

            entity.ToTable("NextSemesterCourseSelectionForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");
            entity.Property(e => e.GpaChange)
                .HasMaxLength(50)
                .HasColumnName("gpaChange");
            entity.Property(e => e.PrerequisiteViolation)
                .HasMaxLength(200)
                .HasColumnName("prerequisiteViolation");
            entity.Property(e => e.RecommendedCourses).HasColumnName("recommendedCourses");
            entity.Property(e => e.RecommendedHours).HasColumnName("recommendedHours");
            entity.Property(e => e.TrackChoice)
                .HasMaxLength(120)
                .HasColumnName("trackChoice");

            entity.HasOne(d => d.Form).WithOne(p => p.NextSemesterCourseSelectionForm)
                .HasForeignKey<NextSemesterCourseSelectionForm>(d => d.FormId)
                .HasConstraintName("FK_NSCSF_Forms");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__4BA5CE894A5EA9FB");

            entity.ToTable("Notification");

            entity.HasIndex(e => e.StudentId, "IX_Notif_StudentID");

            entity.Property(e => e.NotificationId).HasColumnName("notificationID");
            entity.Property(e => e.AdvisorId).HasColumnName("advisorID");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("date");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.StudentId).HasColumnName("studentID");
            entity.Property(e => e.Type)
                .HasMaxLength(60)
                .HasColumnName("type");

            entity.HasOne(d => d.Advisor).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.AdvisorId)
                .HasConstraintName("FK_Notif_Advisor");

            entity.HasOne(d => d.Student).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Notif_Student");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__4D11D65C5268B985");

            entity.ToTable("Student");

            entity.HasIndex(e => e.AdvisorId, "IX_Student_AdvisorID");

            entity.Property(e => e.StudentId)
                .ValueGeneratedNever()
                .HasColumnName("studentID");
            entity.Property(e => e.AdvisorId).HasColumnName("advisorID");
            entity.Property(e => e.CohortYear).HasColumnName("cohortYear");
            entity.Property(e => e.CompletedHours).HasColumnName("completedHours");
            entity.Property(e => e.Level)
                .HasMaxLength(50)
                .HasColumnName("level");
            entity.Property(e => e.Major)
                .HasMaxLength(120)
                .HasColumnName("major");
            entity.Property(e => e.Name).HasMaxLength(120);

            entity.HasOne(d => d.Advisor).WithMany(p => p.Students)
                .HasForeignKey(d => d.AdvisorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Student_Advisor");
        });

        modelBuilder.Entity<StudyPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__StudyPla__A2942D18F1FD0385");

            entity.ToTable("StudyPlan");

            entity.Property(e => e.PlanId).HasColumnName("planID");
            entity.Property(e => e.Major)
                .HasMaxLength(120)
                .HasColumnName("major");
            entity.Property(e => e.PdfFile)
                .HasMaxLength(255)
                .HasColumnName("pdfFile");
            entity.Property(e => e.TotalHours).HasColumnName("totalHours");

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
                        j.HasKey("PlanId", "CourseId").HasName("PK__StudyPla__A03EA9E751641F9F");
                        j.ToTable("StudyPlanCourse");
                        j.IndexerProperty<int>("PlanId").HasColumnName("planID");
                        j.IndexerProperty<string>("CourseId")
                            .HasMaxLength(30)
                            .HasColumnName("courseID");
                    });
        });

        modelBuilder.Entity<StudyPlanMatchingForm>(entity =>
        {
            entity.HasKey(e => e.FormId).HasName("PK__StudyPla__51BCB7CBB34A7238");

            entity.ToTable("StudyPlanMatchingForm");

            entity.Property(e => e.FormId)
                .ValueGeneratedNever()
                .HasColumnName("formID");
            entity.Property(e => e.EarnedHours).HasColumnName("earnedHours");
            entity.Property(e => e.GraduationStatus)
                .HasMaxLength(80)
                .HasColumnName("graduationStatus");
            entity.Property(e => e.RegisteredHours).HasColumnName("registeredHours");
            entity.Property(e => e.RemainingHours).HasColumnName("remainingHours");
            entity.Property(e => e.RequiredHours).HasColumnName("requiredHours");

            entity.HasOne(d => d.Form).WithOne(p => p.StudyPlanMatchingForm)
                .HasForeignKey<StudyPlanMatchingForm>(d => d.FormId)
                .HasConstraintName("FK_SPMF_Forms");
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasKey(e => e.TranscriptId).HasName("PK__Transcri__DF577D86B0C5F5A3");

            entity.ToTable("Transcript");

            entity.HasIndex(e => e.StudentId, "UQ__Transcri__4D11D65D923B0AED").IsUnique();

            entity.Property(e => e.TranscriptId).HasColumnName("transcriptID");
            entity.Property(e => e.ExtractedCourses).HasColumnName("extractedCourses");
            entity.Property(e => e.ExtractedInfo).HasColumnName("extractedInfo");
            entity.Property(e => e.Gpa)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("GPA");
            entity.Property(e => e.PdfFile)
                .HasMaxLength(300)
                .HasColumnName("pdfFile");
            entity.Property(e => e.SemesterGpa)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("semesterGPA");
            entity.Property(e => e.StudentId).HasColumnName("studentID");

            entity.HasOne(d => d.Student).WithOne(p => p.Transcript)
                .HasForeignKey<Transcript>(d => d.StudentId)
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
                        j.HasKey("TranscriptId", "CourseId").HasName("PK__Transcri__DDFDF97953D4FFD7");
                        j.ToTable("TranscriptCourse");
                        j.IndexerProperty<int>("TranscriptId").HasColumnName("transcriptID");
                        j.IndexerProperty<string>("CourseId")
                            .HasMaxLength(30)
                            .HasColumnName("courseID");
                    });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CDF08544AC2");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__AB6E6164034B6D9D").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
        });

        modelBuilder.Entity<VwMyStudent>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MyStudents");

            entity.Property(e => e.AdvisorId).HasColumnName("advisorID");
            entity.Property(e => e.CohortYear).HasColumnName("cohortYear");
            entity.Property(e => e.GraduationStatus)
                .HasMaxLength(80)
                .HasColumnName("graduationStatus");
            entity.Property(e => e.MatchingStatus)
                .HasMaxLength(80)
                .HasColumnName("matchingStatus");
            entity.Property(e => e.StudentId).HasColumnName("studentID");
            entity.Property(e => e.StudentName)
                .HasMaxLength(120)
                .HasColumnName("studentName");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
