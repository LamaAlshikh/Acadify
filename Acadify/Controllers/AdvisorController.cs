using Acadify.Models;
<<<<<<< HEAD
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
=======
using Acadify.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using Microsoft.AspNetCore.Http;
using Acadify.Models.Db;

>>>>>>> origin_second/rahafgh

namespace Acadify.Controllers
{
    public class AdvisorController : Controller
<<<<<<< HEAD
    {
        private readonly AcadifyDbContext _context;
        private const string Form4SessionKey = "Form4Draft";

        public AdvisorController(AcadifyDbContext context)
        {
            _context = context;
        }

        /* ========================================================
                            Student Forms
           ======================================================== */
        public IActionResult StudentForms()
        {
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
        public IActionResult ViewForm(int formId)
        {
            return Content($"View Form {formId}");
        }

        [HttpGet]
        public IActionResult PrintForm(int formId)
        {
            return Content($"Print Form {formId}");
        }

        [HttpPost]
        public IActionResult SendForm(int formId)
        {
            TempData["SuccessMessage"] = $"Form {formId} sent successfully.";
            return RedirectToAction("StudentForms");
        }

        /* ========================================================
                            Advisor Home
           ======================================================== */
        public IActionResult AdvisorHome(string? cohort = null)
        {
            var students = new List<AdvisorHomeStudentVM>
            {
                new AdvisorHomeStudentVM
                {
                    StudentId = 1,
                    StudentName = "lama alshikh",
                    CohortYear = 2025,
                    AcademicStatus = "near graduation",
                    MatchStatus = "matched",
                    ImagePath = "~/images/user.png"
                },
                new AdvisorHomeStudentVM
                {
                    StudentId = 2,
                    StudentName = "lina alrwaily",
                    CohortYear = 2025,
                    AcademicStatus = "near graduation",
                    MatchStatus = "matched",
                    ImagePath = "~/images/user.png"
                },
                new AdvisorHomeStudentVM
                {
                    StudentId = 3,
                    StudentName = "rahaf alghamdi",
                    CohortYear = 2024,
                    AcademicStatus = "Has Remaining Courses",
                    MatchStatus = "not matched",
                    ImagePath = "~/images/user.png"
                }
            };

            if (!string.IsNullOrWhiteSpace(cohort) && int.TryParse(cohort, out int selectedYear))
            {
                students = students.Where(s => s.CohortYear == selectedYear).ToList();
            }

            return View(students);
        }

        public IActionResult StudentFormsByStudent(int studentId)
        {
            return RedirectToAction("StudentForms", new { studentId = studentId });
        }

        public IActionResult RequestMeeting(int studentId)
        {
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
                    new CommunityMemberVM
                    {
                        Name = "Lma Alshaikh",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Lina Alrwaily",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Rahaf Alghamdi",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Rahaf Alzahrani",
                        ImagePath = "~/images/user.png"
                    }
                }
            };

            return View(model);
        }

        /* ========================================================
                            Form 5 يحدددددددددددققققققققققققققققققققققققققققققققققققققققققققققق 
           ======================================================== */
        [HttpGet]
        public IActionResult Form5()
        {
            var model = new GraduationProjectEligibilityForm
            {
                FormId = 5,
                StudentName = "Lama Zaki Alshikh",
                StudentId = "22190123",
                CPIS351 = true,
                CPIS358 = true,
                CPIS323 = true,
                CPIS360 = false,
                CPIS375 = false,
                CPIS342 = true
            };

            model.IsEligible =
                model.CPIS351 &&
                model.CPIS358 &&
                model.CPIS323 &&
                model.CPIS360 &&
                model.CPIS375 &&
                model.CPIS342;

            return View(model);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int formId, string status)
        {
            return RedirectToAction("Form5");
        }

        /* ========================================================
                            Form 3
           ======================================================== */
        [HttpGet]
        public IActionResult Form3()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SendForm3()
        {
            TempData["Success"] = "Form 3 sent successfully.";
            return RedirectToAction("Form3");
        }

