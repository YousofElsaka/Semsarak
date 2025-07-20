namespace SEMSARK.DTOS.BookingDTOs
{
    public class BookingReadDTO
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; }
        public double Price { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public int RoomsCount { get; set; }
        public List<string> ImageUrls { get; set; }

        public string RenterId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
