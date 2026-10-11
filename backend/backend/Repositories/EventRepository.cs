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

    public void CreateEvent(Event @event)
    {
        _dbContext.Events.Add(@event);
    }
}