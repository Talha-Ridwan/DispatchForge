using backend.Data;
using backend.Entities;

namespace backend.Repositories;

public class IdempotencyRecordRepository : IIdempotencyRecordRepository
{
    private readonly AppDbContext _dbContext;

    public IdempotencyRecordRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public void CreateIdempotencyRecord(IdempotencyRecord idempotencyRecord)
    {
        _dbContext.IdempotencyRecords.Add(idempotencyRecord); 
    }
}