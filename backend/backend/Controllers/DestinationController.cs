using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Route("api/tenants/{tenantId:guid}/destinations")]
[ApiController]
public class DestinationController : ControllerBase
{
    private readonly IDestinationService _destinationService;

    public DestinationController(IDestinationService destinationService)
    {
        _destinationService = destinationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDestination(DestinationRequestDto destinationRequestDto, Guid tenantId)
    {
        var response = await _destinationService.CreateDestinationAsync(destinationRequestDto, tenantId);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDestination(Guid tenantId, Guid id)
    {
        var response = await _destinationService.GetDestinationAsync(tenantId, id);
        return response == null ? NotFound() : Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetDestinations(Guid tenantId)
    {
        var response = await _destinationService.GetDestinationsAsync(tenantId);
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDestination(DestinationRequestDto destinationRequestDto, Guid tenantId, Guid id)
    {
        var response = await _destinationService.UpdateDestinationAsync(destinationRequestDto, tenantId, id);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDestination(Guid tenantId, Guid id)
    {
        var response = await _destinationService.DeleteDestinationAsync(tenantId, id);
        return response ? NoContent() : NotFound();
    }
}