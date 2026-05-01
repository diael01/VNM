using Microsoft.AspNetCore.Mvc;

namespace EnergyManagement.Api.Controllers;

[ApiController]
[Route("api/simulators/transfer-execution")]
public class TransferExecutionSimulatorController : ControllerBase
{
    private readonly ILogger<TransferExecutionSimulatorController> _logger;

    public TransferExecutionSimulatorController(
        ILogger<TransferExecutionSimulatorController> logger)
    {
        _logger = logger;
    }

    [HttpPost("execute")]
    public ActionResult<TransferExecutionResultDto> Execute(
        [FromBody] TransferExecutionSimulatorRequestDto request)
    {
        if (request.AmountKwh <= 0)
        {
            return BadRequest(new TransferExecutionResultDto
            {
                Success = false,
                ErrorMessage = "AmountKwh must be greater than zero."
            });
        }

        var reference =
            $"SIM-{request.BalanceDay:yyyyMMdd}-{request.SourceAddressId}-{request.DestinationAddressId}-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Transfer execution simulator called. WorkflowId={WorkflowId}, Source={Source}, Destination={Destination}, AmountKwh={AmountKwh}, Reference={Reference}",
            request.WorkflowId,
            request.SourceAddressId,
            request.DestinationAddressId,
            request.AmountKwh,
            reference);

        return Ok(new TransferExecutionResultDto
        {
            Success = true,
            ExternalReference = reference,
            ExecutedAtUtc = DateTime.UtcNow
        });
    }
}