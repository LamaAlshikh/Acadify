using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Acadify.Controllers
{
    public class AdvisorController : Controller
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
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult SaveForm4(Form4ViewModel model)
        {
            model.Status = "Draft";
            SaveForm4ToSession(model);

            TempData["Success"] = "Form 4 saved successfully.";
            return RedirectToAction("Form4");
        }

        [HttpPost]
        public IActionResult SendForm4(Form4ViewModel model)
        {
            model.Status = "Sent";
            SaveForm4ToSession(model);

            TempData["Success"] = "Form 4 sent successfully.";
            return RedirectToAction("Form4");
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