namespace backend.Exceptions;

public class DuplicateEventTypeException : Exception
{
    public DuplicateEventTypeException(string name) : base($"Event type '{name}' already exists")
    {
    }
}
