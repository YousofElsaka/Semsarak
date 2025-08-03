namespace SEMSARK.DTOS.Admin
{
    public class AdminDashboardDto
    {
        public decimal TotalProfitFromBookings { get; set; }
        public decimal TotalProfitFromAdvertisements { get; set; }
        public decimal TotalProfitOverall => TotalProfitFromBookings + TotalProfitFromAdvertisements;
        public int TotalRenters { get; set; }
        public int TotalOwners { get; set; }
    }
}
