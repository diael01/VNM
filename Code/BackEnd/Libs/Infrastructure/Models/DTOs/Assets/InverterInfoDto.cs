
namespace Infrastructure.DTOs;
public class InverterInfoDto
{
    public int Id { get; set; }

    public string? Model { get; set; }

    public string? Manufacturer { get; set; }

    public string? SerialNumber { get; set; }

    public int AddressId { get; set; }
}
