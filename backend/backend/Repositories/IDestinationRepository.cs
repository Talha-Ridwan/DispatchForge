using backend.Entities;

namespace backend.Repositories;

public interface IDestinationRepository
{
    Task<Destination?> GetDestinationAsync(Guid tenantId, Guid id);
    Task<List<Destination>> GetDestinationsAsync(Guid tenantId);
    Task<Destination> AddDestinationAsync(Destination destination);
    Task<Destination> UpdateDestinationAsync(Destination destination);
    Task<int> DeleteDestinationAsync(Guid tenantId, Guid id);
    Task<int> ClearEventBitAsync(Guid tenantId, int bitPosition);
}
