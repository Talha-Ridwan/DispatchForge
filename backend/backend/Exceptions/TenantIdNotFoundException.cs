using System;

namespace backend.Exceptions;

public class TenantIdNotFoundException : Exception 
{
    public TenantIdNotFoundException(string name) : base($"Tenant with {name} not found"){}
}
