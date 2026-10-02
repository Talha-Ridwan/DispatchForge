using backend.Data;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class EventTypeRepository : IEventTypeRepository
{
    private readonly AppDbContext _dbContext;
    public EventTypeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventType?> GetEventTypeAsync(Guid tenantId, Guid id)
    {
        return await _dbContext.EventTypes.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);
    }

    public async Task<IEnumerable<EventType>> GetEventTypesAsync(Guid tenantId)
    {
        return await _dbContext.EventTypes.Where(e => e.TenantId == tenantId).ToListAsync();
    }

    public async Task<EventType> AddEventTypeAsync(EventType eventType)
    {
        _dbContext.EventTypes.Add(eventType);
        await _dbContext.SaveChangesAsync();
        return eventType;
    }

    public async Task<EventType> UpdateEventTypeAsync(EventType eventType)
    {
        _dbContext.EventTypes.Update(eventType);
        await _dbContext.SaveChangesAsync();
        return eventType;
    }

    public async Task<int> DeleteEventTypeAsync(Guid id)
    {
        return await _dbContext.EventTypes.Where(e => e.Id == id).ExecuteDeleteAsync();
    }

    public async Task<List<EventType>> GetAllEvents(Guid tenantId)
    {
        return await _dbContext.EventTypes.Where(type => type.TenantId == tenantId).ToListAsync();
    }
}
