using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Route("api/tenants/{tenantID:Guid}/event-types")]
[ApiController]
public class EventTypeController : ControllerBase
{
    private readonly IEventTypeService _eventTypeService;

    public EventTypeController(IEventTypeService eventTypeService)
    {
        _eventTypeService = eventTypeService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateEventType(EventTypeRequestDto eventTypeRequestDto, Guid tenantId)
    {
        var response = await _eventTypeService.CreateEventType(eventTypeRequestDto, tenantId);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEvents(Guid tenantId)
    {
        var response = await _eventTypeService.GetEventType(tenantId);
        return Ok(response);
    }

    [HttpDelete("{id:Guid}")]
    public async Task<IActionResult> DeleteEvent(Guid tenantId, Guid id)
    {
        var response = await _eventTypeService.MarkEventForDeathAndClearBitsAsync(tenantId, id);
        return response ? NoContent() : NotFound();
    }
}