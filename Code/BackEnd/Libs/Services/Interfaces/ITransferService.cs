using Repositories.Models;

public interface ITransferService
    {
        Task<TransferRequest> CreateTransferAsync(CreateTransferRequestDto request);
    }