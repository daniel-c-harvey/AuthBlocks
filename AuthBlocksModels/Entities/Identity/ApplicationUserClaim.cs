using Microsoft.AspNetCore.Identity;

namespace AuthBlocksModels.Entities.Identity;

public class ApplicationUserClaim : IdentityUserClaim<long>
{
    public bool Deleted { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}