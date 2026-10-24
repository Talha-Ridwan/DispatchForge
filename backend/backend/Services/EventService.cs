using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using backend.DataStructure;
using backend.DTOs;
using backend.Entities;
using backend.Exceptions;
using backend.Repositories;


namespace backend.Services;

public class EventService : IEventService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IDestinationRepository _destinationRepository;
    private readonly ILogger<EventService> _logger;

    private readonly IngestQueue _ingestQueue;

    public EventService(ITenantRepository tenantRepository, IEventTypeRepository eventTypeRepository, IDestinationRepository destinationRepository, ILogger<EventService> logger, IngestQueue ingestQueue)
    {
        _tenantRepository = tenantRepository;
        _eventTypeRepository = eventTypeRepository;
        _destinationRepository = destinationRepository;
        _logger = logger;
        _ingestQueue = ingestQueue;
    }

    private async Task<IngestReturn> ResolveRoutingAsync(Guid tenantId, EventRequestDto eventRequestDto)
    {
        var tenant = await _tenantRepository.GetTenantAsync(tenantId);
        if (tenant == null)
        {
            throw new TenantIdNotFoundException(tenantId.ToString());
        }

        if (tenant.TenantStatus != TenantStatus.Active)
        {
            _logger.LogWarning("Ingest rejected: tenant {TenantId} has status {Status}", tenantId, tenant.TenantStatus);
            throw new TenantIdNotFoundException(tenantId.ToString());
        }

        var requestedEvent = await _eventTypeRepository.GetEventTypeByNameAsync(tenantId, eventRequestDto.EventType);
        if (requestedEvent == null)
        {
            throw new UnknownEventTypeException(eventRequestDto.EventType);
        }

        var allDestinations = await _destinationRepository.GetDestinationsAsync(tenantId);
        List<Destination> matchingDestinations = new List<Destination>();
        
        for (int i = 0; i < allDestinations.Count; i++)
        {
            if ((allDestinations[i].EventFilter & (1L << requestedEvent.BitPosition)) != 0)
            {
                matchingDestinations.Add(allDestinations[i]);
            }
        }

        if (matchingDestinations.Count == 0)
        {
            throw new NoMatchingDestinationException(eventRequestDto.EventType);
        }

        return new IngestReturn(requestedEvent.Id, matchingDestinations);
    }

    public async Task IngestAsync(Guid tenantId, EventRequestDto eventRequestDto)
    {
        var ingestReturn = await ResolveRoutingAsync(tenantId, eventRequestDto);
        var now = DateTime.UtcNow;
        var newId = Guid.CreateVersion7();
        var hashedKey = SHA256.HashData(Encoding.UTF8.GetBytes(eventRequestDto.IdempotencyKey));

        var newEvent = new Event()
        {
            Id = newId,
            EventTypeId = ingestReturn.EventTypeId,
            IdempotencyKey = eventRequestDto.IdempotencyKey,
            ReceivedAt = now,
            Payload = JsonSerializer.SerializeToUtf8Bytes(eventRequestDto.Payload),
            TenantId = tenantId
        };

        var newIdempotencyRecord = new IdempotencyRecord()
        {
            CreatedAt = now,
            IdempotencyHash = hashedKey,
            TenantId = tenantId
        };

        var destinations = ingestReturn.Destinations;
        var newOutboxes = new List<DeliveryOutbox>();

        for (int i = 0; i < destinations.Count; i++)
        {
            newOutboxes.Add(new DeliveryOutbox()
            {
                EventId = newId,
                EventReceivedAt = now,
                DestinationId = destinations[i].Id,
                TenantId = tenantId,
                ScheduledAt = now
            });
        }

        var ticket = new IngestTicket(
            newEvent,
            newIdempotencyRecord,
            newOutboxes,
            new TaskCompletionSource<IngestOutcome>(TaskCreationOptions.RunContinuationsAsynchronously)
        );

        await _ingestQueue.EnqueueAsync(ticket);
        await ticket.TaskCompletionSource.Task;
    }
}