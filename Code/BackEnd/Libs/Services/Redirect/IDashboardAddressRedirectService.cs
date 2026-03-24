using Infrastructure.DTOs;
using Repositories.Models;

namespace Services.Redirect;

public interface IDashboardAddressRedirectService
{
    Task<List<Address>> GetAddressesAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<Address?> GetAddressByIdAsync(string accessToken, int id, CancellationToken cancellationToken = default);
    Task<Address> CreateAddressAsync(string accessToken, AddressDto addressDto, CancellationToken cancellationToken = default);
    Task<Address> UpdateAddressAsync(string accessToken, int id, AddressDto addressDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAddressAsync(string accessToken, int id, CancellationToken cancellationToken = default);
}
