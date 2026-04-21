using Acadify.Models;
using Acadify.Models.Db;
using Acadify.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;


namespace Acadify.Controllers
{
    public class AdvisorController : Controller


    {

        private readonly AiSummaryService _aiSummaryService;


        private readonly AcadifyDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdvisorController(AcadifyDbContext context, IWebHostEnvironment env, AiSummaryService aiSummaryService)
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
                // 1) مواد مصنفة مباشرة
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

                // 2) إذا المادة غير مصنفة، نشوف هل لها قرار سابق
                var decision = decisions.FirstOrDefault(d => d.TranscriptCourseId == course.CourseId);

                if (decision != null)
                {
                    // إذا القرار مادة حرة
                    if (decision.DecisionType == "FreeElective")
                    {
                        freeCoursesHours += course.Hours;
                        continue;
                    }

                    // إذا القرار معادلة لمادة من الخطة
                    if (decision.DecisionType == "EquivalentToPlan" &&
                        !string.IsNullOrWhiteSpace(decision.EquivalentCourseId))
                    {
                        var equivalentCourse = _context.Courses
                            .FirstOrDefault(c => c.CourseId == decision.EquivalentCourseId);

                        if (equivalentCourse != null)
                        {
                            if (equivalentCourse.RequirementCategory == "University")
                            {
                                universityHours += equivalentCourse.Hours;
                            }
                            else if (equivalentCourse.RequirementCategory == "PrepYear")
                            {
                                prepYearHours += equivalentCourse.Hours;
                            }
                            else if (equivalentCourse.RequirementCategory == "CollegeMandatory")
                            {
                                collegeMandatoryHours += equivalentCourse.Hours;
                            }
                            else if (equivalentCourse.RequirementCategory == "DeptMandatory")
                            {
                                deptMandatoryHours += equivalentCourse.Hours;
                            }
                            else if (electiveCourses.Contains(equivalentCourse.CourseId))
                            {
                                deptElectiveHours += equivalentCourse.Hours;
                            }
                            else
                            {
                                freeCoursesHours += equivalentCourse.Hours;
                            }

                            continue;
                        }
                    }
                }

                // 3) إذا ما عندها قرار → تظهر للدكتورة في Form 4
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

                AdvisorNameLabel = "المرشدة الأكاديمية للطالبة",
                AdvisorName = "",
                AdvisorNotes = "",

                PendingCourses = pendingCourses
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

        private bool IsDirectlyClassifiedCourse(Course course, List<string> electiveCourses)
        {
            if (course.RequirementCategory == "University")
                return true;

            if (course.RequirementCategory == "PrepYear")
                return true;

            if (course.RequirementCategory == "CollegeMandatory")
                return true;

            if (course.RequirementCategory == "DeptMandatory")
                return true;

            if (electiveCourses.Contains(course.CourseId))
                return true;

            return false;
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
                    existing = new TranscriptCourseDecision
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

            var form = new Form
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

            var meetingForm = new MeetingForm
            {
                FormId = form.FormId,
                MeetingId = meetingId
            };

            var row1 = model.Meetings.FirstOrDefault();

            if (row1 != null)
            {
                DateTime parsedMeetingStart;

                if (DateTime.TryParseExact(
                        row1.MeetingDate,
                        "dd/MM/yyyy hh:mm tt",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out parsedMeetingStart))
                {
                    meetingForm.MeetingStart = parsedMeetingStart;
                }
                else
                {
                    meetingForm.MeetingStart = meeting.RecordingStartedAt;
                }

                meetingForm.MeetingEnd = meeting.RecordingStoppedAt;

                if (row1.PurposeAcademic)
                    meetingForm.MeetingPurpose = "Academic";
                else if (row1.PurposeCareer)
                    meetingForm.MeetingPurpose = "Career";
                else if (row1.PurposeOther)
                    meetingForm.MeetingPurpose = "Other";
                else
                    meetingForm.MeetingPurpose = null;

                meetingForm.ReferredTo = row1.ReferralName;
                meetingForm.ReferralReason = row1.ReferralReason;
                meetingForm.MeetingNotes = row1.ProposedSolutions;
            }

            _context.MeetingForms.Add(meetingForm);
            _context.SaveChanges();

            TempData["Success"] = "Form 3 saved successfully.";
            return RedirectToAction("Form3History");
        }
        [HttpPost]
        public IActionResult SendForm3(Form3ViewModel model)
        {
            int meetingId = 1; // مؤقتًا

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId);

            if (meeting == null)
                return NotFound();

            var form = new Form
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

            var meetingForm = new MeetingForm
            {
                FormId = form.FormId,
                MeetingId = meetingId
            };

            var row1 = model.Meetings.FirstOrDefault();

