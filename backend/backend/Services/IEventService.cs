using backend.DTOs;

namespace backend.Services;

public interface IEventService
{
    public Task ResolveRoutingAsync(Guid tenantId, EventRequestDto eventRequestDto); //return type and input parameters will be changed
}