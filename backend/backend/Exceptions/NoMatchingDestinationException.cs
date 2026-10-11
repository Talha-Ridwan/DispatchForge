namespace backend.Exceptions;

public class NoMatchingDestinationException : Exception 
{
    public NoMatchingDestinationException(string name) : base($"Event with name {name} doesn't have a destination")
    {
    }
}