        [HttpPost]
        public IActionResult AddNotesForm3(string notes)
        {
            TempData["Success"] = "Notes saved successfully.";
            return RedirectToAction("Form3");
        }

        [HttpGet]
        public IActionResult Form3History()
        {
            return View();
        }

        /* ========================================================
                            Form 4
           ======================================================== */
        [HttpGet]
        public IActionResult Form4()
        {
            var json = HttpContext.Session.GetString(Form4SessionKey);

            Form4ViewModel model;

            if (!string.IsNullOrEmpty(json))
            {
                model = JsonSerializer.Deserialize<Form4ViewModel>(json) ?? CreateNewForm4();
            }
            else
            {
                model = CreateNewForm4();
=======


    {
        /* form1 did by rahaf gh*/
        // ==============================
        // FORM 1 (Advisor) - Session Draft
        // ==============================
        private const string Form1SessionKey = "Form1Draft";

        // GET: Advisor/Form1
        [HttpGet]
        public IActionResult Form1()
        {
            var json = HttpContext.Session.GetString(Form1SessionKey);

            Form1ViewModel model;

            if (!string.IsNullOrEmpty(json))
            {
                model = JsonSerializer.Deserialize<Form1ViewModel>(json)
                        ?? CreateNewForm1();
            }
            else
            {
                model = CreateNewForm1();
            }

            return View(model);
        }

        // POST: Save Draft
        [HttpPost]
        public IActionResult SaveForm1(Form1ViewModel model)
        {
            model.Status = "Draft";
            SaveForm1ToSession(model);

            TempData["Success"] = "Form 1 saved successfully.";
            return RedirectToAction("Form1");

        }

        // POST: Send
        [HttpPost]
        public IActionResult SendForm1(Form1ViewModel model)
        {
            model.Status = "Sent";
            SaveForm1ToSession(model);

            TempData["Success"] = "Form 1 sent successfully.";
            return RedirectToAction("Form1");
        }

        private void SaveForm1ToSession(Form1ViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            HttpContext.Session.SetString(Form1SessionKey, json);
        }

        // Default data (تجريبي)
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



      



        private readonly AiSummaryService _aiSummaryService;
        private readonly Acadify.Models.Db.AcadifyDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdvisorController(Acadify.Models.Db.AcadifyDbContext context, IWebHostEnvironment env, AiSummaryService aiSummaryService)
        {
            _context = context;
            _env = env;
            _aiSummaryService = aiSummaryService;
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

        private Form4ViewModel BuildForm4ViewModel(Student student)
        {
            var transcript = student.Transcript;
            var courses = transcript?.Courses?.ToList() ?? new List<Course>();

            var electiveCourses = new List<string>
    {
        "CPIS382","CPIS483","CPIS486","CPIS320",
        "CPIS420","CPIS424","CPIS360","CPIS363",
        "CPIS490","CPIS430","CPIS426","CPIS350"
    };

            int universityHours = courses
                .Where(c => c.RequirementCategory == "University")
                .Sum(c => c.Hours);

            int prepYearHours = courses
                .Where(c => c.RequirementCategory == "PrepYear")
                .Sum(c => c.Hours);

            int collegeMandatoryHours = courses
                .Where(c => c.RequirementCategory == "CollegeMandatory")
                .Sum(c => c.Hours);

            int deptMandatoryHours = courses
                .Where(c => c.RequirementCategory == "DeptMandatory")
                .Sum(c => c.Hours);

            var electiveTaken = courses
                .Where(c => electiveCourses.Contains(c.CourseId))
                .Select(c => c.CourseId)
                .Distinct()
                .ToList();

            int electiveHoursRaw = electiveTaken.Count * 3;
            int deptElectiveHours = Math.Min(electiveHoursRaw, 9);
            int extraElective = Math.Max(electiveHoursRaw - 9, 0);

            int freeCoursesHoursFromPdf = ExtractFreeCourseHoursFromTranscriptPdf(transcript, electiveCourses);
            int freeCoursesHours = Math.Min(freeCoursesHoursFromPdf + extraElective, 9);

            // هذا فقط مجموع التصنيفات في الفورم
            int categorizedHours = universityHours
                                 + prepYearHours
                                 + collegeMandatoryHours
                                 + deptMandatoryHours
                                 + deptElectiveHours
                                 + freeCoursesHours;

            // هذا هو الرقم الرسمي من الترانسكربت
            int transcriptEarnedHours = ExtractTranscriptEarnedHours(transcript?.ExtractedInfo);

            // إذا ما قدرنا نقرأه من الترانسكربت، نرجع لمجموع التصنيفات
            if (transcriptEarnedHours <= 0)
                transcriptEarnedHours = categorizedHours;

            return new Form4ViewModel
            {
                StudentName = student.Name ?? "",
                StudentId = student.StudentId.ToString(),
                AcademicYear = DateTime.Now.Year.ToString(),

                // من الترانسكربت
                EarnedHours = transcriptEarnedHours,

                // منطق التصنيفات
                UniversityReqHours = universityHours,
                PrepYearReqHours = prepYearHours,
                FreeCoursesHours = freeCoursesHours,
                CollegeMandatoryHours = collegeMandatoryHours,
                DeptMandatoryHours = deptMandatoryHours,
                DeptElectiveHours = deptElectiveHours,

                // من الترانسكربت أيضًا
                TotalHours = transcriptEarnedHours,

                AdvisorNameLabel = "المرشدة الأكاديمية للطالبة",
                AdvisorName = "",
                AdvisorNotes = ""
            };
        }

        private int ExtractFreeCourseHoursFromTranscriptPdf(Transcript? transcript, List<string> electiveCourses)
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
                {
                    lines.Add(new List<UglyToad.PdfPig.Content.Word> { word });
                }
                else
                {
                    existingLine.Add(word);
                }
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
                // حالة: MRKC323 أو MRKC-323 أو CPIS352
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

                // حالة: MRKC 323 أو CPIS 352
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

            // نبحث في نطاق أوسع حول الكود
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

            // نفضّل:
            // 1) الأقرب
            // 2) اللي قبل الكود
            // 3) غير الصفر أولًا
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


        // =======================
        // helpers
        // =======================
        private Form3AutoFillViewModel BuildForm3AutoFillData(Meeting meeting)
        {
            var model = new Form3AutoFillViewModel
            {
                MeetingId = meeting.MeetingId,
                MeetingDateText = "",
                AutoBriefNotes = ""
            };

            if (meeting.RecordingStartedAt.HasValue)
            {
                model.MeetingDateText = meeting.RecordingStartedAt.Value.ToString("dd/MM/yyyy hh:mm tt");
            }

            if (!string.IsNullOrWhiteSpace(meeting.ChatRecord))
            {
                model.AutoBriefNotes = SummarizeChatRecord(meeting.ChatRecord);
            }


            return model;
        }

        [HttpGet("/Advisor/Form3AutoPreview")]
        public IActionResult Form3AutoPreview()
        {
            int meetingId = 1; // مؤقتًا

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
            {
                summary += $"ناقشت الطالبة موضوع: {studentText}. ";
            }

            if (!string.IsNullOrWhiteSpace(advisorText))
            {
                summary += $"وقدمت المرشدة التوجيه التالي: {advisorText}.";
            }

            return summary.Trim();
        }

        // =======================
        // Advisor Chat
        // =======================

        [HttpGet("/Advisor/Chat")]
        public IActionResult Chat()
        {
            int meetingId = 1; // مؤقتًا

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
            int meetingId = 1; // مؤقتًا

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
            int meetingId = 1; // مؤقتًا

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

            int meetingId = 1; // مؤقتًا

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);

            if (meeting == null)
                return NotFound();

            var newMessage = new MeetingMessage
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
            int meetingId = 1; // مؤقتًا

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
            int meetingId = 1; // مؤقتًا

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);

            if (meeting == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(meeting.ChatRecord))
            {
                TempData["Success"] = "لا يوجد ChatRecord لتلخيصه.";
                return RedirectToAction("Form3");
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

            return RedirectToAction("Form3");
        }







        // GET: عرض الفورم
        /* public IActionResult Form5()
         {
             // بيانات افتراضية (حالياً)
             var model = new GraduationProjectEligibilityForm
             {
                 FormId = 5,
                 StudentName = "Lama Zaki Alshikh",
                 StudentId = "22190123",

                 CPIS351 = true,
                 CPIS358 = true,
                 CPIS323 = true,

                 CPIS360 = false,
                 CPIS375 = false,
                 CPIS342 = true,

                 FormStatus = "Pending",
                 CreatedDate = DateTime.Now
             };

             // تحديد الأهلية
             model.IsEligible =
                 model.CPIS351 &&
                 model.CPIS358 &&
                 model.CPIS323 &&
                 model.CPIS360 &&
                 model.CPIS375 &&
                 model.CPIS342;

             return View(model);
         }

         // POST: تحديث حالة الفورم (Accept / Reject / Update)
         [HttpPost]
         public IActionResult UpdateStatus(int formId, string status)
         {
             /*
             // 🔗 ربط الداتابيس (معلّق حالياً)
             using (var context = new AcadifyDbContext())
             {
                 var form = context.GraduationProjectEligibilityForms
                                   .FirstOrDefault(f => f.FormId == formId);

                 if (form != null)
                 {
                     form.FormStatus = status;
                     context.SaveChanges();
                 }
             }


             // حالياً بدون تخزين
             return RedirectToAction("Form5");
         }
          */




        // GET: Advisor/Form3
        [HttpGet]
        public IActionResult Form3()
        {
            int meetingId = 1; // مؤقتًا

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);

            if (meeting == null)
                return NotFound();

            var existingForm = _context.Forms
                .Include(f => f.MeetingForm)
                .FirstOrDefault(f => f.FormType == "Form 3"
                                  && f.MeetingForm != null
                                  && f.MeetingForm.MeetingId == meetingId);

            var model = new Form3ViewModel
            {
                StudentName = "",
                StudentId = "",
                Status = existingForm?.FormStatus ?? "Draft",
                AdvisorNotes = existingForm?.AdvisorNotes ?? "",
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

            // أول صف فقط
            var row1 = model.Meetings[0];

            // 1) إذا فيه بيانات محفوظة بالداتابيس، عبّي منها
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

            // 2) إذا التاريخ ما زال فاضي، خذيه من الـ Meeting
            if (string.IsNullOrWhiteSpace(row1.MeetingDate) && meeting.RecordingStartedAt.HasValue)
            {
                row1.MeetingDate = meeting.RecordingStartedAt.Value.ToString("dd/MM/yyyy hh:mm tt");
            }

            // 3) إذا خانة الملخص ما زالت فاضية، خذيها من ChatSummary
            if (string.IsNullOrWhiteSpace(row1.ProposedSolutions) && !string.IsNullOrWhiteSpace(meeting.ChatSummary))
            {
                row1.ProposedSolutions = meeting.ChatSummary;
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult SaveForm3(Form3ViewModel model)
        {
            int meetingId = 1; // مؤقتًا

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);

            if (meeting == null)
                return NotFound();

            var form = _context.Forms
                .Include(f => f.MeetingForm)
                .FirstOrDefault(f => f.FormType == "Form 3"
                                  && f.MeetingForm != null
                                  && f.MeetingForm.MeetingId == meetingId);

            if (form == null)
            {
                form = new Form
                {
                    StudentId = meeting.StudentId,
                    AdvisorId = meeting.AdvisorId,
                    FormType = "Form 3",
                    FormDate = DateTime.Now,
                    FormStatus = "Draft",
                    AdvisorNotes = model.AdvisorNotes,
                    AutoFilled = true,
                    AdvisorConfirmation = null
                };

                _context.Forms.Add(form);
                _context.SaveChanges();

                form.MeetingForm = new MeetingForm
                {
                    FormId = form.FormId,
                    MeetingId = meetingId
                };

                _context.MeetingForms.Add(form.MeetingForm);
            }

            form.FormDate = DateTime.Now;
            form.FormStatus = "Draft";
            form.AdvisorNotes = model.AdvisorNotes;

            var row1 = model.Meetings.FirstOrDefault();

            if (row1 != null && form.MeetingForm != null)
            {
                DateTime parsedMeetingStart;

                if (DateTime.TryParseExact(
                        row1.MeetingDate,
                        "dd/MM/yyyy hh:mm tt",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out parsedMeetingStart))
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

            TempData["Success"] = "Form 3 saved successfully.";
            return RedirectToAction("Form3");
        }
        [HttpPost]
        public IActionResult SendForm3(Form3ViewModel model)
        {
            int meetingId = 1; // مؤقتًا

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);

            if (meeting == null)
                return NotFound();

            var form = _context.Forms
                .Include(f => f.MeetingForm)
                .FirstOrDefault(f => f.FormType == "Form 3"
                                  && f.MeetingForm != null
                                  && f.MeetingForm.MeetingId == meetingId);

            if (form == null)
            {
                form = new Form
                {
                    StudentId = meeting.StudentId,
                    AdvisorId = meeting.AdvisorId,
                    FormType = "Form 3",
                    FormDate = DateTime.Now,
                    FormStatus = "Sent",
                    AdvisorNotes = model.AdvisorNotes,
                    AutoFilled = true,
                    AdvisorConfirmation = null
                };

                _context.Forms.Add(form);
                _context.SaveChanges();

                form.MeetingForm = new MeetingForm
                {
                    FormId = form.FormId,
                    MeetingId = meetingId
                };

                _context.MeetingForms.Add(form.MeetingForm);
            }

            form.FormDate = DateTime.Now;
            form.FormStatus = "Sent";
            form.AdvisorNotes = model.AdvisorNotes;

            var row1 = model.Meetings.FirstOrDefault();

            if (row1 != null && form.MeetingForm != null)
            {
                DateTime parsedMeetingStart;
                if (DateTime.TryParseExact(
                        row1.MeetingDate,
                        "dd/MM/yyyy hh:mm tt",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out parsedMeetingStart))
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

            TempData["Success"] = "Form 3 sent successfully.";
            return RedirectToAction("Form3");
        }
       


        private const string Form4SessionKey = "Form4Draft";

        // ==============================
        // GET: Advisor/Form4
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Form4()
        {
            int studentId = 2210783; // مؤقتًا

            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return NotFound("Student not found.");

            var model = BuildForm4ViewModel(student);

            var latestForm = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == "Form 4")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm != null)
            {
                model.AdvisorNotes = latestForm.AdvisorNotes ?? "";
>>>>>>> origin_second/rahafgh
            }

            return View(model);
        }

        [HttpPost]
<<<<<<< HEAD
        public IActionResult SaveForm4(Form4ViewModel model)
        {
            model.Status = "Draft";
            SaveForm4ToSession(model);
=======
        public async Task<IActionResult> SaveForm4(Form4ViewModel model)
        {
            int studentId = 2210783; // مؤقتًا
            int advisorId = 1;       // مؤقتًا إلى أن تربطيه بتسجيل الدخول

            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return NotFound("Student not found.");

            var vm = BuildForm4ViewModel(student);

            var form = new Form
            {
                StudentId = studentId,
                AdvisorId = advisorId,
                FormType = "Form 4",
                FormDate = DateTime.Now,
                FormStatus = "Draft",
                AdvisorNotes = model.AdvisorNotes,   // هنا الصح
                AutoFilled = true,
                AdvisorConfirmation = null
            };

            _context.Forms.Add(form);
            await _context.SaveChangesAsync();

            var form4 = new StudyPlanMatchingForm
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
>>>>>>> origin_second/rahafgh

            TempData["Success"] = "Form 4 saved successfully.";
            return RedirectToAction("Form4");
        }

        [HttpPost]
<<<<<<< HEAD
        public IActionResult SendForm4(Form4ViewModel model)
        {
            model.Status = "Sent";
            SaveForm4ToSession(model);
=======
        public async Task<IActionResult> SendForm4(Form4ViewModel model)
        {
            int studentId = 2210783; // مؤقتًا
            int advisorId = 1;       // مؤقتًا

            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return NotFound("Student not found.");

            var vm = BuildForm4ViewModel(student);

            var form = new Form
            {
                StudentId = studentId,
                AdvisorId = advisorId,
                FormType = "Form 4",
                FormDate = DateTime.Now,
                FormStatus = "Sent",
                AdvisorNotes = model.AdvisorNotes,   // هنا الصح
                AutoFilled = true,
                AdvisorConfirmation = true
            };

            _context.Forms.Add(form);
            await _context.SaveChangesAsync();

            var form4 = new StudyPlanMatchingForm
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
>>>>>>> origin_second/rahafgh

            TempData["Success"] = "Form 4 sent successfully.";
            return RedirectToAction("Form4");
        }

        private void SaveForm4ToSession(Form4ViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
<<<<<<< HEAD
            HttpContext.Session.SetString(Form4SessionKey, json);
        }

        private Form4ViewModel CreateNewForm4()
        {
            return new Form4ViewModel
            {
                StudentName = "Lama Alshikh",
                StudentId = "000000000",
                AcademicYear = "2024",
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
    }
}
=======
            HttpContext.Session.SetString("Form4Draft", json);
        }


        /* from here to dowm codes for form2 by rahaf gh */
        /*
        [HttpGet]
        public async Task<IActionResult> Form2(int studentId)
        {
            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null || latestForm.CourseChoiceMonitoringForm == null)
                return NotFound("Form 2 not found.");

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

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
        } */


        [HttpGet]
        public async Task<IActionResult> Form2()
        {
            int studentId = 2210783;

            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null || latestForm.CourseChoiceMonitoringForm == null)
                return NotFound("Form 2 not found.");

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

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

        //سشsave form2

        [HttpPost]
        public async Task<IActionResult> SaveForm2(Form2ViewModel model)
        {
            int studentId = model.StudentId;

            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null || latestForm.CourseChoiceMonitoringForm == null)
                return NotFound("Form 2 not found.");

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

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Form 2 saved successfully.";
            return RedirectToAction("Form2");
        }


        // Send Form2
        [HttpPost]
        public async Task<IActionResult> SendForm2(Form2ViewModel model)
        {
            int studentId = model.StudentId;

            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null || latestForm.CourseChoiceMonitoringForm == null)
                return NotFound("Form 2 not found.");

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

            // هنا حطي حالة الإرسال حسب الأعمدة الموجودة عندك
            latestForm.FormStatus = "Sent to Advising Committee";
            latestForm.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Form 2 sent to the advising committee successfully.";
            return RedirectToAction("Form2");
        }

        // Return Form2 To Student
        [HttpPost]
        public async Task<IActionResult> ReturnForm2ToStudent(Form2ViewModel model)
        {
            int studentId = model.StudentId;

            var latestForm = await _context.Forms
                .Include(f => f.CourseChoiceMonitoringForm)
                .Where(f => f.StudentId == studentId && f.FormType == "Form 2")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm == null || latestForm.CourseChoiceMonitoringForm == null)
                return NotFound("Form 2 not found.");

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

            // هنا حطي حالة الإرجاع حسب الأعمدة الموجودة عندك
            latestForm.FormStatus = "Returned to Student";
            latestForm.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Form 2 returned to the student successfully.";
            return RedirectToAction("Form2");
        }

    }
}
>>>>>>> origin_second/rahafgh
