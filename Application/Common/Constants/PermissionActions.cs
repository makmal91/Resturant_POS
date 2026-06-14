namespace POSSystem.Application.Common.Constants;

public static class PermissionActions
{
    public const string View = "View";
    public const string Create = "Create";
    public const string Edit = "Edit";
    public const string Delete = "Delete";
    public const string Export = "Export";
    public const string Upload = "Upload";

    public static readonly IReadOnlyList<string> All =
    [
        View,
        Create,
        Edit,
        Delete,
        Export,
        Upload
    ];
}
