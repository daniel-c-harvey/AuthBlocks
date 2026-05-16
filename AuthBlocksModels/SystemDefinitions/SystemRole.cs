using GenBlocks.Generators;
using NetBlocks.Utilities;

namespace AuthBlocksModels.SystemDefinitions;

[GenerateConstants]
public class SystemRole : Enumeration<SystemRole>
{
    public static SystemRole UserAdmin = new(2, "UserAdmin");
    public static SystemRole Admin = new(1, "Admin", [UserAdmin]);
    
    public SystemRole? ParentRole { get; internal set; }
    
    private SystemRole(int id, string name, SystemRole[]? childRoles = null) 
        : base(id, name)
    {
        if (childRoles != null)
        {
            foreach (var child in childRoles)
            {
                child.ParentRole = this;
            }
        }
    }
    
    public IEnumerable<SystemRole> GetAncestors()
    {
        var current = ParentRole;
        while (current != null)
        {
            yield return current;
            current = current.ParentRole;
        }
    }
    
    public bool InheritsFrom(SystemRole role) => GetAncestors().Any(r => r.Id == role.Id);
}