
using EnergyManagement.Services.Transfers;
using Microsoft.AspNetCore.Mvc;

namespace EnergyManagement.Services.Transfers;

[ApiController]
[Route("api/v1/transfers")]
public class TransfersController : ControllerBase
{
    private readonly ITransferAllocationService _service;

    public TransfersController(ITransferAllocationService service)
    {
        _service = service;
    }

    [HttpPost("auto/{day}")]
    public async Task<IActionResult> RunAutomatic(DateOnly day, CancellationToken ct)
    {
        var result = await _service.RunAutomaticAllocationAsync(day, ct);
        return Ok(result);
    }

    [HttpPost("auto/{day}/source/{sourceAddressId:int}")]
    public async Task<IActionResult> RunAutomaticForSource(
        DateOnly day,
        int sourceAddressId,
        CancellationToken ct)
    {
        var result = await _service.RunAutomaticAllocationForSourceAsync(sourceAddressId, day, ct);
        return Ok(result);
    }

    [HttpPost("manual")]
    public async Task<IActionResult> RunManual(
        [FromBody] ManualTransferRequest request,
        CancellationToken ct)
    {
        var result = await _service.ExecuteManualTransferAsync(request, ct);
        return Ok(result);
    }
}
