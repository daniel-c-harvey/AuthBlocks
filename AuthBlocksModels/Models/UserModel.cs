using Models.Shared.Models;

namespace AuthBlocksModels.Models;

public class UserModel : IModel
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public bool IsDeactivated { get; set; }
    public bool CanDelete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
} 