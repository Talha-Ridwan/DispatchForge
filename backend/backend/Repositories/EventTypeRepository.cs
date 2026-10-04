using backend.Data;
using backend.DataStructure;
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

    public async Task<List<EventType>> GetEventTypesAsync(Guid tenantId)
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

    public async Task<int> DeleteMarkedEventTypesAsync()
    {
        return await _dbContext.EventTypes
        .Where(e => e.Status == EventTypeStatus.MarkedForDeath)
        .ExecuteDeleteAsync();
    }


}
