namespace AuthBlocksModels.Models;

public class CreatePendingRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public RoleModel[]? Roles { get; set; }
    public string ReturnHost { get; set; } = string.Empty;
}