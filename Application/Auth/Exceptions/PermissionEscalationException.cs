namespace POSSystem.Application.Auth.Exceptions;

/// <summary>
/// Thrown when a user attempts to assign permissions they do not hold.
/// </summary>
public class PermissionEscalationException : Exception
{
    public PermissionEscalationException(string message) : base(message)
    {
    }
}
