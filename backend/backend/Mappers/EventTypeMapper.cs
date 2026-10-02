using backend.DTOs;
using backend.Entities;

namespace backend.Mappers;

public class EventTypeMapper
{
    public EventTypeResponseDto ToResponse(EventType eventType)
    {
        return new EventTypeResponseDto()
        {
            Id = eventType.Id,
            Name = eventType.Name
        };
    }

    public EventType ToEntity(EventTypeRequestDto dto, Guid tenantId)
    {
        return new EventType()
        {
            TenantId = tenantId,
            Name = dto.Name
        };
    }
}
