using Acadify.Models;
using Acadify.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Controllers
{
    public class FormHistoryController : Controller
    {
        private readonly AcadifyDbContext _context;

        public FormHistoryController(AcadifyDbContext context)
        {
            _context = context;
        }

        // GET: /FormHistory/FormHistory?studentId=1&formType=MeetingForm
        public async Task<IActionResult> FormHistory(int studentId, string formType)
        {
            if (studentId <= 0 || string.IsNullOrWhiteSpace(formType))
            {
                return BadRequest();
            }

            // Note: This returns all stored forms for the same student and the same form type (latest first).
            var forms = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == formType)
                .OrderByDescending(f => f.FormDate)
                .Select(f => new FormHistoryItemVM
                {
                    FormId = f.FormId,
                    FormType = f.FormType,
                    FormStatus = f.FormStatus,
                    FormDate = f.FormDate
                })
                .ToListAsync();

            var vm = new FormHistoryVM
            {
                StudentId = studentId,
                FormType = formType,
                PageTitle = GetFormTitle(formType),
                Forms = forms
            };

            return View(vm); // Views/FormHistory/FormHistory.cshtml
        }

        // Note: This is just the page title text based on the selected form type.
        private string GetFormTitle(string formType)
        {
            return formType switch
            {
                "AcademicAdvisingForm" => "Academic Advising Form History",
                "CourseSelectionForm" => "Course Selection Form History",
                "MeetingForm" => "Meeting Form History",
                "StudyPlanMatchingForm" => "Study Plan Matching Form History",
                "GraduationProjectEligibilityForm" => "Graduation Project Eligibility Form History",
                _ => "Form History"
            };
        }

        // Note: This redirects to the correct form details page based on form type.
        // GET: /FormHistory/ViewForm?id=10&formType=MeetingForm
        public IActionResult ViewForm(int id, string formType)
        {
            return formType switch
            {
                "AcademicAdvisingForm" => RedirectToAction("AcademicAdvisingFormDetails", "Forms", new { id }),
                "CourseSelectionForm" => RedirectToAction("CourseSelectionFormDetails", "Forms", new { id }),
                "MeetingForm" => RedirectToAction("MeetingFormDetails", "Forms", new { id }),
                "StudyPlanMatchingForm" => RedirectToAction("StudyPlanMatchingFormDetails", "Forms", new { id }),
                "GraduationProjectEligibilityForm" => RedirectToAction("GraduationProjectEligibilityFormDetails", "Forms", new { id }),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}