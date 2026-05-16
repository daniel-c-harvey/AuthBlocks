using Microsoft.AspNetCore.Identity;
using Models.Shared.Entities;

namespace AuthBlocksModels.Entities.Identity;

public class ApplicationUser : IdentityUser<long>, IEntity
{
    
    public bool IsDeactivated { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    
}