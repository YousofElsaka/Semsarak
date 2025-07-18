using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Routing;

namespace SEMSARK.DTOS.Auth
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [StringLength(14, ErrorMessage = "National ID must be 14 digits long.", MinimumLength = 14)]
        public string NationalId { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "Phone number must be a valid Egyptian mobile number")]
        public string PhoneNumber { get; set; }


        public string Role { get; set; } = "Renter"; // Default role 


    }
}
