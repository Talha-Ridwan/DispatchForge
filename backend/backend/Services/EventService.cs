using backend.Data;
using backend.DataStructure;
using backend.DTOs;
using backend.Exceptions;
using backend.Repositories;

namespace backend.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantRepository _tenantRepository;

    public EventService(AppDbContext dbContext, ITenantRepository tenantRepository)
    {
        _dbContext = dbContext;
        _tenantRepository = tenantRepository;
    }

    public async Task ResolveRoutingAsync(Guid tenantId, EventRequestDto eventRequestDto)
    {
        var tenant = await _tenantRepository.GetTenantAsync(tenantId);
        if (tenant == null)
        {
            throw new TenantIdNotFoundException(tenantId.ToString());
        }

        if (tenant.TenantStatus == TenantStatus.MarkedForDeath)
        {
            throw new TenantNotActiveException();
        }
    }
}