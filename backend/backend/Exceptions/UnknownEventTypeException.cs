namespace backend.Exceptions;

public class UnknownEventTypeException : Exception
{
    public UnknownEventTypeException(string name) : base($"Event type '{name}' does not exist")
    {
    }
}
