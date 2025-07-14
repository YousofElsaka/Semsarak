namespace SEMSARK.DTOS.PaymentDTO
{
    public class PaymentHistoryDTO
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public double Commission { get; set; }
        public string PaymentType { get; set; }
        public string Status { get; set; }
        public DateTime DateTime { get; set; }

        public int? PropertyId { get; set; }
        public string? PropertyTitle { get; set; }

        public int? BookingId { get; set; }
    }
}
