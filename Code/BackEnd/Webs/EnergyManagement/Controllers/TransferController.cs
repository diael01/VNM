
using EnergyManagement.Services.Transfers;
using Microsoft.AspNetCore.Mvc;


namespace EnergyManagement.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransfersController : ControllerBase
{
    private readonly IAvailableBalanceService _balance;
    private readonly ITransferService _transfer;

    public TransfersController(
        IAvailableBalanceService balance,
        ITransferService transfer)
    {
        _balance = balance;
        _transfer = transfer;
    }

    [HttpGet("available/{addressId}/{day}")]
    public async Task<IActionResult> GetAvailable(int addressId, DateOnly day)
        => Ok(await _balance.GetAvailableBalanceAsync(addressId, day));

    [HttpPost]
    public async Task<IActionResult> Create(CreateTransferRequestDto request)
        => Ok(await _transfer.CreateTransferAsync(request));

    // [HttpGet("impact/{id}")] //todo: uncomment when all works
    // public async Task<IActionResult> Impact(int id)
    //     => Ok(await _transfer.GetTransferImpactAsync(id));
}