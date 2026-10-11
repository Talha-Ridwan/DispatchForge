using backend.Entities;

namespace backend.Repositories;

public interface IIdempotencyRecordRepository
{ 
    public void CreateIdempotencyRecord(IdempotencyRecord idempotencyRecord);
}