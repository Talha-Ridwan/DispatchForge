using backend.DTOs;

namespace backend.Services;

public interface IEventTypeService
{
    public Task<EventTypeResponseDto> CreateEventType(EventTypeRequestDto eventTypeRequestDto, Guid tenantId);
    public Task<List<EventTypeResponseDto>> GetEventType(Guid tenantId);
}