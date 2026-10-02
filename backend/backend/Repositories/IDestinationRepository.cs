using backend.Entities;

namespace backend.Repositories;

public interface IDestinationRepository
{
    Task<Destination?> GetDestinationAsync(Guid tenantId, Guid id);
    Task<IEnumerable<Destination>> GetDestinationsAsync(Guid tenantId);
    Task<Destination> AddDestinationAsync(Destination destination);
    Task<Destination> UpdateDestinationAsync(Destination destination);
    Task<int> DeleteDestinationAsync(Guid tenantId, Guid id);
}
