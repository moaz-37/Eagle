using System.ComponentModel.DataAnnotations;

namespace Eagle.PL.Models
{
    public class LoginViewModel
    {
        [Required, EmailAddress, Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "تذكرني")]
        public bool RememberMe { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required, Display(Name = "الاسم بالكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Required, Display(Name = "الدور")]
        public string Role { get; set; } = "Cashier"; // "Cashier" or "Manager"
    }
}