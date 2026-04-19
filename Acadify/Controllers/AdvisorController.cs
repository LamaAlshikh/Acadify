using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Acadify.Controllers
{
    public class AdvisorController : Controller
    {
        private readonly AcadifyDbContext _context;
        private const string Form4SessionKey = "Form4Draft";
        private const string SelectedStudentSessionKey = "SelectedStudentId";

        public AdvisorController(AcadifyDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentAdvisorId()
        {
            return HttpContext.Session.GetInt32("AdvisorId");
        }

        private int? GetSelectedStudentId()
        {
            return HttpContext.Session.GetInt32(SelectedStudentSessionKey);
        }

        private void SetSelectedStudentId(int studentId)
        {
            HttpContext.Session.SetInt32(SelectedStudentSessionKey, studentId);
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

        private static string GetAcademicStatusText(GraduationStatus? graduationStatus)
        {
            if (graduationStatus == null)
                return "Has Remaining Courses";

            if (graduationStatus.RemainingHours <= 0)
                return "Graduated";

            if (graduationStatus.RemainingHours <= 3)
                return "near graduation";

            return "Has Remaining Courses";
        }

        private async Task<Student?> GetAdvisorStudentAsync(int advisorId, int studentId)
        {
            return await _context.Students
                .Include(s => s.User)
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
                latestForm5 = new Form
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

                var details = new GraduationProjectEligibilityForm
                {
                    FormId = latestForm5.FormId,
                    Eligibility = null,
                    RequiredCoursesStatus = null
                };

                _context.GraduationProjectEligibilityForms.Add(details);
                await _context.SaveChangesAsync();
            }
            else
            {
                var details = await _context.GraduationProjectEligibilityForms
                    .FirstOrDefaultAsync(g => g.FormId == latestForm5.FormId);

                if (details == null)
                {
                    details = new GraduationProjectEligibilityForm
                    {
                        FormId = latestForm5.FormId,
                        Eligibility = null,
                        RequiredCoursesStatus = null
                    };

                    _context.GraduationProjectEligibilityForms.Add(details);
                    await _context.SaveChangesAsync();
                }
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
                .Include(s => s.User)
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

            int resolvedStudentId;

            if (studentId.HasValue && studentId.Value > 0)
            {
                resolvedStudentId = studentId.Value;
                SetSelectedStudentId(resolvedStudentId); // نحفظ الطالبة الحالية
            }
            else
            {
                var selectedStudentId = GetSelectedStudentId();
                if (!selectedStudentId.HasValue || selectedStudentId.Value <= 0)
                    return RedirectToAction("AdvisorHome");

                resolvedStudentId = selectedStudentId.Value;
            }

            var student = await GetAdvisorStudentAsync(advisorId.Value, resolvedStudentId);

            if (student == null)
                return NotFound("Student was not found for this advisor.");

            ViewBag.StudentId = student.StudentId;
            ViewBag.StudentName = student.Name;

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
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int? advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return RedirectToAction("Login", "Account");

            int resolvedStudentId;

            if (studentId.HasValue && studentId.Value > 0)
            {
                resolvedStudentId = studentId.Value;
                SetSelectedStudentId(resolvedStudentId);
            }
            else
            {
                var selectedStudentId = GetSelectedStudentId();
                if (!selectedStudentId.HasValue || selectedStudentId.Value <= 0)
                    return RedirectToAction("AdvisorHome");

                resolvedStudentId = selectedStudentId.Value;
            }

            var student = await GetAdvisorStudentAsync(advisorId.Value, resolvedStudentId);

            if (student == null)
                return NotFound("Student was not found for this advisor.");

            switch (formId)
            {
                case 1:
                    return Content($"Form 1 page for student {student.Name} is not connected yet.");

                case 2:
                    return Content($"Form 2 page for student {student.Name} is not connected yet.");

                case 3:
                    return RedirectToAction("Form3", new { studentId = resolvedStudentId });

                case 4:
                    return RedirectToAction("Form4", new { studentId = resolvedStudentId });

                case 5:
                    int form5Id = await GetOrCreateLatestForm5ForStudentAsync(resolvedStudentId, advisorId.Value);
                    return RedirectToAction("Form5", "GraduationProjectEligibility", new { formId = form5Id });

                default:
                    return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintForm(int? studentId, int formId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int? advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return RedirectToAction("Login", "Account");

            int resolvedStudentId;

            if (studentId.HasValue && studentId.Value > 0)
            {
                resolvedStudentId = studentId.Value;
                SetSelectedStudentId(resolvedStudentId);
            }
            else
            {
                var selectedStudentId = GetSelectedStudentId();
                if (!selectedStudentId.HasValue || selectedStudentId.Value <= 0)
                    return RedirectToAction("AdvisorHome");

                resolvedStudentId = selectedStudentId.Value;
            }

            var student = await GetAdvisorStudentAsync(advisorId.Value, resolvedStudentId);

            if (student == null)
                return NotFound("Student was not found for this advisor.");

            if (formId == 5)
            {
                int form5Id = await GetOrCreateLatestForm5ForStudentAsync(resolvedStudentId, advisorId.Value);
                return RedirectToAction("Form5", "GraduationProjectEligibility", new { formId = form5Id });
            }

            return Content($"Print Form {formId} for student {student.Name}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendForm(int studentId, int formId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Advisor")
                return RedirectToAction("Login", "Account");

            int? advisorId = GetCurrentAdvisorId();
            if (!advisorId.HasValue)
                return RedirectToAction("Login", "Account");

            SetSelectedStudentId(studentId);

            var student = await GetAdvisorStudentAsync(advisorId.Value, studentId);

            if (student == null)
                return NotFound("Student was not found for this advisor.");

            if (formId == 5)
            {
                int form5Id = await GetOrCreateLatestForm5ForStudentAsync(studentId, advisorId.Value);

                var form = await _context.Forms.FirstOrDefaultAsync(f => f.FormId == form5Id);
                if (form != null)
                {
                    form.FormStatus = "Sent";
                    form.FormDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = $"Form {formId} sent successfully.";
            return RedirectToAction("StudentForms", new { studentId = studentId });
        }

        [HttpGet]
        public IActionResult StudentFormsByStudent(int studentId)
        {
            SetSelectedStudentId(studentId);
            return RedirectToAction("StudentForms", new { studentId = studentId });
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
                            Form 3
           ======================================================== */
        [HttpGet]
        public IActionResult Form3(int? studentId = null)
        {
            if (studentId.HasValue && studentId.Value > 0)
                SetSelectedStudentId(studentId.Value);

            ViewBag.StudentId = studentId ?? GetSelectedStudentId();
            return View();
        }

        [HttpPost]
        public IActionResult SendForm3()
        {
            TempData["Success"] = "Form 3 sent successfully.";
            return RedirectToAction("Form3", new { studentId = GetSelectedStudentId() });
        }

        [HttpPost]
        public IActionResult AddNotesForm3(string notes)
        {
            TempData["Success"] = "Notes saved successfully.";
            return RedirectToAction("Form3", new { studentId = GetSelectedStudentId() });
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
        public IActionResult Form4(int? studentId = null)
        {
            if (studentId.HasValue && studentId.Value > 0)
                SetSelectedStudentId(studentId.Value);
            else
                studentId = GetSelectedStudentId();

            var json = HttpContext.Session.GetString(Form4SessionKey);

            Form4ViewModel model;

            if (!string.IsNullOrEmpty(json))
            {
                model = JsonSerializer.Deserialize<Form4ViewModel>(json) ?? CreateNewForm4();
            }
            else
            {
                model = CreateNewForm4();
            }

            if (studentId.HasValue)
            {
                model.StudentId = studentId.Value.ToString();
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult SaveForm4(Form4ViewModel model)
        {
            model.Status = "Draft";
            SaveForm4ToSession(model);

            TempData["Success"] = "Form 4 saved successfully.";
            return RedirectToAction("Form4", new { studentId = GetSelectedStudentId() });
        }

        [HttpPost]
        public IActionResult SendForm4(Form4ViewModel model)
        {
            model.Status = "Sent";
            SaveForm4ToSession(model);

            TempData["Success"] = "Form 4 sent successfully.";
            return RedirectToAction("Form4", new { studentId = GetSelectedStudentId() });
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