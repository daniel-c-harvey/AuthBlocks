using Models.Shared.Models;

namespace AuthBlocksModels.Models;

public class PendingRegistrationModel: BaseModel
{
    public string PendingUserEmail { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsConsumed { get; set; }
    public DateTime? ConsumedAt { get; set; }
    
    public IEnumerable<RoleModel>? Roles { get; set; }
}
