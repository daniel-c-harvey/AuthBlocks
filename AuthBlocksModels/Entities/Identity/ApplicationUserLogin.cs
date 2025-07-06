using Microsoft.AspNetCore.Identity;

namespace AuthBlocksModels.Entities.Identity;

// User logins (external auth)
public class ApplicationUserLogin : IdentityUserLogin<long>
{
    public long Id { get; set; }
    public bool Deleted { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}