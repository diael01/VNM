namespace Infrastructure.DTOs
{
    public class AddressDto
    {
        public int Id { get; set; }
        public required string Country { get; set; }
        public required string County { get; set; }
        public required string City { get; set; }
        public required string Street { get; set; }
        public required string StreetNumber { get; set; }
        public required string PostalCode { get; set; }
        public int InverterId { get; set; }
    }
}