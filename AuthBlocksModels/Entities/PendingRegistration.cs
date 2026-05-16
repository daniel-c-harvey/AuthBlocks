using AuthBlocksModels.Entities.Identity;
using Models.Shared.Entities;
using Models.Shared.Models;

namespace AuthBlocksModels.Entities;

public class PendingRegistration : BaseEntity, IEntity
{
    public string PendingUserEmail { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string TokenHash { get; set; }
    public bool IsConsumed { get; set; }
    public DateTime? ConsumedAt { get; set; }
    
    public virtual IEnumerable<ApplicationRole>? Roles { get; set; }
    public long[]? RoleIds { get; set; }
}