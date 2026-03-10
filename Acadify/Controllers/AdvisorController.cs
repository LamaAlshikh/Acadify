using Acadify.Data;
using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Controllers
{
    public class AdvisorController : Controller
    {
        private readonly AcadifyDbContext _context;

        public AdvisorController(AcadifyDbContext context)
        {
            _context = context;
        }

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
                            AdvisorHome
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

        /* =========================================================
                           CommunityAdvisor
           ========================================================= */
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
    }
}