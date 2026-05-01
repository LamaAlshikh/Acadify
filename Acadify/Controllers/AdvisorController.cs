using Acadify.Models;
using Db = Acadify.Models.Db;
using Acadify.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Acadify.Controllers
{
    public class AdvisorController : Controller
    {
        private readonly Db.AcadifyDbContext _context; private readonly IWebHostEnvironment _env; private readonly AiSummaryService _aiSummaryService;

        private const string Form1SessionKey = "Form1Draft";
        private const string Form4SessionKey = "Form4Draft";
        private const string SelectedStudentSessionKey = "SelectedStudentId";

        public AdvisorController(
            Db.AcadifyDbContext context,
            IWebHostEnvironment env,
            AiSummaryService aiSummaryService)
        {
            _context = context;
            _env = env;
            _aiSummaryService = aiSummaryService;
        }

        /* ========================================================
                            Shared Helpers
           ======================================================== */

        private int? GetCurrentAdvisorId()
        {
            return HttpContext.Session.GetInt32("AdvisorId");
        }
        private async Task<bool> HasUploadedTranscriptAsync(int studentId)
        {
            return await _context.Transcripts
                .AnyAsync(t => t.StudentId == studentId &&
                               !string.IsNullOrWhiteSpace(t.PdfFile));
        }
        private int? GetSelectedStudentId()
        {
            return HttpContext.Session.GetInt32(SelectedStudentSessionKey);
        }

        private void SetSelectedStudentId(int studentId)
        {
            HttpContext.Session.SetInt32(SelectedStudentSessionKey, studentId);
        }

        private int ResolveStudentId(int? studentId = null)
        {
            if (studentId.HasValue && studentId.Value > 0)
            {
                SetSelectedStudentId(studentId.Value);
                return studentId.Value;
            }

            var selectedStudentId = GetSelectedStudentId();
            if (selectedStudentId.HasValue && selectedStudentId.Value > 0)
                return selectedStudentId.Value;

            // Temporary fallback for prototype pages that are opened directly.
            return 2210783;
        }

        private int ResolveAdvisorId()
        {
            return GetCurrentAdvisorId() ?? 1;
        }

        private static string GetMatchStatusText(object? matchingStatus)
        {
            if (matchingStatus == null)
                return "not matched";

            var type = matchingStatus.GetType();

            var propNames = new[]
            {
            "Status",
            "MatchStatus",
            "MatchingStatus",
            "Result"
        };

            foreach (var propName in propNames)
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(matchingStatus)?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return "matched";
        }

        private static string GetAcademicStatusText(Db.GraduationStatus? graduationStatus)
        {
            if (graduationStatus == null)
                return "Has Remaining Courses";

            if (graduationStatus.RemainingHours <= 0)
                return "Graduated";

            if (graduationStatus.RemainingHours <= 3)
                return "near graduation";

            return "Has Remaining Courses";
        }

        private async Task<Db.Student?> GetAdvisorStudentAsync(int advisorId, int studentId)
        {
            return await _context.Students
                .Include(s => s.GraduationStatus)
                .Include(s => s.MatchingStatus)
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.AdvisorId == advisorId);
        }

        private async Task<int> GetOrCreateLatestForm5ForStudentAsync(int studentId, int advisorId)
        {
            var latestForm5 = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == "Form 5")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm5 == null)
            {
                latestForm5 = new Db.Form
                {
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    FormType = "Form 5",
                    FormDate = DateTime.Now,
                    FormStatus = "Pending",
                    AdvisorNotes = null,
                    AutoFilled = true,
                    AdvisorConfirmation = null
                };

                _context.Forms.Add(latestForm5);
                await _context.SaveChangesAsync();
            }

            var details = await _context.GraduationProjectEligibilityForms
                .FirstOrDefaultAsync(g => g.FormId == latestForm5.FormId);

            if (details == null)
            {
                details = new Db.GraduationProjectEligibilityForm
                {
                    FormId = latestForm5.FormId,
                    Eligibility = null,
                    RequiredCoursesStatus = null
                };

                _context.GraduationProjectEligibilityForms.Add(details);
                await _context.SaveChangesAsync();
            }

            return latestForm5.FormId;
        }

        /* ========================================================
                            Advisor Home
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> AdvisorHome(string? cohort = null)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int? advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return RedirectToAction("Login", "Account");

            var studentsQuery = _context.Students
                .Include(s => s.GraduationStatus)
                .Include(s => s.MatchingStatus)
                .Where(s => s.AdvisorId == advisorId.Value)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(cohort) && int.TryParse(cohort, out int selectedYear))
            {
                studentsQuery = studentsQuery.Where(s => s.CohortYear == selectedYear);
            }

            var studentsFromDb = await studentsQuery
                .OrderByDescending(s => s.CohortYear)
                .ThenBy(s => s.Name)
                .ToListAsync();

            var students = studentsFromDb.Select(s => new AdvisorHomeStudentVM
            {
                StudentId = s.StudentId,
                StudentName = s.Name,
                CohortYear = s.CohortYear ?? 0,
                AcademicStatus = GetAcademicStatusText(s.GraduationStatus),
                MatchStatus = GetMatchStatusText(s.MatchingStatus),
                ImagePath = "~/images/user.png"
            }).ToList();

            return View(students);
        }

        /* ========================================================
                            Student Forms
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> StudentForms(int? studentId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int? advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return RedirectToAction("Login", "Account");

            int resolvedStudentId = ResolveStudentId(studentId);

            var student = await GetAdvisorStudentAsync(advisorId.Value, resolvedStudentId);
            if (student == null)
                return NotFound("Student was not found for this advisor.");

            ViewBag.StudentId = student.StudentId;
            ViewBag.StudentName = student.Name;

            bool hasTranscript = await HasUploadedTranscriptAsync(student.StudentId);

            if (!hasTranscript)
            {
                ViewBag.TranscriptMissing = true;
                ViewBag.TranscriptMessage = "The student has not uploaded the transcript yet.";

                return View(new List<StudentFormsVM>());
            }

            var forms = new List<StudentFormsVM>
{
    new StudentFormsVM
    {
        FormId = 1,
        FormTitle = "Academic Advising Confirmation",
        FormType = "Form1",
        CanSend = true
    },
    new StudentFormsVM
    {
        FormId = 2,
        FormTitle = "Next Semester Course Selection",
        FormType = "Form2",
        CanSend = true
    },
    new StudentFormsVM
    {
        FormId = 3,
        FormTitle = "Meeting Record Form",
        FormType = "Form3",
        CanSend = true
    },
    new StudentFormsVM
    {
        FormId = 4,
        FormTitle = "Study Plan Matching",
        FormType = "Form4",
        CanSend = true
    },
    new StudentFormsVM
    {
        FormId = 5,
        FormTitle = "Graduation Project Eligibility",
        FormType = "Form5",
        CanSend = true
    }
};

            return View(forms);
        }

        public IActionResult TestDb()
        {
            var count = _context.Students.Count();
            return Content("عدد الطلاب: " + count);
        }

        [HttpGet]
        public async Task<IActionResult> ViewForm(int? studentId, int formId)
        {
            int resolvedStudentId = ResolveStudentId(studentId);
            int advisorId = ResolveAdvisorId();

            switch (formId)
            {
                case 1:
                    return RedirectToAction(nameof(Form1), new { studentId = resolvedStudentId });

                case 2:
                    return RedirectToAction(nameof(Form2), new { studentId = resolvedStudentId });

                case 3:
                    return RedirectToAction(nameof(Form3), new { studentId = resolvedStudentId });

                case 4:
                    return RedirectToAction(nameof(Form4), new { studentId = resolvedStudentId });

                case 5:
                    int form5Id = await GetOrCreateLatestForm5ForStudentAsync(resolvedStudentId, advisorId);
                    return RedirectToAction("Form5", "GraduationProjectEligibility", new { formId = form5Id });

                default:
                    return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintForm(int? studentId, int formId)
        {
            int resolvedStudentId = ResolveStudentId(studentId);
            int advisorId = ResolveAdvisorId();

            if (formId == 5)
            {
                int form5Id = await GetOrCreateLatestForm5ForStudentAsync(resolvedStudentId, advisorId);
                return RedirectToAction("Form5", "GraduationProjectEligibility", new { formId = form5Id });
            }

            return RedirectToAction(nameof(ViewForm), new { studentId = resolvedStudentId, formId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendForm(int studentId, int formId)
        {
            int advisorId = ResolveAdvisorId();
            SetSelectedStudentId(studentId);

            if (formId == 5)
            {
                int form5Id = await GetOrCreateLatestForm5ForStudentAsync(studentId, advisorId);

                var form = await _context.Forms.FirstOrDefaultAsync(f => f.FormId == form5Id);
                if (form != null)
                {
                    form.FormStatus = "Sent";
                    form.FormDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = $"Form {formId} sent successfully.";
            return RedirectToAction(nameof(StudentForms), new { studentId });
        }

        [HttpGet]
        public IActionResult StudentFormsByStudent(int studentId)
        {
            SetSelectedStudentId(studentId);
            return RedirectToAction(nameof(StudentForms), new { studentId });
        }

        public IActionResult RequestMeeting(int studentId)
        {
            SetSelectedStudentId(studentId);
            return Content($"Meeting page for student {studentId}");
        }

        /* ========================================================
                            Community Advisor
           ======================================================== */

        public IActionResult CommunityAdvisor()
        {
            var model = new CommunityAdvisorVM
            {
                Messages = new List<CommunityMessageVM>
            {
                new CommunityMessageVM
                {
                    SenderName = "Lina Alrwaily",
                    SenderInitials = "LA",
                    MessageText = "السلام عليكم دكتورة أمينة",
                    IsAdvisorMessage = false,
                    BubbleColorClass = "msg-blue"
                },
                new CommunityMessageVM
                {
                    SenderName = "Lina Alrwaily",
                    SenderInitials = "LA",
                    MessageText = "هل أقدر أنزل مادة تطوير برمجيات الترم الجاي",
                    IsAdvisorMessage = false,
                    BubbleColorClass = "msg-blue"
                },
                new CommunityMessageVM
                {
                    SenderName = "Rahaf Alghamdi",
                    SenderInitials = "RA",
                    MessageText = "ايوه دكتورة حتى أنا",
                    IsAdvisorMessage = false,
                    BubbleColorClass = "msg-pink"
                },
                new CommunityMessageVM
                {
                    SenderName = "Amina Gamlo (me)",
                    SenderInitials = "AG",
                    MessageText = "وعليكم السلام و رحمة الله و بركاته\nليش ما تبغو تنزلوها هذا الترم؟",
                    IsAdvisorMessage = true,
                    BubbleColorClass = "msg-purple"
                },
                new CommunityMessageVM
                {
                    SenderName = "Lama Alshaikh",
                    SenderInitials = "LA",
                    MessageText = "عندي استفسار بخصوص التدريب",
                    IsAdvisorMessage = false,
                    BubbleColorClass = "msg-indigo"
                }
            },
                Members = new List<CommunityMemberVM>
            {
                new CommunityMemberVM { Name = "Lma Alshaikh", ImagePath = "~/images/user.png" },
                new CommunityMemberVM { Name = "Lina Alrwaily", ImagePath = "~/images/user.png" },
                new CommunityMemberVM { Name = "Rahaf Alghamdi", ImagePath = "~/images/user.png" },
                new CommunityMemberVM { Name = "Rahaf Alzahrani", ImagePath = "~/images/user.png" }
            }
            };

            return View(model);
        }

        /* ========================================================
                            Form 1
           ======================================================== */

        [HttpGet]
        public IActionResult Form1(int? studentId = null)
        {
            if (studentId.HasValue && studentId.Value > 0)
                SetSelectedStudentId(studentId.Value);

            var json = HttpContext.Session.GetString(Form1SessionKey);

            Form1ViewModel model;

            if (!string.IsNullOrEmpty(json))
            {
                model = JsonSerializer.Deserialize<Form1ViewModel>(json) ?? CreateNewForm1();
            }
            else
            {
                model = CreateNewForm1();
            }

            if (studentId.HasValue)
                model.StudentId = studentId.Value.ToString();

            return View(model);
        }

        [HttpPost]
        public IActionResult SaveForm1(Form1ViewModel model)
        {
            model.Status = "Draft";
            SaveForm1ToSession(model);

            TempData["Success"] = "Form 1 saved successfully.";
            return RedirectToAction(nameof(Form1), new { studentId = GetSelectedStudentId() });
        }

        [HttpPost]
        public IActionResult SendForm1(Form1ViewModel model)
        {
            model.Status = "Sent";
            SaveForm1ToSession(model);

            TempData["Success"] = "Form 1 sent successfully.";
            return RedirectToAction(nameof(Form1), new { studentId = GetSelectedStudentId() });
        }

        private void SaveForm1ToSession(Form1ViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            HttpContext.Session.SetString(Form1SessionKey, json);
        }

        private Form1ViewModel CreateNewForm1()
        {
            return new Form1ViewModel
            {
                FullName = "Lama Zaki Alshikh",
                StudentId = "22190123",
                AdvisorName = "Dr. Amina Gamlo",
                Email = "lalshikh@stu.kau.edu.sa",
                ApprovalDate = DateTime.Today,
                AdvisingCommencementDate = DateTime.Today,
                Status = "Draft"
            };
        }

        /* ========================================================
                            Form 2
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> Form2(int? studentId = null)
        {
            int resolvedStudentId = ResolveStudentId(studentId);

            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == resolvedStudentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null || latestForm.CourseChoiceMonitoringForm == null)
                return NotFound("Form 2 not found.");

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == resolvedStudentId);

            if (student == null)
                return NotFound("Student not found.");

            var form2 = latestForm.CourseChoiceMonitoringForm;

            var selectedCourses = string.IsNullOrWhiteSpace(form2.SelectedCoursesJson)
                ? new List<SelectedCourseVM>()
                : JsonSerializer.Deserialize<List<SelectedCourseVM>>(form2.SelectedCoursesJson) ?? new List<SelectedCourseVM>();

            var model = new Form2ViewModel
            {
                StudentName = student.Name,
                StudentId = student.StudentId,
                Semester = form2.Semester ?? "",
                ComingSemester = form2.ComingSemester ?? "",
                RunningCreditHours = form2.RunningCreditHours ?? 0,
                AdvisedCreditHours = form2.AdvisedCreditHours ?? 0,
                Level = form2.Level ?? "",
                DropSubjects = form2.DropSubjects ?? "",
                ICSubjects = form2.ICSubjects ?? "",
                IPSubjects = form2.IpSubjects ?? "",
                SelectedCourses = selectedCourses
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveForm2(Form2ViewModel model)
        {
            await UpdateForm2Async(model, "Draft");

            TempData["SuccessMessage"] = "Form 2 saved successfully.";
            return RedirectToAction(nameof(Form2), new { studentId = model.StudentId });
        }

        [HttpPost]
        public async Task<IActionResult> SendForm2(Form2ViewModel model)
        {
            await UpdateForm2Async(model, "Sent to Advising Committee");

            TempData["SuccessMessage"] = "Form 2 sent to the advising committee successfully.";
            return RedirectToAction(nameof(Form2), new { studentId = model.StudentId });
        }

        [HttpPost]
        public async Task<IActionResult> ReturnForm2ToStudent(Form2ViewModel model)
        {
            await UpdateForm2Async(model, "Returned to Student");

            TempData["SuccessMessage"] = "Form 2 returned to the student successfully.";
            return RedirectToAction(nameof(Form2), new { studentId = model.StudentId });
        }

        private async Task UpdateForm2Async(Form2ViewModel model, string status)
        {
            int studentId = model.StudentId;

            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null || latestForm.CourseChoiceMonitoringForm == null)
                throw new InvalidOperationException("Form 2 not found.");

            var form2 = latestForm.CourseChoiceMonitoringForm;

            form2.Semester = model.Semester;
            form2.ComingSemester = model.ComingSemester;
            form2.RunningCreditHours = model.RunningCreditHours;
            form2.AdvisedCreditHours = model.AdvisedCreditHours;
            form2.Level = model.Level;
            form2.DropSubjects = model.DropSubjects;
            form2.ICSubjects = model.ICSubjects;
            form2.IpSubjects = model.IPSubjects;
            form2.SelectedCoursesJson = JsonSerializer.Serialize(model.SelectedCourses ?? new List<SelectedCourseVM>());

            latestForm.FormStatus = status;
            latestForm.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        /* ========================================================
                            Form 3
           ======================================================== */

        [HttpGet]
        public IActionResult Form3(int? studentId = null)
        {
            if (studentId.HasValue && studentId.Value > 0)
                SetSelectedStudentId(studentId.Value);

            ViewBag.StudentId = studentId ?? GetSelectedStudentId();

            int meetingId = 1; // temporary until meetings are fully connected

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                return View(CreateEmptyForm3ViewModel());

            var existingForm = _context.Forms
                .Include(f => f.MeetingForm)
                .FirstOrDefault(f => f.FormType == "Form 3"
                                  && f.MeetingForm != null
                                  && f.MeetingForm.MeetingId == meetingId);

            var model = CreateEmptyForm3ViewModel();
            model.Status = existingForm?.FormStatus ?? "Draft";
            model.AdvisorNotes = existingForm?.AdvisorNotes ?? "";

            var row1 = model.Meetings[0];

            if (existingForm?.MeetingForm != null)
            {
                var mf = existingForm.MeetingForm;

                if (mf.MeetingStart.HasValue)
                    row1.MeetingDate = mf.MeetingStart.Value.ToString("dd/MM/yyyy hh:mm tt");

                row1.PurposeAcademic = mf.MeetingPurpose == "Academic";
                row1.PurposeCareer = mf.MeetingPurpose == "Career";
                row1.PurposeOther = mf.MeetingPurpose == "Other";
                row1.ReferralName = mf.ReferredTo ?? "";
                row1.ReferralReason = mf.ReferralReason ?? "";
                row1.ProposedSolutions = mf.MeetingNotes ?? "";
            }

            if (string.IsNullOrWhiteSpace(row1.MeetingDate) && meeting.RecordingStartedAt.HasValue)
                row1.MeetingDate = meeting.RecordingStartedAt.Value.ToString("dd/MM/yyyy hh:mm tt");

            if (string.IsNullOrWhiteSpace(row1.ProposedSolutions) && !string.IsNullOrWhiteSpace(meeting.ChatSummary))
                row1.ProposedSolutions = meeting.ChatSummary;

            return View(model);
        }

        [HttpPost]
        public IActionResult SaveForm3(Form3ViewModel model)
        {
            SaveOrSendForm3(model, "Draft");
            TempData["Success"] = "Form 3 saved successfully.";
            return RedirectToAction(nameof(Form3), new { studentId = GetSelectedStudentId() });
        }

        [HttpPost]
        public IActionResult SendForm3(Form3ViewModel model)
        {
            SaveOrSendForm3(model, "Sent");
            TempData["Success"] = "Form 3 sent successfully.";
            return RedirectToAction(nameof(Form3), new { studentId = GetSelectedStudentId() });
        }

        [HttpPost]
        public IActionResult AddNotesForm3(string notes)
        {
            TempData["Success"] = "Notes saved successfully.";
            return RedirectToAction(nameof(Form3), new { studentId = GetSelectedStudentId() });
        }

        private Form3ViewModel CreateEmptyForm3ViewModel()
        {
            var model = new Form3ViewModel
            {
                StudentName = "",
                StudentId = "",
                Status = "Draft",
                AdvisorNotes = "",
                Meetings = new List<Form3MeetingRowVM>()
            };

            for (int i = 1; i <= 3; i++)
            {
                model.Meetings.Add(new Form3MeetingRowVM
                {
                    MeetingNo = i,
                    MeetingDate = "",
                    PurposeAcademic = false,
                    PurposeCareer = false,
                    PurposeOther = false,
                    ReferralName = "",
                    ReferralReason = "",
                    ProposedSolutions = "",
                    StudentInitial = "",
                    AdvisorInitial = ""
                });
            }

            return model;
        }

        private void SaveOrSendForm3(Form3ViewModel model, string status)
        {
            int meetingId = 1; // temporary until meetings are fully connected

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                throw new InvalidOperationException("Meeting not found.");

            var form = _context.Forms
                .Include(f => f.MeetingForm)
                .FirstOrDefault(f => f.FormType == "Form 3"
                                  && f.MeetingForm != null
                                  && f.MeetingForm.MeetingId == meetingId);

            if (form == null)
            {
                form = new Db.Form
                {
                    StudentId = meeting.StudentId,
                    AdvisorId = meeting.AdvisorId,
                    FormType = "Form 3",
                    FormDate = DateTime.Now,
                    FormStatus = status,
                    AdvisorNotes = model.AdvisorNotes,
                    AutoFilled = true,
                    AdvisorConfirmation = status == "Sent"
                };

                _context.Forms.Add(form);
                _context.SaveChanges();

                form.MeetingForm = new Db.MeetingForm
                {
                    FormId = form.FormId,
                    MeetingId = meetingId
                };

                _context.MeetingForms.Add(form.MeetingForm);
            }

            form.FormDate = DateTime.Now;
            form.FormStatus = status;
            form.AdvisorNotes = model.AdvisorNotes;
            form.AdvisorConfirmation = status == "Sent";

            var row1 = model.Meetings.FirstOrDefault();

            if (row1 != null && form.MeetingForm != null)
            {
                if (DateTime.TryParseExact(
                        row1.MeetingDate,
                        "dd/MM/yyyy hh:mm tt",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedMeetingStart))
                {
                    form.MeetingForm.MeetingStart = parsedMeetingStart;
                }
                else
                {
                    form.MeetingForm.MeetingStart = meeting.RecordingStartedAt;
                }

                form.MeetingForm.MeetingEnd = meeting.RecordingStoppedAt;

                if (row1.PurposeAcademic)
                    form.MeetingForm.MeetingPurpose = "Academic";
                else if (row1.PurposeCareer)
                    form.MeetingForm.MeetingPurpose = "Career";
                else if (row1.PurposeOther)
                    form.MeetingForm.MeetingPurpose = "Other";
                else
                    form.MeetingForm.MeetingPurpose = null;

                form.MeetingForm.ReferredTo = row1.ReferralName;
                form.MeetingForm.ReferralReason = row1.ReferralReason;
                form.MeetingForm.MeetingNotes = row1.ProposedSolutions;
                form.MeetingForm.MeetingId = meetingId;
            }

            _context.SaveChanges();
        }

        [HttpGet]
        public async Task<IActionResult> Form3History()
        {
            int meetingId = 1; // temporary

            var items = await _context.Forms
                .Include(f => f.MeetingForm)
                .Where(f => f.FormType == "Form 3"
                         && f.MeetingForm != null
                         && f.MeetingForm.MeetingId == meetingId)
                .OrderByDescending(f => f.FormDate)
                .Select(f => new FormHistoryItemViewModel
                {
                    FormId = f.FormId,
                    FormTitle = "Meeting Record (Form 3)",
                    Status = f.FormStatus,
                    DateText = f.FormDate.ToString("MMM d, yyyy"),
                    ViewUrl = Url.Action("ViewSavedForm3", "Advisor", new { formId = f.FormId })!
                })
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public IActionResult ViewSavedForm3(int formId)
        {
            var form = _context.Forms
                .Include(f => f.MeetingForm)
                .FirstOrDefault(f => f.FormId == formId && f.FormType == "Form 3");

            if (form == null || form.MeetingForm == null)
                return NotFound();

            var model = CreateEmptyForm3ViewModel();
            model.Status = form.FormStatus ?? "Draft";
            model.AdvisorNotes = form.AdvisorNotes ?? "";

            var row1 = model.Meetings[0];
            var mf = form.MeetingForm;

            if (mf.MeetingStart.HasValue)
                row1.MeetingDate = mf.MeetingStart.Value.ToString("dd/MM/yyyy hh:mm tt");

            row1.PurposeAcademic = mf.MeetingPurpose == "Academic";
            row1.PurposeCareer = mf.MeetingPurpose == "Career";
            row1.PurposeOther = mf.MeetingPurpose == "Other";
            row1.ReferralName = mf.ReferredTo ?? "";
            row1.ReferralReason = mf.ReferralReason ?? "";
            row1.ProposedSolutions = mf.MeetingNotes ?? "";

            return View("Form3", model);
        }

        /* ========================================================
                            Form 4
           ======================================================== */

        [HttpGet]
        public async Task<IActionResult> Form4(int? studentId = null)
        {
            int resolvedStudentId = ResolveStudentId(studentId);

            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == resolvedStudentId);

            if (student == null)
                return NotFound("Student not found.");

            var model = BuildForm4ViewModel(student);
            model.PlanCourseOptions = GetIsPlanCourseOptions();

            var latestForm = await _context.Forms
                .Where(f => f.StudentId == resolvedStudentId && f.FormType == "Form 4")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm != null)
                model.AdvisorNotes = latestForm.AdvisorNotes ?? "";

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveForm4(Form4ViewModel model)
        {
            int studentId = ResolveStudentId(ParseNullableInt(model.StudentId));
            int advisorId = ResolveAdvisorId();

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);
            await SaveForm4ToDatabaseAsync(model, studentId, advisorId, "Draft", false);

            TempData["Success"] = "Form 4 saved successfully.";
            return RedirectToAction(nameof(Form4), new { studentId });
        }

        [HttpPost]
        public async Task<IActionResult> SendForm4(Form4ViewModel model)
        {
            int studentId = ResolveStudentId(ParseNullableInt(model.StudentId));
            int advisorId = ResolveAdvisorId();

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);
            await SaveForm4ToDatabaseAsync(model, studentId, advisorId, "Sent", true);

            TempData["Success"] = "Form 4 sent successfully.";
            return RedirectToAction(nameof(Form4), new { studentId });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveFreeCourses(Form4ViewModel model)
        {
            int studentId = ResolveStudentId(ParseNullableInt(model.StudentId));

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);

            TempData["Success"] = "Free courses updated successfully.";
            return RedirectToAction(nameof(Form4), new { studentId });
        }

        private async Task SaveForm4ToDatabaseAsync(
            Form4ViewModel model,
            int studentId,
            int advisorId,
            string status,
            bool advisorConfirmation)
        {
            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                throw new InvalidOperationException("Student not found.");

            var vm = BuildForm4ViewModel(student);

            var form = new Db.Form
            {
                StudentId = studentId,
                AdvisorId = advisorId,
                FormType = "Form 4",
                FormDate = DateTime.Now,
                FormStatus = status,
                AdvisorNotes = model.AdvisorNotes,
                AutoFilled = true,
                AdvisorConfirmation = advisorConfirmation
            };

            _context.Forms.Add(form);
            await _context.SaveChangesAsync();

            var form4 = new Db.StudyPlanMatchingForm
            {
                FormId = form.FormId,
                GraduationStatus = null,
                RemainingHours = Math.Max(140 - vm.EarnedHours, 0),
                RequiredHours = 140,
                EarnedHours = vm.EarnedHours,
                RegisteredHours = null,
                UniversityHours = vm.UniversityReqHours,
                PrepYearHours = vm.PrepYearReqHours,
                FreeCoursesHours = vm.FreeCoursesHours,
                CollegeMandatoryHours = vm.CollegeMandatoryHours,
                DeptMandatoryHours = vm.DeptMandatoryHours,
                DeptElectiveHours = vm.DeptElectiveHours,
                TotalHours = vm.TotalHours
            };

            _context.StudyPlanMatchingForms.Add(form4);
            await _context.SaveChangesAsync();
        }

        private static int? ParseNullableInt(string? value)
        {
            return int.TryParse(value, out var result) ? result : null;
        }

        private void SaveForm4ToSession(Form4ViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            HttpContext.Session.SetString(Form4SessionKey, json);
        }

        private Form4ViewModel CreateNewForm4()
        {
            return new Form4ViewModel
            {
                StudentName = "Lama Alshikh",
                StudentId = "000000000",
                AcademicYear = DateTime.Now.Year.ToString(),
                EarnedHours = 129,
                RegisteredHours = 11,
                UniversityReqHours = 26,
                PrepYearReqHours = 15,
                FreeCoursesHours = 9,
                CollegeMandatoryHours = 24,
                DeptMandatoryHours = 57,
                DeptElectiveHours = 9,
                TotalHours = 140,
                GraduationTermText = "الفصل الدراسي الاول 2024",
                Note1 = "BUS220 و BUS320 تعادل إدارة المنظمات BUS 232",
                Note2 = "BUS230 و BUS335 تعادل إدارة المنظمات BUS 232",
                Note3 = "مادة التسويق تعادل BUS 232",
                Note4 = "تم احتساب 11 ساعة ضمن الفصل القادم",
                AdvisorNameLabel = "المرشدة الأكاديمية للطالبة",
                AdvisorName = "Dr. Amina Gamlo",
                AdvisorSignature = "",
                Status = "Draft"
            };
        }

        [HttpGet]
        public async Task<IActionResult> Form4History()
        {
            int studentId = ResolveStudentId();

            var items = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == "Form 4")
                .OrderByDescending(f => f.FormDate)
                .Select(f => new FormHistoryItemViewModel
                {
                    FormId = f.FormId,
                    FormTitle = "Study Plan Matching (Form 4)",
                    Status = f.FormStatus,
                    DateText = f.FormDate.ToString("MMM d, yyyy"),
                    ViewUrl = Url.Action("ViewSavedForm4", "Advisor", new { formId = f.FormId })!
                })
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> ViewSavedForm4(int formId)
        {
            var form = await _context.Forms
                .Include(f => f.Student)
                    .ThenInclude(s => s.Transcript)
                        .ThenInclude(t => t.Courses)
                .Include(f => f.StudyPlanMatchingForm)
                .FirstOrDefaultAsync(f => f.FormId == formId && f.FormType == "Form 4");

            if (form == null || form.Student == null)
                return NotFound("Form 4 not found.");

            var model = BuildForm4ViewModel(form.Student);
            model.AdvisorNotes = form.AdvisorNotes ?? "";
            model.PlanCourseOptions = GetIsPlanCourseOptions();

            if (form.StudyPlanMatchingForm != null)
            {
                model.EarnedHours = form.StudyPlanMatchingForm.EarnedHours ?? model.EarnedHours;
                model.UniversityReqHours = form.StudyPlanMatchingForm.UniversityHours ?? model.UniversityReqHours;
                model.PrepYearReqHours = form.StudyPlanMatchingForm.PrepYearHours ?? model.PrepYearReqHours;
                model.FreeCoursesHours = form.StudyPlanMatchingForm.FreeCoursesHours ?? model.FreeCoursesHours;
                model.CollegeMandatoryHours = form.StudyPlanMatchingForm.CollegeMandatoryHours ?? model.CollegeMandatoryHours;
                model.DeptMandatoryHours = form.StudyPlanMatchingForm.DeptMandatoryHours ?? model.DeptMandatoryHours;
                model.DeptElectiveHours = form.StudyPlanMatchingForm.DeptElectiveHours ?? model.DeptElectiveHours;
                model.TotalHours = form.StudyPlanMatchingForm.TotalHours ?? model.TotalHours;
            }

            return View("Form4", model);
        }

        private string ExtractLastAcademicTerm(string? extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return "غير محدد";

            var matches = Regex.Matches(
                extractedInfo,
                @"\b(FALL|SPRING|SUMMER|WINTER)\s+\d{4}/\d{4}\b",
                RegexOptions.IgnoreCase);

            if (matches.Count == 0)
                return "غير محدد";

            return matches[matches.Count - 1].Value.ToUpper();
        }

        private Form4ViewModel BuildForm4ViewModel(Db.Student student)
        {
            var transcript = student.Transcript;
            var courses = transcript?.Courses?.ToList() ?? new List<Db.Course>();

            var electiveCourses = new List<string>
        {
            "CPIS382","CPIS483","CPIS486","CPIS320",
            "CPIS420","CPIS424","CPIS360","CPIS363",
            "CPIS490","CPIS430","CPIS426","CPIS350"
        };

            int universityHours = 0;
            int prepYearHours = 0;
            int collegeMandatoryHours = 0;
            int deptMandatoryHours = 0;
            int deptElectiveHours = 0;
            int freeCoursesHours = 0;

            var pendingCourses = new List<Form4CourseDecisionItemVM>();

            var decisions = _context.TranscriptCourseDecisions
                .Where(d => d.StudentId == student.StudentId)
                .ToList();

            foreach (var course in courses)
            {
                if (course.RequirementCategory == "University")
                {
                    universityHours += course.Hours;
                    continue;
                }

                if (course.RequirementCategory == "PrepYear")
                {
                    prepYearHours += course.Hours;
                    continue;
                }

                if (course.RequirementCategory == "CollegeMandatory")
                {
                    collegeMandatoryHours += course.Hours;
                    continue;
                }

                if (course.RequirementCategory == "DeptMandatory")
                {
                    deptMandatoryHours += course.Hours;
                    continue;
                }

                if (electiveCourses.Contains(course.CourseId))
                {
                    deptElectiveHours += course.Hours;
                    continue;
                }

                var decision = decisions.FirstOrDefault(d => d.TranscriptCourseId == course.CourseId);

                if (decision != null)
                {
                    if (decision.DecisionType == "FreeElective")
                    {
                        freeCoursesHours += course.Hours;
                        continue;
                    }

                    if (decision.DecisionType == "EquivalentToPlan" &&
                        !string.IsNullOrWhiteSpace(decision.EquivalentCourseId))
                    {
                        var equivalentCourse = _context.Courses
                            .FirstOrDefault(c => c.CourseId == decision.EquivalentCourseId);

                        if (equivalentCourse != null)
                        {
                            if (equivalentCourse.RequirementCategory == "University")
                                universityHours += equivalentCourse.Hours;
                            else if (equivalentCourse.RequirementCategory == "PrepYear")
                                prepYearHours += equivalentCourse.Hours;
                            else if (equivalentCourse.RequirementCategory == "CollegeMandatory")
                                collegeMandatoryHours += equivalentCourse.Hours;
                            else if (equivalentCourse.RequirementCategory == "DeptMandatory")
                                deptMandatoryHours += equivalentCourse.Hours;
                            else if (electiveCourses.Contains(equivalentCourse.CourseId))
                                deptElectiveHours += equivalentCourse.Hours;
                            else
                                freeCoursesHours += equivalentCourse.Hours;

                            continue;
                        }
                    }
                }

                pendingCourses.Add(new Form4CourseDecisionItemVM
                {
                    TranscriptCourseId = course.CourseId,
                    TranscriptCourseName = course.CourseName,
                    Hours = course.Hours,
                    DecisionType = "",
                    EquivalentCourseId = null
                });
            }

            int categorizedHours = universityHours
                                 + prepYearHours
                                 + collegeMandatoryHours
                                 + deptMandatoryHours
                                 + deptElectiveHours
                                 + freeCoursesHours;

            int transcriptEarnedHours = ExtractTranscriptEarnedHours(transcript?.ExtractedInfo);

            if (transcriptEarnedHours <= 0)
                transcriptEarnedHours = categorizedHours;

            return new Form4ViewModel
            {
                StudentName = student.Name ?? "",
                StudentId = student.StudentId.ToString(),
                AcademicYear = DateTime.Now.Year.ToString(),
                EarnedHours = transcriptEarnedHours,
                UniversityReqHours = universityHours,
                PrepYearReqHours = prepYearHours,
                FreeCoursesHours = freeCoursesHours,
                CollegeMandatoryHours = collegeMandatoryHours,
                DeptMandatoryHours = deptMandatoryHours,
                DeptElectiveHours = deptElectiveHours,
                TotalHours = transcriptEarnedHours,
                GraduationTermText = ExtractLastAcademicTerm(transcript?.ExtractedInfo),
                AdvisorNameLabel = "المرشدة الأكاديمية للطالبة",
                AdvisorName = "",
                AdvisorNotes = "",
                PendingCourses = pendingCourses
            };
        }

        private int ExtractFreeCourseHoursFromTranscriptPdf(Db.Transcript? transcript, List<string> electiveCourses)
        {
            if (transcript == null || string.IsNullOrWhiteSpace(transcript.PdfFile))
                return 0;

            var freeCourseIds = transcript.Courses?
                .Where(c =>
                    c.RequirementCategory != "University" &&
                    c.RequirementCategory != "PrepYear" &&
                    c.RequirementCategory != "CollegeMandatory" &&
                    c.RequirementCategory != "DeptMandatory" &&
                    !electiveCourses.Contains(c.CourseId))
                .Select(c => c.CourseId)
                .Distinct()
                .ToList() ?? new List<string>();

            if (freeCourseIds.Count == 0)
                return 0;

            var cleanRelativePath = transcript.PdfFile.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, cleanRelativePath);

            if (!System.IO.File.Exists(fullPath))
                return 0;

            var hourMap = ExtractFreeCourseHoursMapFromPdf(fullPath, freeCourseIds);

            int total = 0;
            foreach (var id in freeCourseIds)
            {
                if (hourMap.TryGetValue(id, out var h))
                    total += h;
            }

            return total;
        }

        private Dictionary<string, int> ExtractFreeCourseHoursMapFromPdf(string fullPath, List<string> freeCourseIds)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using var doc = PdfDocument.Open(fullPath);

            foreach (var page in doc.GetPages())
            {
                var words = page.GetWords()
                    .OrderByDescending(w => w.BoundingBox.Bottom)
                    .ThenBy(w => w.BoundingBox.Left)
                    .ToList();

                var lines = GroupWordsIntoLines(words);

                foreach (var line in lines)
                {
                    var tokens = line
                        .OrderBy(w => w.BoundingBox.Left)
                        .Select(w => w.Text.Trim().ToUpperInvariant())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (tokens.Count == 0)
                        continue;

                    foreach (var courseId in freeCourseIds)
                    {
                        if (result.ContainsKey(courseId))
                            continue;

                        if (!TryFindCourseOnLine(tokens, courseId, out var codeIndex))
                            continue;

                        if (TryExtractHourNearCourse(tokens, codeIndex, out var hours))
                            result[courseId] = hours;
                    }
                }
            }

            return result;
        }

        private List<List<UglyToad.PdfPig.Content.Word>> GroupWordsIntoLines(List<UglyToad.PdfPig.Content.Word> words)
        {
            var lines = new List<List<UglyToad.PdfPig.Content.Word>>();

            foreach (var word in words)
            {
                var existingLine = lines.FirstOrDefault(line =>
                    Math.Abs(line[0].BoundingBox.Bottom - word.BoundingBox.Bottom) <= 3.5);

                if (existingLine == null)
                    lines.Add(new List<UglyToad.PdfPig.Content.Word> { word });
                else
                    existingLine.Add(word);
            }

            return lines;
        }

        private bool TryFindCourseOnLine(List<string> tokens, string courseId, out int codeIndex)
        {
            codeIndex = -1;

            var upperCourseId = courseId.ToUpperInvariant();
            var prefix = new string(upperCourseId.TakeWhile(char.IsLetter).ToArray());
            var number = new string(upperCourseId.SkipWhile(char.IsLetter).ToArray());

            for (int i = 0; i < tokens.Count; i++)
            {
                var m = Regex.Match(tokens[i], @"^([A-Z]{3,6})[-]?(\d{3})$");
                if (m.Success)
                {
                    var code = (m.Groups[1].Value + m.Groups[2].Value).ToUpperInvariant();
                    if (code == upperCourseId)
                    {
                        codeIndex = i;
                        return true;
                    }
                }

                if (i < tokens.Count - 1 &&
                    Regex.IsMatch(tokens[i], @"^[A-Z]{3,6}$") &&
                    tokens[i] == prefix &&
                    tokens[i + 1] == number)
                {
                    codeIndex = i;
                    return true;
                }
            }

            return false;
        }

        private bool TryExtractHourNearCourse(List<string> tokens, int codeIndex, out int hours)
        {
            hours = 0;

            var candidates = new List<(int Distance, int Value, bool IsBefore)>();

            for (int i = Math.Max(0, codeIndex - 10); i <= Math.Min(tokens.Count - 1, codeIndex + 10); i++)
            {
                if (i == codeIndex)
                    continue;

                if (Regex.IsMatch(tokens[i], @"^[0-5]$"))
                {
                    int value = int.Parse(tokens[i]);
                    int distance = Math.Abs(i - codeIndex);
                    bool isBefore = i < codeIndex;

                    candidates.Add((distance, value, isBefore));
                }
            }

            if (candidates.Count == 0)
                return false;

            var best = candidates
                .OrderBy(c => c.Distance)
                .ThenBy(c => c.IsBefore ? 0 : 1)
                .ThenBy(c => c.Value == 0 ? 1 : 0)
                .First();

            hours = best.Value;
            return true;
        }

        private int ExtractTranscriptEarnedHours(string? extractedInfo)
        {
            if (string.IsNullOrWhiteSpace(extractedInfo))
                return 0;

            var normalized = Regex.Replace(extractedInfo, @"\s+", " ").Trim();

            var grandTotalMatch = Regex.Match(
                normalized,
                @"(\d+)\s*Grand\s*Total",
                RegexOptions.IgnoreCase);

            if (grandTotalMatch.Success && int.TryParse(grandTotalMatch.Groups[1].Value, out var grandTotal))
                return grandTotal;

            var cumulativeTotalMatch = Regex.Match(
                normalized,
                @"(\d+)\s*Cumulative\s*Total",
                RegexOptions.IgnoreCase);

            if (cumulativeTotalMatch.Success && int.TryParse(cumulativeTotalMatch.Groups[1].Value, out var cumulativeTotal))
                return cumulativeTotal;

            return 0;
        }

        private List<PlanCourseOptionVM> GetIsPlanCourseOptions()
        {
            return _context.StudyPlans
                .Where(p => p.Major != null &&
                            p.Major.Trim().ToUpper() == "INFORMATION SYSTEMS")
                .SelectMany(p => p.Courses)
                .GroupBy(c => new { c.CourseId, c.CourseName })
                .Select(g => new PlanCourseOptionVM
                {
                    CourseId = g.Key.CourseId,
                    CourseName = g.Key.CourseName
                })
                .OrderBy(x => x.CourseId)
                .ToList();
        }

        private bool IsDirectlyClassifiedCourse(Db.Course course, List<string> electiveCourses)
        {
            return course.RequirementCategory == "University"
                || course.RequirementCategory == "PrepYear"
                || course.RequirementCategory == "CollegeMandatory"
                || course.RequirementCategory == "DeptMandatory"
                || electiveCourses.Contains(course.CourseId);
        }

        private async Task SaveCourseDecisionsAsync(int studentId, List<Form4CourseDecisionItemVM> pendingCourses)
        {
            if (pendingCourses == null || pendingCourses.Count == 0)
                return;

            foreach (var item in pendingCourses)
            {
                if (string.IsNullOrWhiteSpace(item.TranscriptCourseId))
                    continue;

                if (string.IsNullOrWhiteSpace(item.DecisionType))
                    continue;

                var existing = await _context.TranscriptCourseDecisions
                    .FirstOrDefaultAsync(x =>
                        x.StudentId == studentId &&
                        x.TranscriptCourseId == item.TranscriptCourseId);

                if (existing == null)
                {
                    existing = new Db.TranscriptCourseDecision
                    {
                        StudentId = studentId,
                        TranscriptCourseId = item.TranscriptCourseId
                    };

                    _context.TranscriptCourseDecisions.Add(existing);
                }

                existing.DecisionType = item.DecisionType;

                existing.EquivalentCourseId = item.DecisionType == "EquivalentToPlan"
                    ? item.EquivalentCourseId
                    : null;

                existing.IsApprovedByAdvisor = true;
            }

            await _context.SaveChangesAsync();
        }

        /* ========================================================
                            Chat + Form 3 Auto Fill
           ======================================================== */

        private Form3AutoFillViewModel BuildForm3AutoFillData(Db.Meeting meeting)
        {
            var model = new Form3AutoFillViewModel
            {
                MeetingId = meeting.MeetingId,
                MeetingDateText = "",
                AutoBriefNotes = ""
            };

            if (meeting.RecordingStartedAt.HasValue)
                model.MeetingDateText = meeting.RecordingStartedAt.Value.ToString("dd/MM/yyyy hh:mm tt");

            if (!string.IsNullOrWhiteSpace(meeting.ChatRecord))
                model.AutoBriefNotes = SummarizeChatRecord(meeting.ChatRecord);

            return model;
        }

        [HttpGet("/Advisor/Form3AutoPreview")]
        public IActionResult Form3AutoPreview()
        {
            int meetingId = 1; // temporary

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                return NotFound();

            var model = BuildForm3AutoFillData(meeting);
            return View(model);
        }

        private string SummarizeChatRecord(string chatRecord)
        {
            if (string.IsNullOrWhiteSpace(chatRecord))
                return "";

            var lines = chatRecord
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var studentText = lines
                .Where(x => x.StartsWith("Student:", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Replace("Student:", "", StringComparison.OrdinalIgnoreCase).Trim())
                .FirstOrDefault();

            var advisorText = lines
                .Where(x => x.StartsWith("Advisor:", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Replace("Advisor:", "", StringComparison.OrdinalIgnoreCase).Trim())
                .FirstOrDefault();

            string summary = "";

            if (!string.IsNullOrWhiteSpace(studentText))
                summary += $"ناقشت الطالبة موضوع: {studentText}. ";

            if (!string.IsNullOrWhiteSpace(advisorText))
                summary += $"وقدمت المرشدة التوجيه التالي: {advisorText}.";

            return summary.Trim();
        }

        [HttpGet("/Advisor/Chat")]
        public IActionResult Chat()
        {
            int meetingId = 1; // temporary

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                return NotFound();

            string recordingMessage = "";

            if (meeting.LastRecordingAction == "started")
                recordingMessage = "The chat recording is started";
            else if (meeting.LastRecordingAction == "stopped")
                recordingMessage = "The chat recording is stopped";

            ViewBag.RecordingMessage = recordingMessage;

            var messages = _context.MeetingMessages
                .Where(m => m.MeetingId == meetingId)
                .OrderBy(m => m.MessageDate)
                .Select(m => new ChatMessageVM
                {
                    SenderName = (m.SenderName ?? "")
                        .Replace("(me)", "", StringComparison.OrdinalIgnoreCase)
                        .Trim(),
                    Text = m.MessageText,
                    IsFromStudent = m.SenderName != null &&
                                    m.SenderName.Trim().ToLower().Contains("lama"),
                    TimeText = m.MessageDate.HasValue ? m.MessageDate.Value.ToString("hh:mm tt") : "",
                    IsRecorded = m.IsRecorded
                })
                .ToList();

            var model = new StudentChatViewModel
            {
                AdvisorName = "DR. Amina Hasan Gamlo",
                StudentName = "Lama Alshaikh",
                IsRecordingStarted = meeting.IsRecordingStarted,
                Messages = messages
            };

            return View(model);
        }

        [HttpPost("/Advisor/StartRecording")]
        public IActionResult StartRecording()
        {
            int meetingId = 1; // temporary

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                return NotFound();

            meeting.IsRecordingStarted = true;
            meeting.LastRecordingAction = "started";
            meeting.RecordingStartedAt = DateTime.Now;
            meeting.RecordingStoppedAt = null;
            meeting.ChatRecord = null;

            _context.SaveChanges();

            return Redirect("/Advisor/Chat");
        }

        [HttpPost("/Advisor/StopRecording")]
        public async Task<IActionResult> StopRecording()
        {
            int meetingId = 1; // temporary

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                return NotFound();

            meeting.IsRecordingStarted = false;
            meeting.LastRecordingAction = "stopped";
            meeting.RecordingStoppedAt = DateTime.Now;

            if (meeting.RecordingStartedAt.HasValue && meeting.RecordingStoppedAt.HasValue)
            {
                var start = meeting.RecordingStartedAt.Value;
                var stop = meeting.RecordingStoppedAt.Value;

                var recordedMessages = _context.MeetingMessages
                    .Where(m => m.MeetingId == meetingId
                                && m.MessageDate.HasValue
                                && m.MessageDate.Value >= start
                                && m.MessageDate.Value <= stop)
                    .OrderBy(m => m.MessageDate)
                    .ToList();

                if (recordedMessages.Any())
                {
                    var lines = recordedMessages.Select(m =>
                    {
                        var sender = (m.SenderName ?? "").Trim().ToLower().Contains("lama")
                            ? "Student"
                            : "Advisor";

                        return $"{sender}: {m.MessageText}";
                    });

                    meeting.ChatRecord = string.Join(Environment.NewLine, lines);

                    if (!string.IsNullOrWhiteSpace(meeting.ChatRecord))
                    {
                        try
                        {
                            var summary = await _aiSummaryService.SummarizeMeetingChatAsync(meeting.ChatRecord);
                            meeting.ChatSummary = summary;
                        }
                        catch
                        {
                            meeting.ChatSummary = null;
                        }
                    }
                }
                else
                {
                    meeting.ChatRecord = null;
                    meeting.ChatSummary = null;
                }
            }
            else
            {
                meeting.ChatRecord = null;
                meeting.ChatSummary = null;
            }

            _context.SaveChanges();

            return Redirect("/Advisor/Chat");
        }

        [HttpPost("/Advisor/SendMessage")]
        public IActionResult SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Redirect("/Advisor/Chat");

            int meetingId = 1; // temporary

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                return NotFound();

            var newMessage = new Db.MeetingMessage
            {
                MeetingId = meetingId,
                SenderName = "Amina Gamlo (me)",
                MessageText = message,
                MessageDate = DateTime.Now,
                IsRecorded = meeting.IsRecordingStarted
            };

            _context.MeetingMessages.Add(newMessage);
            _context.SaveChanges();

            return Redirect("/Advisor/Chat");
        }

        [HttpPost("/Advisor/BuildChatRecord")]
        public IActionResult BuildChatRecord()
        {
            int meetingId = 1; // temporary

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                return NotFound();

            if (!meeting.RecordingStartedAt.HasValue || !meeting.RecordingStoppedAt.HasValue)
            {
                meeting.ChatRecord = null;
                _context.SaveChanges();
                return Redirect("/Advisor/Chat");
            }

            var start = meeting.RecordingStartedAt.Value;
            var stop = meeting.RecordingStoppedAt.Value;

            var recordedMessages = _context.MeetingMessages
                .Where(m => m.MeetingId == meetingId
                            && m.MessageDate.HasValue
                            && m.MessageDate.Value >= start
                            && m.MessageDate.Value <= stop)
                .OrderBy(m => m.MessageDate)
                .ToList();

            if (!recordedMessages.Any())
            {
                meeting.ChatRecord = null;
                _context.SaveChanges();
                return Redirect("/Advisor/Chat");
            }

            var lines = recordedMessages.Select(m =>
            {
                var sender = (m.SenderName ?? "").Trim().ToLower().Contains("lama")
                    ? "Student"
                    : "Advisor";

                return $"{sender}: {m.MessageText}";
            });

            meeting.ChatRecord = string.Join(Environment.NewLine, lines);

            _context.SaveChanges();

            return Redirect("/Advisor/Chat");
        }

        [HttpPost("/Advisor/GenerateChatSummary")]
        public async Task<IActionResult> GenerateChatSummary()
        {
            int meetingId = 1; // temporary

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(meeting.ChatRecord))
            {
                TempData["Success"] = "لا يوجد ChatRecord لتلخيصه.";
                return RedirectToAction(nameof(Form3));
            }

            try
            {
                var summary = await _aiSummaryService.SummarizeMeetingChatAsync(meeting.ChatRecord);

                meeting.ChatSummary = summary;
                _context.SaveChanges();

                TempData["Success"] = "تم توليد الملخص الذكي بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["Success"] = "حدث خطأ أثناء التلخيص: " + ex.Message;
            }

            return RedirectToAction(nameof(Form3));
        }
    }

}