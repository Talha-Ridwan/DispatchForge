using backend.Data;
using backend.Entities;

namespace backend.Repositories;

public class DedupTableRepository : IDedupTableRepository
{
    private readonly AppDbContext _dbContext;

    public DedupTableRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DedupTable?> CheckDedup(Guid tenantId, byte[] idempotencyHash)
    {
        var find = await _dbContext.DedupTables.FindAsync(tenantId, idempotencyHash);
        return find;
    }

    public async Task CreateDedupEntry(DedupTable dedupTable)
    {
        await _dbContext.DedupTables.AddAsync(dedupTable);
        await _dbContext.SaveChangesAsync();
    }
}