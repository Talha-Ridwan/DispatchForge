using backend.Entities;

namespace backend.Repositories;

public interface IEventRepository
{ 
    public void CreateEvent(Event @event);
}
