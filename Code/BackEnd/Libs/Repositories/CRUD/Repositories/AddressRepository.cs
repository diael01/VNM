using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface IAddressRepository : IRepository<Address> { }

public class AddressRepository : Repository<Address>, IAddressRepository
{
    public AddressRepository(VnmDbContext context) : base(context) { }
}