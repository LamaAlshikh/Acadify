using Acadify.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Db = Acadify.Models.Db;
using UserModel = Acadify.Models.User;
using AdminModel = Acadify.Models.Admin;
using AdvisorModel = Acadify.Models.Advisor;
using StudentModel = Acadify.Models.Student;

namespace Acadify.Controllers
{
    public class AccountController : Controller
    {
        private readonly Db.AcadifyDbContext _db;

        public AccountController(Db.AcadifyDbContext db)
        {
            _db = db;
        }

        // ==============================
        // SignUp
        // ==============================
        [HttpGet]
        public IActionResult SignUp()
        {
            return View(new SignUpVM());
        }

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
                ModelState.AddModelError("Email", "يسمح فقط باستخدام البريد الجامعي الخاص بجامعة KAU.");
                return View(model);
            }

            bool emailExists = await _db.Set<UserModel>()
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "هذا البريد مسجل مسبقاً.");
                return View(model);
            }

            var user = new UserModel
            {
                Name = model.FullName.Trim(),
                Email = email,
                Password = model.Password
            };

            _db.Set<UserModel>().Add(user);
            await _db.SaveChangesAsync();

            if (isStudentEmail)
            {
                if (!int.TryParse(model.ID, out int studentId))
                {
                    ModelState.AddModelError("ID", "يجب أن يكون الرقم الجامعي أرقاماً فقط.");

                    _db.Set<UserModel>().Remove(user);
                    await _db.SaveChangesAsync();

                    return View(model);
                }

                bool studentExists = await _db.Set<StudentModel>()
                    .AnyAsync(s => EF.Property<int>(s, "StudentId") == studentId);

                if (studentExists)
                {
                    ModelState.AddModelError("ID", "الرقم الجامعي مستخدم بالفعل.");

                    _db.Set<UserModel>().Remove(user);
                    await _db.SaveChangesAsync();

                    return View(model);
                }

                var student = new StudentModel();

                SetPropertyValue(student, studentId, "StudentId", "StudentID", "Id");
                SetPropertyValue(student, model.FullName.Trim(), "Name", "StudentName", "FullName");
                SetPropertyValue(student, "Information Systems", "Major");
                SetPropertyValue(student, 0, "CompletedHours");

                _db.Set<StudentModel>().Add(student);
                await _db.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "تم إنشاء الحساب بنجاح، يمكنك تسجيل الدخول الآن.";
            return RedirectToAction(nameof(Login));
        }

        // ==============================
        // Login
        // ==============================
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

            var user = await _db.Set<UserModel>()
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == email &&
                    u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserEmail", user.Email);

            // Student login
            if (email.EndsWith("@stu.kau.edu.sa"))
            {
                var student = await FindStudentForUserAsync(user);

                if (student == null)
                {
                    ModelState.AddModelError("", "Student record was not found.");
                    return View(model);
                }

                int? studentId = GetIntPropertyValue(student, "StudentId", "StudentID", "Id");

                if (!studentId.HasValue)
                {
                    ModelState.AddModelError("", "Student ID was not found.");
                    return View(model);
                }

                HttpContext.Session.SetString("UserRole", "Student");
                HttpContext.Session.SetInt32("StudentId", studentId.Value);

                int? advisorId = GetIntPropertyValue(student, "AdvisorId", "AdvisorID");

                if (!advisorId.HasValue || advisorId.Value <= 0)
                    return RedirectToAction("SelectAdvisor", "Student");

                return RedirectToAction("StudentHome", "Student");
            }

            // Staff login
            string role = string.IsNullOrWhiteSpace(model.Role) ? "Advisor" : model.Role;

            if (role == "Admin")
            {
                int? adminId = await FindAdminIdForUserAsync(user);

                if (!adminId.HasValue)
                {
                    ModelState.AddModelError("", "Admin record was not found.");
                    return View(model);
                }

                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetInt32("AdminId", adminId.Value);

                return RedirectToAction("ManageAdvisorRequests", "Admin");
            }

            if (role == "Advisor")
            {
                int? advisorId = await FindAdvisorIdForUserAsync(user);

                if (!advisorId.HasValue)
                {
                    ModelState.AddModelError("", "Advisor record was not found.");
                    return View(model);
                }

                HttpContext.Session.SetString("UserRole", "Advisor");
                HttpContext.Session.SetInt32("AdvisorId", advisorId.Value);

                return RedirectToAction("AdvisorHome", "Advisor");
            }

            ModelState.AddModelError("", "لا يوجد دور مرتبط بهذا الحساب.");
            return View(model);
        }

        private async Task<StudentModel?> FindStudentForUserAsync(UserModel user)
        {
            var students = await _db.Set<StudentModel>().ToListAsync();

            string userEmail = user.Email.Trim().ToLower();
            string userName = user.Name.Trim().ToLower();

            return students.FirstOrDefault(s =>
            {
                string name = GetStringPropertyValue(s, "Name", "StudentName", "FullName")
                    .Trim()
                    .ToLower();

                string email = GetStringPropertyValue(s, "Email", "StudentEmail", "UniversityEmail")
                    .Trim()
                    .ToLower();

                return (!string.IsNullOrWhiteSpace(email) && email == userEmail) ||
                       (!string.IsNullOrWhiteSpace(name) && name == userName);
            });
        }

        private async Task<int?> FindAdminIdForUserAsync(UserModel user)
        {
            var admins = await _db.Set<AdminModel>().ToListAsync();

            string userEmail = user.Email.Trim().ToLower();
            string userName = user.Name.Trim().ToLower();

            var admin = admins.FirstOrDefault(a =>
            {
                int? userId = GetIntPropertyValue(a, "UserId", "UserID");
                string email = GetStringPropertyValue(a, "Email", "AdminEmail", "UniversityEmail")
                    .Trim()
                    .ToLower();

                string name = GetStringPropertyValue(a, "Name", "AdminName", "FullName")
                    .Trim()
                    .ToLower();

                return (userId.HasValue && userId.Value == user.UserId) ||
                       (!string.IsNullOrWhiteSpace(email) && email == userEmail) ||
                       (!string.IsNullOrWhiteSpace(name) && name == userName);
            });

            return admin == null ? null : GetIntPropertyValue(admin, "AdminId", "AdminID", "Id");
        }

        private async Task<int?> FindAdvisorIdForUserAsync(UserModel user)
        {
            var advisors = await _db.Set<AdvisorModel>().ToListAsync();

            string userEmail = user.Email.Trim().ToLower();
            string userName = user.Name.Trim().ToLower();

            var advisor = advisors.FirstOrDefault(a =>
            {
                int? userId = GetIntPropertyValue(a, "UserId", "UserID");
                string email = GetStringPropertyValue(a, "Email", "AdvisorEmail", "UniversityEmail")
                    .Trim()
                    .ToLower();

                string name = GetStringPropertyValue(a, "Name", "AdvisorName", "FullName")
                    .Trim()
                    .ToLower();

                return (userId.HasValue && userId.Value == user.UserId) ||
                       (!string.IsNullOrWhiteSpace(email) && email == userEmail) ||
                       (!string.IsNullOrWhiteSpace(name) && name == userName);
            });

            return advisor == null ? null : GetIntPropertyValue(advisor, "AdvisorId", "AdvisorID", "Id");
        }

        private static string GetStringPropertyValue(object obj, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var prop = obj.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj)?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return string.Empty;
        }

        private static int? GetIntPropertyValue(object obj, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var prop = obj.GetType().GetProperty(propertyName);
                if (prop == null)
                    continue;

                var value = prop.GetValue(obj);

                if (value is int intValue)
                    return intValue;

                if (value != null && int.TryParse(value.ToString(), out int parsed))
                    return parsed;
            }

            return null;
        }

        private static void SetPropertyValue(object obj, object? value, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var prop = obj.GetType().GetProperty(propertyName);

                if (prop == null || !prop.CanWrite)
                    continue;

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                try
                {
                    var convertedValue = value == null
                        ? null
                        : Convert.ChangeType(value, targetType);

                    prop.SetValue(obj, convertedValue);
                    return;
                }
                catch
                {
                    // Try the next property name if conversion fails.
                }
            }
        }

        // ==============================
        // Logout
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }
    }
}
