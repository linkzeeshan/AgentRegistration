using System.ComponentModel.DataAnnotations;

namespace MkTechSys.AgentRegistrationForm.Models
{
    public class AgentRegistrationViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "CR Code is required")]
        [Display(Name = "CR Code")]
        [StringLength(50, ErrorMessage = "CR Code cannot exceed 50 characters")]
        public string CRCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Iqama/National Identity is required")]
        [Display(Name = "Iqama/National Identity")]
        [StringLength(50, ErrorMessage = "Iqama/National Identity cannot exceed 50 characters")]
        public string IqamaOrNationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telephone is required")]
        [Display(Name = "Telephone")]
        [RegularExpression(@"^\+?966[0-9]{9}$|^[0-9]{10}$", ErrorMessage = "Please enter a valid Saudi phone number (e.g., +966XXXXXXXXX or 05XXXXXXXX)")]
        public string Telephone { get; set; } = string.Empty;
    }
}
