
using EnergyManagement.Services.Transfers;
using Microsoft.AspNetCore.Mvc;

namespace EnergyManagement.Services.Transfers;

[ApiController]
[Route("api/v1/transfers")]
public class TransfersController : ControllerBase
{
    private readonly ITransferWorkflowScheduledService _service;

    public TransfersController(ITransferWorkflowScheduledService service)
    {
        _service = service;
    }

    [HttpPost("auto/{day}")]
    public async Task<IActionResult> RunAutomatic(DateOnly day, CancellationToken ct)
    {
        var result = await _service.RunAutomaticWorkflowAsync(day, ct);
        return Ok(result);
    }

    [HttpPost("auto/{day}/source/{sourceAddressId:int}")]
    public async Task<IActionResult> RunAutomaticForSource(
        DateOnly day,
        int sourceAddressId,
        CancellationToken ct)
    {
        var result = await _service.RunAutomaticWorkflowForSourceAsync(sourceAddressId, day, ct);
        return Ok(result);
    }

}

