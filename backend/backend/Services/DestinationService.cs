using System.Security.Cryptography;
using backend.DataStructure;
using backend.DTOs;
using backend.Entities;
using backend.Exceptions;
using backend.Mappers;
using backend.Repositories;

namespace backend.Services;

public class DestinationService : IDestinationService
{
    private readonly IDestinationRepository _destinationRepository;
    private readonly DestinationMapper _destinationMapper;
    private readonly IEventTypeRepository _eventTypeRepository;

    public DestinationService(IDestinationRepository destinationRepository, DestinationMapper destinationMapper, IEventTypeRepository eventTypeRepository)
    {
        _destinationRepository = destinationRepository;
        _destinationMapper = destinationMapper;
        _eventTypeRepository = eventTypeRepository;
    }

    public async Task<DestinationResponseDto> CreateDestinationAsync(DestinationRequestDto destinationRequestDto, Guid tenantId)
    {
        var tenantEvents = await _eventTypeRepository.GetEventTypesAsync(tenantId);

        var destination = new Destination()
        {
            CircuitState = CircuitState.Closed,
            CryptographyKeyPrimary = GenerateKey(),
            CryptographyKeySecondary = "",
            EventFilter = NameToBitMask(destinationRequestDto.EventTypes,tenantEvents, tenantId),
            TenantId = tenantId,
            TimeoutMilliseconds = destinationRequestDto.TimeoutMilliseconds,
            Url = destinationRequestDto.Url
        };

        var savedDestination = await _destinationRepository.AddDestinationAsync(destination);
        return _destinationMapper.ToResponse(savedDestination, destinationRequestDto.EventTypes);
    }

    private long NameToBitMask(List<string> eventTypes, List<EventType> tenantEvents, Guid tenantId)
    {
        long mask = 0;
        
        for(int i = 0; i < eventTypes.Count; i++)
        {
            bool found = false;
            for (int j = 0; j < tenantEvents.Count; j++)
            {
                if (tenantEvents[j].Name == eventTypes[i] && tenantEvents[j].Status != EventTypeStatus.MarkedForDeath)
                {
                    mask |= 1L << tenantEvents[j].BitPosition;
                    found = true;
                }
            }

            if (!found)
            {
                throw new UnknownEventTypeException(eventTypes[i]);
            }
        }

        return mask;
    }
    private static string GenerateKey()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
    



    public Task<DestinationResponseDto?> GetDestinationAsync(Guid tenantId, Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<List<DestinationResponseDto>> GetDestinationsAsync(Guid tenantId)
    {
        throw new NotImplementedException();
    }

    public Task<DestinationResponseDto?> UpdateDestinationAsync(DestinationRequestDto destinationRequestDto, Guid tenantId, Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteDestinationAsync(Guid tenantId, Guid id)
    {
        throw new NotImplementedException();
    }
}