using System.ComponentModel.DataAnnotations;

namespace Acadify.Models
{
    public class SignUpVM
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // للطالبة فقط
        public string? ID { get; set; }

        [Required]
        [MinLength(4)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // يظهر فقط إذا كان الإيميل @kau.edu.sa
        public string? Role { get; set; }
    }
}