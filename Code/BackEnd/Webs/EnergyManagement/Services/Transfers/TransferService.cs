

using EnergyManagement.Services.Transfers;
using Repositories.Models;

public class TransferService : ITransferService
    {
        private readonly VnmDbContext _db;
        private readonly IAvailableBalanceService _available;
        private readonly ISettlementModeResolver _resolver;

        public TransferService(VnmDbContext db, IAvailableBalanceService available, ISettlementModeResolver resolver)
        {
            _db = db;
            _available = available;
            _resolver = resolver;
        }

       public async Task<TransferRequest> CreateTransferAsync(CreateTransferRequestDto request)
        {
            var available = await _available.GetAvailableBalanceAsync(request.SourceAddressId, request.Day);

            var mode = _resolver.GetCurrentMode();
            var strategy = _resolver.Resolve(mode);

            strategy.ValidateRequest(request.Amount, available);

            var transfer = new TransferRequest
            {
                SourceAddressId = request.SourceAddressId,
                DestinationAddressId = request.DestinationAddressId,
                Day = request.Day,
                CreatedAtUtc = DateTime.UtcNow,
                Status = "Completed"
            };

            strategy.FillTransferAmounts(transfer, request.Amount);

           // _db.TransferRequests.Add(transfer);//todo: uncomment when all works
            await _db.SaveChangesAsync();

            return transfer;
        }
    }