            if (row1 != null)
            {
                DateTime parsedMeetingStart;

                if (DateTime.TryParseExact(
                        row1.MeetingDate,
                        "dd/MM/yyyy hh:mm tt",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out parsedMeetingStart))
                {
                    meetingForm.MeetingStart = parsedMeetingStart;
                }
                else
                {
                    meetingForm.MeetingStart = meeting.RecordingStartedAt;
                }

                meetingForm.MeetingEnd = meeting.RecordingStoppedAt;

                if (row1.PurposeAcademic)
                    meetingForm.MeetingPurpose = "Academic";
                else if (row1.PurposeCareer)
                    meetingForm.MeetingPurpose = "Career";
                else if (row1.PurposeOther)
                    meetingForm.MeetingPurpose = "Other";
                else
                    meetingForm.MeetingPurpose = null;

                meetingForm.ReferredTo = row1.ReferralName;
                meetingForm.ReferralReason = row1.ReferralReason;
                meetingForm.MeetingNotes = row1.ProposedSolutions;
            }

            _context.MeetingForms.Add(meetingForm);
            _context.SaveChanges();

            TempData["Success"] = "Form 3 sent successfully.";
            return RedirectToAction("Form3History");
        }



        private const string Form4SessionKey = "Form4Draft";

        // ==============================
        // GET: Advisor/Form4
        // ==============================
        [HttpGet]
        public async Task<IActionResult> Form4()
        {
            int studentId = 2210783;

            var student = await _context.Students
                .Include(s => s.Transcript)
                    .ThenInclude(t => t.Courses)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return NotFound("Student not found.");

            var model = BuildForm4ViewModel(student);
            model.PlanCourseOptions = GetIsPlanCourseOptions();

            var latestForm = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == "Form 4")
                .OrderByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm != null)
                model.AdvisorNotes = latestForm.AdvisorNotes ?? "";

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveForm4(Form4ViewModel model)
        {
            int studentId = 2210783; // مؤقتًا
            int advisorId = 1;       // مؤقتًا

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);

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
                AdvisorNotes = model.AdvisorNotes,
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

            TempData["Success"] = "Form 4 saved successfully.";
            return RedirectToAction("Form4History");
        }

        [HttpPost]
        public async Task<IActionResult> SendForm4(Form4ViewModel model)
        {
            int studentId = 2210783; // مؤقتًا
            int advisorId = 1;       // مؤقتًا

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);

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
                AdvisorNotes = model.AdvisorNotes,
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

            TempData["Success"] = "Form 4 sent successfully.";
            return RedirectToAction("Form4History");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveFreeCourses(Form4ViewModel model)
        {
            int studentId = 2210783; // مؤقتًا

            await SaveCourseDecisionsAsync(studentId, model.PendingCourses);

            TempData["Success"] = "Free courses updated successfully.";
            return RedirectToAction("Form4");
        }

        private void SaveForm4ToSession(Form4ViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            HttpContext.Session.SetString("Form4Draft", json);
        }

        [HttpGet]
        public async Task<IActionResult> Form4History()
        {
            int studentId = 2210783; // مؤقتًا

            var items = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == "Form 4")
                .OrderByDescending(f => f.FormDate)
                .Select(f => new Acadify.Models.FormHistoryItemViewModel
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

            if (form == null)
                return NotFound("Form 4 not found.");

            var student = form.Student;
            var model = BuildForm4ViewModel(student);

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

        [HttpGet]
        public async Task<IActionResult> Form3History()
        {
            int meetingId = 1; // مؤقتًا

            var items = await _context.Forms
                .Include(f => f.MeetingForm)
                .Where(f => f.FormType == "Form 3"
                         && f.MeetingForm != null
                         && f.MeetingForm.MeetingId == meetingId)
                .OrderByDescending(f => f.FormDate)
                .Select(f => new Acadify.Models.FormHistoryItemViewModel
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

            if (form == null)
                return NotFound();

            var meetingId = form.MeetingForm?.MeetingId;
            if (meetingId == null)
                return NotFound();

            var meeting = _context.Meetings.FirstOrDefault(m => m.MeetingId == meetingId.Value);
            if (meeting == null)
                return NotFound();

            var model = new Form3ViewModel
            {
                StudentName = "",
                StudentId = "",
                Status = form.FormStatus ?? "Draft",
                AdvisorNotes = form.AdvisorNotes ?? "",
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

            var row1 = model.Meetings[0];
            var mf = form.MeetingForm;

            if (mf != null)
            {
                if (mf.MeetingStart.HasValue)
                    row1.MeetingDate = mf.MeetingStart.Value.ToString("dd/MM/yyyy hh:mm tt");

                row1.PurposeAcademic = mf.MeetingPurpose == "Academic";
                row1.PurposeCareer = mf.MeetingPurpose == "Career";
                row1.PurposeOther = mf.MeetingPurpose == "Other";

                row1.ReferralName = mf.ReferredTo ?? "";
                row1.ReferralReason = mf.ReferralReason ?? "";
                row1.ProposedSolutions = mf.MeetingNotes ?? "";
            }

            return View("Form3", model);
        }





    }
}
