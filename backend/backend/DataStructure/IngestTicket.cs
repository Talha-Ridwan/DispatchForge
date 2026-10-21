using backend.Entities;

namespace backend.DataStructure;

public record IngestTicket(
    Event Event,
    IdempotencyRecord IdempotencyRecord,
    List<DeliveryOutbox> DeliveryOutboxes,
    TaskCompletionSource<IngestOutcome> TaskCompletionSource
    );
