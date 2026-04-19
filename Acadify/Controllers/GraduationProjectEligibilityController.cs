using Acadify.Models;
using Acadify.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Controllers
{
    [Route("[controller]/[action]")]
    public class GraduationProjectEligibilityController : Controller
    {
        private readonly AcadifyDbContext _context;

        public GraduationProjectEligibilityController(AcadifyDbContext context)
        {
            _context = context;
        }

        // =========================
        // Session helpers
        // =========================
        private int? GetCurrentStudentId()
        {
            return HttpContext.Session.GetInt32("StudentId");
        }

        private async Task<int?> GetAdvisorIdForStudentAsync(int studentId)
        {
            return await _context.Students
                .Where(s => s.StudentId == studentId)
                .Select(s => (int?)s.AdvisorId)
                .FirstOrDefaultAsync();
        }

        private async Task<int> GetOrCreateLatestForm5ForStudentAsync(int studentId)
        {
            var latestForm5 = await _context.Forms
                .Where(f => f.StudentId == studentId && f.FormType == "Form 5")
                .OrderByDescending(f => f.FormDate)
                .ThenByDescending(f => f.FormId)
                .FirstOrDefaultAsync();

            if (latestForm5 == null)
            {
                var advisorId = await GetAdvisorIdForStudentAsync(studentId);

                if (!advisorId.HasValue || advisorId.Value <= 0)
                    throw new InvalidOperationException("No advisor is assigned to this student.");

                latestForm5 = new Form
                {
                    StudentId = studentId,
                    AdvisorId = advisorId.Value,
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

        private static string BuildSnapshot(
            bool cpis351,
            bool cpis358,
            bool cpis323,
            bool cpis380,
            bool cpis357,
            bool cpis342)
        {
            return string.Join(";",
                $"CPIS351={(cpis351 ? 1 : 0)}",
                $"CPIS358={(cpis358 ? 1 : 0)}",
                $"CPIS323={(cpis323 ? 1 : 0)}",
                $"CPIS380={(cpis380 ? 1 : 0)}",
                $"CPIS357={(cpis357 ? 1 : 0)}",
                $"CPIS342={(cpis342 ? 1 : 0)}"
            );
        }

        private static Dictionary<string, string> ParseSnapshot(string? raw)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(raw))
                return map;

            var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var index = part.IndexOf('=');
                if (index <= 0)
                    continue;

                var key = part.Substring(0, index).Trim();
                var value = part[(index + 1)..].Trim();

                if (!map.ContainsKey(key))
                    map.Add(key, value);
            }

            return map;
        }

        private static bool GetBool(Dictionary<string, string> map, string key)
        {
            if (!map.TryGetValue(key, out var value))
                return false;

            return value == "1" ||
                   value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static void FillVmFromSnapshot(GraduationProjectEligibilityFormVM vm, string? snapshot)
        {
            var map = ParseSnapshot(snapshot);

            vm.CPIS351 = GetBool(map, "CPIS351");
            vm.CPIS358 = GetBool(map, "CPIS358");
            vm.CPIS323 = GetBool(map, "CPIS323");
            vm.CPIS380 = GetBool(map, "CPIS380");
            vm.CPIS357 = GetBool(map, "CPIS357");
            vm.CPIS342 = GetBool(map, "CPIS342");

            vm.IsEligible =
                vm.CPIS351 &&
                vm.CPIS358 &&
                vm.CPIS323 &&
                vm.CPIS380 &&
                vm.CPIS357 &&
                vm.CPIS342;
        }

        [HttpGet]
        public async Task<IActionResult> Form5(int? formId, bool editMode = false)
        {
            int selectedFormId;

            if (formId.HasValue && formId.Value > 0)
            {
                selectedFormId = formId.Value;
            }
            else
            {
                var currentStudentId = GetCurrentStudentId();

                if (!currentStudentId.HasValue || currentStudentId.Value <= 0)
                    return BadRequest("Student session is not found. Please login again.");

                selectedFormId = await GetOrCreateLatestForm5ForStudentAsync(currentStudentId.Value);
            }

            var form5Entity = await _context.GraduationProjectEligibilityForms
                .Include(x => x.Form)
                .ThenInclude(f => f.Student)
                .FirstOrDefaultAsync(x => x.FormId == selectedFormId);

            if (form5Entity == null || form5Entity.Form == null)
                return NotFound();

            var sessionStudentId = GetCurrentStudentId();
            if (sessionStudentId.HasValue && form5Entity.Form.StudentId != sessionStudentId.Value)
                return Forbid();

            int currentStudentIdForForm = form5Entity.Form.StudentId;

            var transcript = await _context.Transcripts
                .Include(t => t.Courses)
                .FirstOrDefaultAsync(t => t.StudentId == currentStudentIdForForm);

            var vm = new GraduationProjectEligibilityFormVM
            {
                FormId = form5Entity.FormId,
                Form = form5Entity.Form,
                StudentName = form5Entity.Form.Student?.Name ?? "-",
                StudentId = currentStudentIdForForm.ToString(),
                IsHistoryView = false,
                IsEditMode = editMode
            };

            if (!string.IsNullOrWhiteSpace(form5Entity.RequiredCoursesStatus) &&
                form5Entity.RequiredCoursesStatus.Contains("CPIS351=", StringComparison.OrdinalIgnoreCase))
            {
                FillVmFromSnapshot(vm, form5Entity.RequiredCoursesStatus);
                vm.Eligibility = vm.IsEligible ? "Eligible" : "Not Eligible";
                return View(vm);
            }

            if (transcript == null)
            {
                vm.CPIS351 = false;
                vm.CPIS358 = false;
                vm.CPIS323 = false;
                vm.CPIS380 = false;
                vm.CPIS357 = false;
                vm.CPIS342 = false;
            }
            else
            {
                bool HasCourse(string code)
                {
                    string Normalize(string s) => (s ?? "").Replace(" ", "").Trim().ToUpper();
                    string target = Normalize(code);

                    return transcript.Courses.Any(c => Normalize(c.CourseId) == target);
                }

                vm.CPIS351 = HasCourse("CPIS351");
                vm.CPIS358 = HasCourse("CPIS358");
                vm.CPIS323 = HasCourse("CPIS323");
                vm.CPIS380 = HasCourse("CPIS380");
                vm.CPIS357 = HasCourse("CPIS357");
                vm.CPIS342 = HasCourse("CPIS342");
            }

            vm.IsEligible =
                vm.CPIS351 &&
                vm.CPIS358 &&
                vm.CPIS323 &&
                vm.CPIS380 &&
                vm.CPIS357 &&
                vm.CPIS342;

            vm.Eligibility = vm.IsEligible ? "Eligible" : "Not Eligible";

            form5Entity.Eligibility = vm.Eligibility;
            form5Entity.RequiredCoursesStatus = BuildSnapshot(
                vm.CPIS351,
                vm.CPIS358,
                vm.CPIS323,
                vm.CPIS380,
                vm.CPIS357,
                vm.CPIS342);

            await _context.SaveChangesAsync();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUpdate(GraduationProjectEligibilityFormVM vm)
        {
            var entity = await _context.GraduationProjectEligibilityForms
                .Include(x => x.Form)
                .ThenInclude(f => f.Student)
                .FirstOrDefaultAsync(x => x.FormId == vm.FormId);

            if (entity == null || entity.Form == null)
                return NotFound();

            var sessionStudentId = GetCurrentStudentId();
            if (sessionStudentId.HasValue && entity.Form.StudentId != sessionStudentId.Value)
                return Forbid();

            vm.IsEligible =
                vm.CPIS351 &&
                vm.CPIS358 &&
                vm.CPIS323 &&
                vm.CPIS380 &&
                vm.CPIS357 &&
                vm.CPIS342;

            entity.Eligibility = vm.IsEligible ? "Eligible" : "Not Eligible";
            entity.RequiredCoursesStatus = BuildSnapshot(
                vm.CPIS351,
                vm.CPIS358,
                vm.CPIS323,
                vm.CPIS380,
                vm.CPIS357,
                vm.CPIS342);

            entity.Form.FormStatus = "Updated";
            entity.Form.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["ActionMessage"] = "The form is updated successfully.";

            return RedirectToAction("Form5", new { formId = vm.FormId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int formId, string status)
        {
            var form = await _context.Forms.FirstOrDefaultAsync(f => f.FormId == formId);

            if (form == null)
                return NotFound("Form record not found.");

            form.FormStatus = status;
            form.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["ActionMessage"] = status switch
            {
                "Accepted" => "The form is accepted successfully.",
                "Rejected" => "The form is rejected successfully.",
                "Updated" => "The form is updated successfully.",
                _ => "The form status is updated successfully."
            };

            return RedirectToAction("Form5", new { formId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToAdvisingCommittee(int formId)
        {
            var form = await _context.Forms.FirstOrDefaultAsync(f => f.FormId == formId);

            if (form == null)
                return NotFound("Form record not found.");

            form.FormStatus = "Sent to Advising Committee";
            form.FormDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["ActionMessage"] = "The form is sent to the Advising Committee successfully.";

            return RedirectToAction("Form5", new { formId });
        }
    }
}