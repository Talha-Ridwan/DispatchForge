using backend.DTOs;
using backend.Entities;

namespace backend.Mappers;

public class DestinationMapper
{
    public DestinationResponseDto ToResponse(Destination destination, List<string> eventTypeNames)
    {
        return new DestinationResponseDto()
        {
            Id = destination.Id,
            Url = destination.Url,
            TimeoutMilliseconds = destination.TimeoutMilliseconds,
            CircuitState = destination.CircuitState,
            EventTypes = eventTypeNames
        };
    }

    public Destination ToEntity(DestinationRequestDto dto, Guid tenantId)
    {
        return new Destination()
        {
            TenantId = tenantId,
            Url = dto.Url,
            TimeoutMilliseconds = dto.TimeoutMilliseconds
        };
    }
}
