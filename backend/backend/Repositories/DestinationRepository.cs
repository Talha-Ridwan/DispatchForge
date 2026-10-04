using backend.Data;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class DestinationRepository : IDestinationRepository
{
    private readonly AppDbContext _dbContext;
    public DestinationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Destination?> GetDestinationAsync(Guid tenantId, Guid id)
    {
        return await _dbContext.Destinations.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId);
    }

    public async Task<IEnumerable<Destination>> GetDestinationsAsync(Guid tenantId)
    {
        return await _dbContext.Destinations.Where(d => d.TenantId == tenantId).ToListAsync();
    }

    public async Task<Destination> AddDestinationAsync(Destination destination)
    {
        _dbContext.Destinations.Add(destination);
        await _dbContext.SaveChangesAsync();
        return destination;
    }

    public async Task<Destination> UpdateDestinationAsync(Destination destination)
    {
        _dbContext.Destinations.Update(destination);
        await _dbContext.SaveChangesAsync();
        return destination;
    }

    public async Task<int> DeleteDestinationAsync(Guid tenantId, Guid id)
    {
        return await _dbContext.Destinations.Where(d => d.Id == id && d.TenantId == tenantId).ExecuteDeleteAsync();
    }

    public async Task<int> ClearEventBitAsync(Guid tenantId, int bitPosition)
    {
        return await _dbContext.Destinations
            .Where(d => d.TenantId == tenantId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.EventFilter, d => d.EventFilter & ~(1L << bitPosition)));
    }
}
