using backend.Data;
using backend.DTOs;
using backend.Entities;
using backend.Exceptions;
using backend.Mappers;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Services;

public class EventTypeService : IEventTypeService
{
    private readonly AppDbContext _dbContext;
    private readonly EventTypeMapper _eventTypeMapper;
    private readonly IEventTypeRepository _eventTypeRepository;

    // Index names EF generated in the EventTypes migration
    private const string NameIndex = "IX_EventTypes_TenantId_Name";
    private const string BitPositionIndex = "IX_EventTypes_TenantId_BitPosition";
    private const int MaxBitAttempts = 3;

    public EventTypeService(AppDbContext dbContext, EventTypeMapper eventTypeMapper, IEventTypeRepository eventTypeRepository)
    {
        _dbContext = dbContext;
        _eventTypeMapper = eventTypeMapper;
        _eventTypeRepository = eventTypeRepository;
    }
    
    public async Task<EventTypeResponseDto> CreateEventType(EventTypeRequestDto eventTypeRequestDto, Guid tenantId)
    {
        for (int attempt = 1; ; attempt++)
        {
            List<EventType> allEvents = await _eventTypeRepository.GetAllEvents(tenantId);

            List<int> usedBits = new List<int>();

            for (int i = 0; i < allEvents.Count; i++)
            {
                usedBits.Add(allEvents[i].BitPosition);
            }

            EventType newEvent = new EventType()
            {
                TenantId = tenantId,
                Name = eventTypeRequestDto.Name,
                BitPosition = GetNextBit(usedBits)
            };

            try
            {
                _dbContext.Add(newEvent);
                await _dbContext.SaveChangesAsync();
                return _eventTypeMapper.ToResponse(newEvent);
            }
            catch (DbUpdateException e)
            {
                if (e.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    _dbContext.Entry(newEvent).State = EntityState.Detached;

                    if (pg.ConstraintName == NameIndex)
                    {
                        throw new DuplicateEventTypeException(eventTypeRequestDto.Name);
                    }
                    if (pg.ConstraintName == BitPositionIndex && attempt < MaxBitAttempts)
                    {
                        continue;
                    }
                }

                throw;
            }
        }
    }

    private static int GetNextBit(List<int> usedBits)
    {
        for (int i = 0; i < 64; i++)
        {
            if (!usedBits.Contains(i))
            {
                return i;
            }
        }
        throw new EventTypeLimitReachedException();
    }

    public async Task<List<EventTypeResponseDto>> GetEventType(Guid tenantId)
    {
        List<EventType> allEvents = await _dbContext.EventTypes.Where(e => e.TenantId == tenantId)
            .ToListAsync();
        
        List<EventTypeResponseDto> responseDtos = new List<EventTypeResponseDto>();
        
        for (int i = 0; i < allEvents.Count; i++)
        {
            responseDtos.Add(_eventTypeMapper.ToResponse(allEvents[i]));
        }

        return responseDtos;
    }
}