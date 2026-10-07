using backend.DTOs;
using backend.Entities;

namespace backend.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetTenantAsync(Guid id);
    Task<IEnumerable<Tenant>> GetTenantsAsync();
    Task<Tenant> AddTenantAsync(Tenant tenant);
    Task<Tenant> UpdateTenantAsync(Tenant tenant);
}