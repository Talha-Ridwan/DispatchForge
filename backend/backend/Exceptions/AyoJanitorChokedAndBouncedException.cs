namespace backend.Exceptions;

public class AyoJanitorChokedAndBouncedException : Exception
{
    public AyoJanitorChokedAndBouncedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
