using Microsoft.AspNetCore.Identity;

namespace AuthBlocksModels.Entities.Identity;

public class ApplicationUserToken : IdentityUserToken<long>
{
    public long Id { get; set; }
    public bool Deleted { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}