using backend.Entities;

namespace backend.Repositories;

public interface IDedupTableRepository
{
    public Task<DedupTable?> CheckDedup(Guid tenantId, byte[] idempotencyHash);
    public Task CreateDedupEntry(DedupTable dedupTable);
}