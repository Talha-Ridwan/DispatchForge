using backend.DTOs;
using backend.Entities;

namespace backend.Repositories;

public interface IEventRepository
{
    public Task<Event> CreateEvent(Guid tenantId, Guid eventTypeId, EventRequestDto eventRequestDto);
}