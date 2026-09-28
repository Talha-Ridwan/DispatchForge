using backend.DTOs;
using backend.Entities;

namespace backend.Mappers;

public class TenantMapper
{
    public TenantResponseDto ToResponse(Tenant tenant)
    {
        TenantResponseDto tenantResponseDto = new TenantResponseDto()
        {
            Id = tenant.Id,
            MaxConcurrentDeliveries = tenant.MaxConcurrentDeliveries,
            Subscription = tenant.Subscription,
            Name = tenant.Name,
            RateLimitPerMinute = tenant.RateLimitPerMinute,
            Status = tenant.TenantStatus
        };

        return tenantResponseDto;
    }

    public Tenant ToEntity(TenantRequestDto tenantRequestDto)
    {
        return new Tenant()
        {
            Name = tenantRequestDto.Name,
            Subscription = tenantRequestDto.Subscription,
            TenantStatus = tenantRequestDto.Status,
            MaxConcurrentDeliveries = tenantRequestDto.MaxConcurrentDeliveries,
            RateLimitPerMinute = tenantRequestDto.RateLimitPerMinute
        };
    }
}