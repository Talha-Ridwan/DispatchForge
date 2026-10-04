using backend.DataStructure;
using backend.DTOs;
using backend.Entities;
using backend.Mappers;
using backend.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

namespace backend.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly TenantMapper _tenantMapper;

    public TenantService(ITenantRepository tenantRepository, TenantMapper tenantMapper)
    {
        _tenantRepository = tenantRepository;
        _tenantMapper = tenantMapper;
    }

    public async Task<TenantResponseDto> CreateTenant(TenantRequestDto tenantRequestDto)
    {
        Tenant entity = _tenantMapper.ToEntity(tenantRequestDto);
        return _tenantMapper.ToResponse(await _tenantRepository.AddTenantAsync(entity));
    }

    public async Task<TenantResponseDto?> UpdateTenant(Guid id, TenantRequestDto tenantRequestDto)
    {
        var toEdit = await _tenantRepository.GetTenantAsync(id);
        if (toEdit == null)
        {
            return null;
        }

        toEdit.Name = tenantRequestDto.Name;
        toEdit.TenantStatus = tenantRequestDto.Status;
        toEdit.MaxConcurrentDeliveries = tenantRequestDto.MaxConcurrentDeliveries;
        toEdit.RateLimitPerMinute = tenantRequestDto.RateLimitPerMinute;

        return _tenantMapper.ToResponse(await _tenantRepository.UpdateTenantAsync(toEdit));
    }

    public async Task<TenantResponseDto?> GetTenant(Guid id)
    {
        var tenant = await _tenantRepository.GetTenantAsync(id);
        if (tenant == null)
        {
            return null;
        }
        return _tenantMapper.ToResponse(tenant);
    }

    public async Task<bool> MarkTenantForDeath(Guid id)
    {
        Tenant? tenant = await _tenantRepository.GetTenantAsync(id);
        if (tenant == null)
        {
            return false;
        }

        tenant.TenantStatus = TenantStatus.MarkedForDeath;
        await _tenantRepository.UpdateTenantAsync(tenant);
        return true;
    }

    public async Task<List<TenantResponseDto>> GetTenants()
    {
        IEnumerable<Tenant> tenants  = await _tenantRepository.GetTenantsAsync();
        List<TenantResponseDto> response = new List<TenantResponseDto>();
        foreach (var tenant in tenants)
        {
            response.Add(_tenantMapper.ToResponse(tenant));
        }

        return response;
    }
}