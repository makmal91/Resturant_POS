namespace POSSystem.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public RequirePermissionAttribute(string module, string action)
    {
        Module = module;
        Action = action;
    }

    public string Module { get; }
    public string Action { get; }
}
