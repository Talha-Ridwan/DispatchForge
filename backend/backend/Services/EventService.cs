using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using backend.Data;
using backend.DataStructure;
using backend.DTOs;
using backend.Entities;
using backend.Exceptions;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Services;

public class EventService : IEventService
{
    private const string IdempotencyRecordPk = "PK_IdempotencyRecords";

    private readonly AppDbContext _dbContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IDestinationRepository _destinationRepository;
    private readonly ILogger<EventService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IDeliveryOutboxRepository _deliveryOutboxRepository;
    private readonly IIdempotencyRecordRepository _idempotencyRecordRepository;
    
    public EventService(AppDbContext dbContext, ITenantRepository tenantRepository, IEventTypeRepository eventTypeRepository, IDestinationRepository destinationRepository, ILogger<EventService> logger, IEventRepository eventRepository, IDeliveryOutboxRepository deliveryOutboxRepository, IIdempotencyRecordRepository idempotencyRecordRepository)
    {
        _dbContext = dbContext;
        _tenantRepository = tenantRepository;
        _eventTypeRepository = eventTypeRepository;
        _destinationRepository = destinationRepository;
        _logger = logger;
        _eventRepository = eventRepository;
        _deliveryOutboxRepository = deliveryOutboxRepository;
        _idempotencyRecordRepository = idempotencyRecordRepository;
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

        _eventRepository.CreateEvent(newEvent);
        _idempotencyRecordRepository.CreateIdempotencyRecord(newIdempotencyRecord);
        _deliveryOutboxRepository.CreateDeliveryOutboxes(newOutboxes);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            if (e.InnerException is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation
                && pg.ConstraintName == IdempotencyRecordPk)
            {
                return;
            }

            throw;
        }
    }
}