using System.ComponentModel.DataAnnotations.Schema;

namespace SEMSARK.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public Property Property { get; set; }

        public string RenterId { get; set; }


        [ForeignKey("RenterId")]
        public ApplicationUser Renter { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string Status { get; set; } // "Pending", "Approved", etc. 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // One-to-one: Payment 
        public int? PaymentId { get; set; }  // Nullable
        public Payment? Payment { get; set; }



    }
}
