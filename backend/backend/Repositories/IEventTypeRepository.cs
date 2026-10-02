using backend.Entities;

namespace backend.Repositories;

public interface IEventTypeRepository
{
    Task<EventType?> GetEventTypeAsync(Guid tenantId, Guid id);
    Task<IEnumerable<EventType>> GetEventTypesAsync(Guid tenantId);
    Task<EventType> AddEventTypeAsync(EventType eventType);
    Task<EventType> UpdateEventTypeAsync(EventType eventType);
    Task<int> DeleteEventTypeAsync(Guid id);
    public Task<List<EventType>> GetAllEvents(Guid tenantId);
}
