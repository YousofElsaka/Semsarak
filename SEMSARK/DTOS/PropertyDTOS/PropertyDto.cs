namespace SEMSARK.DTOS.PropertyDTOS
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public double Price { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string Status { get; set; }
        public List<string> ImagePaths { get; set; }
    }
}
