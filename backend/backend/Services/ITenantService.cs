using backend.DTOs;

namespace backend.Services;

public interface ITenantService
{
    public Task<TenantResponseDto> CreateTenant(TenantRequestDto tenantRequestDto);
    public Task<TenantResponseDto?> UpdateTenant(Guid id, TenantRequestDto tenantRequestDto);
    public Task<TenantResponseDto?> GetTenant(Guid id);
    public Task<bool> MarkTenantForDeath(Guid id);
    public Task<List<TenantResponseDto>> GetTenants();
}