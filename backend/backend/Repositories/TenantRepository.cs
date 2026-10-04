using backend.Data;
using backend.DataStructure;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly AppDbContext _dbContext;
    public TenantRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Tenant> AddTenantAsync(Tenant tenant)
    {
        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();
        return tenant;
    }

    public async Task<Tenant?> GetTenantAsync(Guid id)
    {
        var find = await _dbContext.Tenants.FindAsync(id);
        return find;
    }

    public async Task<IEnumerable<Tenant>>GetTenantsAsync()
    {
        return await _dbContext.Tenants.ToListAsync();
    }

    public async Task<Tenant> UpdateTenantAsync(Tenant tenant)
    {
        _dbContext.Tenants.Update(tenant);
        await _dbContext.SaveChangesAsync();
        return tenant;
    }

    public async Task<int> DeleteTenantAsync()
    {
        return await _dbContext.Tenants.Where(t => t.TenantStatus == TenantStatus.MarkedForDeath).ExecuteDeleteAsync(); //Cannot succeed always as one restrict constraint will kill the whole bulk, needs to change later.
    }
}