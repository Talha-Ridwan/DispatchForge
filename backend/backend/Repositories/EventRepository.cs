using backend.Data;
using backend.Entities;

namespace backend.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _dbContext;

    public EventRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Event> CreateEvent(Event @event)
    {
        var savedEntity = await _dbContext.Events.AddAsync(@event);
        var saveStatus = await _dbContext.SaveChangesAsync();
        return @event;
    }
}