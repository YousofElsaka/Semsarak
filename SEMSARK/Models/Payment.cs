using System.ComponentModel.DataAnnotations.Schema;

namespace SEMSARK.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public double Commission { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending"; // Pending - Paid - Failed
        public bool IsConfirmed { get; set; } = false;

        public string PaymentType { get; set; } // "Advertise" or "Booking"

        public int? PropertyId { get; set; }
        public Property? Property { get; set; }

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public string? RenterId { get; set; }
        public ApplicationUser? Renter { get; set; }


    }
}
