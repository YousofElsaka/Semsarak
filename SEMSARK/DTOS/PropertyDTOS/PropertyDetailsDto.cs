public class PropertyDetailsDto
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public double Price { get; set; }

    public int RoomsCount { get; set; }

    public string GenderPreference { get; set; }

    public string City { get; set; }

    public string Region { get; set; }

    public string Street { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string OwnerName { get; set; }

    public List<string> ImageUrls { get; set; }
}
