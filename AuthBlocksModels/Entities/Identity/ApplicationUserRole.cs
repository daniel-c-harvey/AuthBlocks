using Microsoft.AspNetCore.Identity;
using Models.Shared.Entities;

namespace AuthBlocksModels.Entities.Identity;

public class ApplicationUserRole : IdentityUserRole<long>, IEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public virtual ApplicationUser User { get; set; }
    public long RoleId { get; set; } 
    public virtual ApplicationRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
}