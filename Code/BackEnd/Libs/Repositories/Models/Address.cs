namespace Repositories.Models;

public class Address
{
    public int Id { get; set; }
    public string Country { get; set; }
    public string County { get; set; }   // sau State
    public string City { get; set; }
    public string Street { get; set; }
    public string StreetNumber { get; set; }
    public string PostalCode { get; set; }

    public int InverterInfoId { get; set; }
    public InverterInfo InverterInfo { get; set; }
}
