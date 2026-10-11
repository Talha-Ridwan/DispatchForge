using backend.Data;
using backend.Entities;

namespace backend.Repositories;

public class DeliveryOutboxRepository : IDeliveryOutboxRepository
{
    private readonly AppDbContext _dbContext;

    public DeliveryOutboxRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void CreateDeliveryOutboxes(List<DeliveryOutbox> deliveryOutboxes)
    {
        _dbContext.DeliveryOutboxes.AddRange(deliveryOutboxes);
    }
}