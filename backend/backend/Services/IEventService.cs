using backend.DataStructure;
using backend.DTOs;

namespace backend.Services;

public interface IEventService
{
    Task IngestAsync(Guid tenantId, EventRequestDto eventRequestDto);
}