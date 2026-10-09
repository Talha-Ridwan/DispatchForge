namespace backend.Repositories;

public interface IPartitionRepository
{
    Task EnsureWeekPartitionAsync(DateTime monday, CancellationToken cancellationToken);
    Task<List<DateTime>> GetPartitionMondaysAsync(CancellationToken cancellationToken);
    Task DropWeekPartitionAsync(DateTime monday, CancellationToken cancellationToken);
}
