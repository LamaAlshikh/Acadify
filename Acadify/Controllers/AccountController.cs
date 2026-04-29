using Acadify.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Acadify.Controllers
{
    public class AccountController : Controller
    {
<<<<<<< HEAD
<<<<<<< HEAD
        // يعرض صفحة التسجيل
        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        // يستقبل بيانات التسجيل
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(SignUpVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // هنا لاحقاً نحفظ البيانات في الداتابيس

            return RedirectToAction("SignUp");
        }

        // يعرض صفحة تسجيل الدخول
=======
>>>>>>> origin_second/rahafgh
        [HttpGet]
        public IActionResult Login()
=======
        private readonly AcadifyDbContext _db;

        public AccountController(AcadifyDbContext db)
>>>>>>> origin_second/لما2
        {
            _db = db;
        }

        private async Task AddNotificationToAllAdminsAsync(
            string senderRole,
            string sourceType,
            string type,
            string message,
            int? studentId = null,
            int? advisorId = null)
        {
            var admins = await _db.Admins.ToListAsync();

            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    SenderRole = senderRole,
                    SourceType = sourceType,
                    Type = type,
                    Message = message,
                    StudentId = studentId,
                    AdvisorId = advisorId,
                    AdminId = admin.AdminId,
                    Date = DateTime.Now,
                    IsRead = false
                });
            }

            await _db.SaveChangesAsync();
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View(new SignUpVM());
        }

<<<<<<< HEAD
        // يستقبل بيانات تسجيل الدخول
=======
>>>>>>> origin_second/rahafgh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email = model.Email.Trim().ToLower();

            bool isStudentEmail = email.EndsWith("@stu.kau.edu.sa");
            bool isStaffEmail = email.EndsWith("@kau.edu.sa") && !isStudentEmail;

            if (!isStudentEmail && !isStaffEmail)
            {
                ModelState.AddModelError("Email", "Only KAU academic emails are allowed.");
                return View(model);
            }

            if (isStudentEmail)
            {
                if (string.IsNullOrWhiteSpace(model.ID))
                {
                    ModelState.AddModelError("ID", "Student ID is required for student accounts.");
                    return View(model);
                }

                if (!int.TryParse(model.ID, out _))
                {
                    ModelState.AddModelError("ID", "Student ID must be numeric.");
                    return View(model);
                }
            }

            bool emailExists = _db.Users.Any(u => u.Email.ToLower() == email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            var user = new User
            {
                Name = model.FullName.Trim(),
                Email = email,
                Password = model.Password
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            if (isStudentEmail)
            {
                int studentId = int.Parse(model.ID!);

                bool studentIdExists = _db.Students.Any(s => s.StudentId == studentId);
                if (studentIdExists)
                {
                    ModelState.AddModelError("ID", "This student ID already exists.");

                    _db.Users.Remove(user);
                    await _db.SaveChangesAsync();

                    return View(model);
                }

                var student = new Student
                {
                    StudentId = studentId,
                    UserId = user.UserId,
                    Name = model.FullName.Trim(),
                    Major = "Information Systems",
                    Level = null,
                    CompletedHours = 0,
                    CohortYear = null,
                    AdvisorId = null
                };

                _db.Students.Add(student);
                await _db.SaveChangesAsync();

                await AddNotificationToAllAdminsAsync(
                    senderRole: "System",
                    sourceType: "Request",
                    type: "new student account",
                    message: $"New student account was created for {student.Name}.",
                    studentId: student.StudentId);
            }

            HttpContext.Session.SetString("PendingFirstLoginEmail", email);

            TempData["SuccessMessage"] = "Account created successfully. Please log in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email = model.Email.Trim().ToLower();

            bool isStudentEmail = email.EndsWith("@stu.kau.edu.sa");
            bool isStaffEmail = email.EndsWith("@kau.edu.sa") && !isStudentEmail;

            var user = await _db.Users
                .Include(u => u.Student)
                    .ThenInclude(s => s!.Transcript)
                .Include(u => u.Admin)
                .Include(u => u.Advisor)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (user.Password != model.Password)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (isStaffEmail)
            {
                if (string.IsNullOrWhiteSpace(model.Role) ||
                    (model.Role != "Admin" && model.Role != "Advisor"))
                {
                    ModelState.AddModelError("Role", "Please choose Admin or Advisor.");
                    return View(model);
                }

                if (model.Role == "Admin" && user.Admin == null)
                {
                    var admin = new Acadify.Models.Admin
                    {
                        UserId = user.UserId
                    };

                    _db.Admins.Add(admin);
                    await _db.SaveChangesAsync();

                    user = await _db.Users
                        .Include(u => u.Student)
                            .ThenInclude(s => s!.Transcript)
                        .Include(u => u.Admin)
                        .Include(u => u.Advisor)
                        .FirstOrDefaultAsync(u => u.UserId == user.UserId);
                }

                if (model.Role == "Advisor" && user.Advisor == null)
                {
                    var advisor = new Acadify.Models.Advisor
                    {
                        UserId = user.UserId,
                        AdvisorId = user.UserId,
                        Department = "Information Systems"
                    };

                    _db.Advisors.Add(advisor);
                    await _db.SaveChangesAsync();

                    user = await _db.Users
                        .Include(u => u.Student)
                            .ThenInclude(s => s!.Transcript)
                        .Include(u => u.Admin)
                        .Include(u => u.Advisor)
                        .FirstOrDefaultAsync(u => u.UserId == user.UserId);
                }
            }

            if (user == null)
            {
                ModelState.AddModelError("", "This account could not be loaded.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email);

            string role = "";

            if (user.Student != null)
                role = "Student";
            else if (user.Admin != null && model.Role == "Admin")
                role = "Admin";
            else if (user.Advisor != null && model.Role == "Advisor")
                role = "Advisor";
            else if (user.Admin != null)
                role = "Admin";
            else if (user.Advisor != null)
                role = "Advisor";

            HttpContext.Session.SetString("UserRole", role);

            if (user.Student != null)
            {
                HttpContext.Session.SetInt32("StudentId", user.Student.StudentId);

                string? pendingFirstLoginEmail = HttpContext.Session.GetString("PendingFirstLoginEmail");

                bool isFirstLoginAfterSignup =
                    !string.IsNullOrEmpty(pendingFirstLoginEmail) &&
                    pendingFirstLoginEmail == user.Email.ToLower();

                bool hasTranscript = user.Student.Transcript != null;

                if (isFirstLoginAfterSignup || !hasTranscript)
                {
                    HttpContext.Session.Remove("PendingFirstLoginEmail");
                    return RedirectToAction("UploadTranscript", "Student");
                }

                if (!user.Student.AdvisorId.HasValue)
                {
                    var hasPendingRequest = await _db.Set<AdvisorRequest>()
                        .AnyAsync(r => r.StudentId == user.Student.StudentId && r.Status == "Pending");

                    if (!hasPendingRequest)
                        return RedirectToAction("SelectAdvisor", "Student");

                    return RedirectToAction("SelectAdvisor", "Student");
                }

                return RedirectToAction("StudentHome", "Student");
            }

            if (role == "Admin")
            {
                if (user.Admin != null)
                    HttpContext.Session.SetInt32("AdminId", user.Admin.AdminId);

                return RedirectToAction("ManageAdvisorRequests", "Admin");
            }

            if (role == "Advisor")
            {
                if (user.Advisor != null)
                    HttpContext.Session.SetInt32("AdvisorId", user.Advisor.AdvisorId);

                return RedirectToAction("AdvisorHome", "Advisor");
            }

            ModelState.AddModelError("", "This account has no valid role.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}