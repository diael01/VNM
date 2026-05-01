using Repositories.CRUD.Interfaces;
using Repositories.Models;

namespace Repositories.CRUD.Repositories;

public interface ITransferLedgerEntryRepository : IRepository<TransferLedgerEntry> { }

public class TransferLedgerEntryRepository : Repository<TransferLedgerEntry>, ITransferLedgerEntryRepository
{
    public TransferLedgerEntryRepository(VnmDbContext context) : base(context) { }
}