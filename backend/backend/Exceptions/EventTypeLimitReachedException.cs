namespace backend.Exceptions;

public class EventTypeLimitReachedException : Exception
{
    public EventTypeLimitReachedException() : base("All 64 event types are in use for this tenant")
    {
    }
}
