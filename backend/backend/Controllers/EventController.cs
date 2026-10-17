using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

// TODO: tenant API key. Open to anyone until then; never deploy like this.
[AllowAnonymous]
[ApiController]
[Route("api/tenants/{tenantId:guid}/events")]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpPost]
    public async Task<IActionResult> IngestEventsAsync(Guid tenantId, EventRequestDto eventRequestDto)
    {
        await _eventService.IngestAsync(tenantId, eventRequestDto);
        return Accepted();
    }
}