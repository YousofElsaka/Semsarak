using System.ComponentModel.DataAnnotations.Schema;

namespace SEMSARK.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public double Commission { get; set; }
        public DateTime DateTime { get; set; }
        public string Status { get; set; }
        public bool IsConfirmed { get; set; }
        [ForeignKey("Property")]
        public int PropertyId { get; set; }
        [ForeignKey("Renter")]
        public string RenterId { get; set; }
        [ForeignKey("Owner")]
        public string OwnerId { get; set; }
        public Property Property { get; set; }
        public ApplicationUser Owner { get; set; }
        public ApplicationUser Renter { get; set; }

        public Booking Booking { get; set; } // ← لازم تكون موجودة


    }
}
