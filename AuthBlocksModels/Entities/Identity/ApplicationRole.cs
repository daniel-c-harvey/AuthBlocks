using Microsoft.AspNetCore.Identity;
using Models.Shared.Entities;

namespace AuthBlocksModels.Entities.Identity;

public class ApplicationRole : IdentityRole<long>, IEntity
{
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    
    public long? ParentRoleId { get; set; }
    
    // Navigation properties
    public virtual ApplicationRole? ParentRole { get; set; }
    public virtual ICollection<ApplicationRole> ChildRoles { get; set; } = new List<ApplicationRole>();
    
    // Helper properties
    public bool IsRootRole => ParentRoleId == null;
    public bool HasChildren => ChildRoles.Any();

    public bool IsAncestorOf(ApplicationRole other)
    {
        while (true)
        {
            if (other.ParentRoleId is null || other.ParentRole is null) return false;
            if (Id == other.ParentRoleId) return true;
            other = other.ParentRole;
        }
    }

    public bool IsDescendantOf(ApplicationRole other)
    {
        if (ParentRoleId is null || ParentRole is null)
            return false;
        return ParentRoleId == other.Id || ParentRole.IsDescendantOf(other);
    }
}