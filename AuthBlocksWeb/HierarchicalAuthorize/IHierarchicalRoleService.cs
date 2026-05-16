namespace AuthBlocksWeb.HierarchicalAuthorize;

public interface IHierarchicalRoleService
{
    Task<bool> HasRoleOrInheritsAsync(IList<string> userRoles, string requiredRole);
    void ClearCache();
}