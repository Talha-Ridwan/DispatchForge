namespace backend.Exceptions;

public class TenantNotActiveException : Exception
{
    public TenantNotActiveException() : base($"Tenant is marked for delete")
    {
    }
}