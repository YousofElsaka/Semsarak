using Microsoft.AspNetCore.Identity;

namespace SEMSARK.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string NationalId { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public ICollection<Property> Properties { get; set; }
        public ICollection<Payment> PaymentsAsOwner { get; set; }
        public ICollection<Payment> PaymentsAsRenter { get; set; }
    }
}
