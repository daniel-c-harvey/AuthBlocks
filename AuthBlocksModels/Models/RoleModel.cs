using Models.Shared.Models;

namespace AuthBlocksModels.Models;

public class RoleModel : IModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? ConcurrencyStamp { get; set; }
    public RoleModel? ParentRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
} 