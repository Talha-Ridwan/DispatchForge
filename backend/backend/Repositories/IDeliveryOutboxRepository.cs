using backend.Entities;

namespace backend.Repositories;

public interface IDeliveryOutboxRepository
{
    public void CreateDeliveryOutboxes(List<DeliveryOutbox> deliveryOutboxes);
}