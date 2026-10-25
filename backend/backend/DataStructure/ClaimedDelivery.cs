namespace backend.DataStructure;

public record ClaimedDelivery(
    long Id,
    Guid EventId,
    DateTime EventReceivedAt,
    Guid DestinationId,
    int AttemptCount,
    byte[] Payload,
    string Url,
    byte[] SigningKey,
    int TimeoutMilliseconds
    );
