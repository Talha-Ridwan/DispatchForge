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
            EventFilter = NameToBitMask(destinationRequestDto.EventTypes,tenantEvents),
            TenantId = tenantId,
            TimeoutMilliseconds = destinationRequestDto.TimeoutMilliseconds,
            Url = destinationRequestDto.Url
        };

        var savedDestination = await _destinationRepository.AddDestinationAsync(destination);
        return _destinationMapper.ToResponse(savedDestination, destinationRequestDto.EventTypes);
    }

    private static long NameToBitMask(List<string> eventTypes, List<EventType> tenantEvents)
    {
        long mask = 0;

        for (int i = 0; i < eventTypes.Count; i++)
        {
            bool found = false;
            for (int j = 0; j < tenantEvents.Count; j++)
            {
                if (tenantEvents[j].Name == eventTypes[i] && tenantEvents[j].Status != EventTypeStatus.MarkedForDeath)
                {   
                    /* OR flips this event's bit on and leaves the others alone.
                     After the loop, every requested event has its own bit lit. */
                    mask |= 1L << tenantEvents[j].BitPosition;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new UnknownEventTypeException(eventTypes[i]);
            }
        }

        return mask;
    }

    private static List<string> MaskToNames(long mask, List<EventType> tenantEvents)
    {
        var names = new List<string>();

        for (int i = 0; i < tenantEvents.Count; i++)
        {
            /*
             * Keep only overlapping bits on, so whenever bitwise AND gives
             * us non-zero, it indicates it's included in the mask
             */
            if ((mask & 1L << tenantEvents[i].BitPosition) != 0)
            {
                names.Add(tenantEvents[i].Name);
            }
        }
        
        return names;
    }

    private static string GenerateKey()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }



    
    public async Task<DestinationResponseDto?> GetDestinationAsync(Guid tenantId, Guid id)
    {
        var requestedDestination = await _destinationRepository.GetDestinationAsync(tenantId, id);
        if (requestedDestination == null) return null;
        var tenantEvents = await _eventTypeRepository.GetEventTypesAsync(tenantId);
        List<string> namesOfEvents = MaskToNames(requestedDestination.EventFilter, tenantEvents);
        
        return _destinationMapper.ToResponse(requestedDestination, namesOfEvents);
    } 

    public async Task<List<DestinationResponseDto>> GetDestinationsAsync(Guid tenantId)
    {
        var destinations = await _destinationRepository.GetDestinationsAsync(tenantId);
        
        var tenantEvents = await _eventTypeRepository.GetEventTypesAsync(tenantId);

        List<DestinationResponseDto> responseDtos = new List<DestinationResponseDto>();

        for (int i = 0; i < destinations.Count; i++)
        {
            List<string> namesOfEvents = MaskToNames(destinations[i].EventFilter, tenantEvents);
            responseDtos.Add(_destinationMapper.ToResponse(destinations[i], namesOfEvents));
        }

        return responseDtos;
    }

    public async Task<DestinationResponseDto?> UpdateDestinationAsync(DestinationRequestDto destinationRequestDto, Guid tenantId, Guid id)
    {
        var existingDestination = await _destinationRepository.GetDestinationAsync(tenantId, id);
        if (existingDestination == null) return null;

        var tenantEvents = await _eventTypeRepository.GetEventTypesAsync(tenantId);

        existingDestination.Url = destinationRequestDto.Url;
        existingDestination.TimeoutMilliseconds = destinationRequestDto.TimeoutMilliseconds;
        existingDestination.EventFilter = NameToBitMask(destinationRequestDto.EventTypes, tenantEvents);

        var updatedDestination = await _destinationRepository.UpdateDestinationAsync(existingDestination);
        return _destinationMapper.ToResponse(updatedDestination, destinationRequestDto.EventTypes);
    }

    public async Task<bool> DeleteDestinationAsync(Guid tenantId, Guid id)
    {
        int deletedRows = await _destinationRepository.DeleteDestinationAsync(tenantId, id);
        return deletedRows > 0;
    }
}
