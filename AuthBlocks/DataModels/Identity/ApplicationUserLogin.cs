using DataBlocks.DataAccess;
using Microsoft.AspNetCore.Identity;
using ScheMigrator.Migrations;

namespace AuthBlocks.DataModels.Identity;

// User logins (external auth)
[ScheModel]
public class ApplicationUserLogin : IdentityUserLogin<long>, IModel
{
    public static DataSchema Schema { get; } = DataSchema.Create<ApplicationUserLogin>("users");
    [ScheKey("id")]
    public long Id { get; set; }
    
    [ScheData("login_provider")]
    public override string LoginProvider { get; set; }
    
    [ScheData("provider_key")]
    public override string ProviderKey { get; set; }
    
    [ScheData("provider_display_name")]
    public override string ProviderDisplayName { get; set; }
    
    [ScheData("user_id")]
    public override long UserId { get; set; }
    
    [ScheData("deleted")]
    public bool Deleted { get; set; }
    
    [ScheData("created")]
    public DateTime Created { get; set; }
    
    [ScheData("modified")]
    public DateTime Modified { get; set; }
    
    public long ID {
        get => Id;
        set => Id = value;
    }
}