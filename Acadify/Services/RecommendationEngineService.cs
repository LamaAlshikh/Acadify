using System.Text.RegularExpressions;
using Acadify.Models;
using Acadify.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Services
{
    public class RecommendationEngineService : IRecommendationEngineService
    {
        private readonly AcadifyDbContext _context;

        public RecommendationEngineService(AcadifyDbContext context)
        {
            _context = context;
        }

        public async Task<List<RecommendedCourseVm>> GenerateRecommendationsAsync(
            int planId,
            List<TranscriptCourseItem> transcriptCourses)
        {
            var passedCourseIds = transcriptCourses
                .Where(x => x.IsPassed && !string.IsNullOrWhiteSpace(x.CourseId))
                .Select(x => NormalizeCourseId(x.CourseId))
                .ToHashSet();

            var planCourses = await _context.Set<StudyPlanCourse>()
                .Where(sp => sp.PlanId == planId)
                .Join(
                    _context.Set<Course>(),
                    sp => sp.CourseId,
                    c => c.CourseId,
                    (sp, c) => new
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        Hours = c.Hours,
                        Prerequisite = c.Prerequisite,
                        SemesterNo = sp.SemesterNo,
                        DisplayOrder = sp.DisplayOrder
                    })
                .Where(x => x.Hours > 0)
                .OrderBy(x => x.SemesterNo)
                .ThenBy(x => x.DisplayOrder)
                .ToListAsync();

            var remaining = planCourses
                .Where(c => !passedCourseIds.Contains(NormalizeCourseId(c.CourseId)))
                .ToList();

            if (!remaining.Any())
                return new List<RecommendedCourseVm>();

            // أول سمستر فيه مواد متبقية
            var firstIncompleteSemester = remaining
                .Where(x => x.SemesterNo != 81)
                .Select(x => x.SemesterNo)
                .DefaultIfEmpty(81)
                .Min();

            // فقط مواد هذا السمستر
            var currentSemesterCourses = remaining
                .Where(x => x.SemesterNo == firstIncompleteSemester)
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            var recommended = currentSemesterCourses
                .Where(x => ArePrerequisitesSatisfied(x.Prerequisite, passedCourseIds))
                .Select(x => new RecommendedCourseVm
                {
                    CourseId = x.CourseId,
                    CourseName = x.CourseName ?? x.CourseId,
                    Hours = x.Hours,
                    SemesterNo = x.SemesterNo,
                    DisplayOrder = x.DisplayOrder,
                    Reason = BuildReason(x.Prerequisite, firstIncompleteSemester)
                })
                .ToList();

            // لو ما فيه شيء مؤهل في أول سمستر غير مكتمل:
            // لا تقفزي لكل الخطة، رجعي فارغ أو عالجيه في الواجهة كـ blocked courses
            if (recommended.Any())
                return recommended;

            // خيار اختياري: إذا عندك مواد عامة / اختيارية برقم 81 وتبغين تعرضينها فقط لما تكون متاحة
            var electiveRecommendations = remaining
                .Where(x => x.SemesterNo == 81)
                .Where(x => ArePrerequisitesSatisfied(x.Prerequisite, passedCourseIds))
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new RecommendedCourseVm
                {
                    CourseId = x.CourseId,
                    CourseName = x.CourseName ?? x.CourseId,
                    Hours = x.Hours,
                    SemesterNo = x.SemesterNo,
                    DisplayOrder = x.DisplayOrder,
                    Reason = "Elective / general course with satisfied prerequisite."
                })
                .ToList();

            return electiveRecommendations;
        }

        private bool ArePrerequisitesSatisfied(string? prerequisiteText, HashSet<string> passedCourseIds)
        {
            if (string.IsNullOrWhiteSpace(prerequisiteText))
                return true;

            // دعم بسيط لـ OR / أو
            var orParts = Regex.Split(prerequisiteText, @"\s+(?:OR|or|Or|أو)\s+")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            // إذا فيه OR، يكفي أي جزء منها يتحقق بالكامل
            if (orParts.Count > 1)
            {
                return orParts.Any(part =>
                {
                    var codes = ExtractCourseCodes(part);
                    if (!codes.Any())
                        return false;

                    return codes.All(code => passedCourseIds.Contains(NormalizeCourseId(code)));
                });
            }

            // otherwise = AND عادي
            var prerequisiteCodes = ExtractCourseCodes(prerequisiteText);

            if (!prerequisiteCodes.Any())
                return true;

            return prerequisiteCodes.All(code => passedCourseIds.Contains(NormalizeCourseId(code)));
        }

        private List<string> ExtractCourseCodes(string text)
        {
            var list = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return list;

            var matches = Regex.Matches(
                text,
                @"\b([A-Z]{4})\s*-?\s*(\d{3})\b",
                RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var code = $"{match.Groups[1].Value.ToUpper()}-{match.Groups[2].Value}";
                    list.Add(code);
                }
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private string NormalizeCourseId(string? courseId)
        {
            if (string.IsNullOrWhiteSpace(courseId))
                return string.Empty;

            courseId = courseId.Trim().ToUpper();
            courseId = courseId.Replace(" ", "")
                               .Replace("_", "")
                               .Replace("/", "")
                               .Replace("--", "-");

            if (!courseId.Contains("-"))
            {
                var match = Regex.Match(courseId, @"^([A-Z]{4})(\d{3})$");
                if (match.Success)
                {
                    return $"{match.Groups[1].Value}-{match.Groups[2].Value}";
                }
            }

            return courseId;
        }

        private string BuildReason(string? prerequisiteText, int semesterNo)
        {
            if (string.IsNullOrWhiteSpace(prerequisiteText))
                return $"Recommended from semester {semesterNo}.";

            var codes = ExtractCourseCodes(prerequisiteText);
            if (!codes.Any())
                return $"Recommended from semester {semesterNo}; prerequisite text exists.";

            return $"Recommended from semester {semesterNo}; prerequisite satisfied: {string.Join(", ", codes)}.";
        }
    }
}