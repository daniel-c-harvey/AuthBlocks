using Models.Shared.Models;

namespace AuthBlocksModels.Models;

public class UserRoleModel : IModel
{
    public long Id { get; set; }
    public UserModel User { get; set; }
    public RoleModel Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}