using backend.Entities;

namespace backend.Repositories;

public interface IDeliveryOutboxRepository
{
    public Task<DeliveryOutbox> CreateDeliveryOutbox(DeliveryOutbox deliveryOutbox);
}