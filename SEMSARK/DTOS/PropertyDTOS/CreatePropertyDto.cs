namespace SEMSARK.DTOS.PropertyDTOS
{
    public class CreatePropertyDto
    {

        public string Title { get; set; }

        public string Description { get; set; }

        public double Price { get; set; }

        public int RoomsCount { get; set; }

        public string GenderPreference { get; set; }

        public string City { get; set; }

        public string Region { get; set; }

        public string Street { get; set; }
    }
}
