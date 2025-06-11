using DataBlocks.DataAccess;
using Microsoft.AspNetCore.Identity;
using ScheMigrator.Migrations;

namespace AuthBlocks.DataModels.Identity;

[ScheModel]
public class ApplicationUserToken : IdentityUserToken<long>, IModel
{
    public static DataSchema Schema { get; } = DataSchema.Create<ApplicationUserToken>("users");

    [ScheKey("id")]
    public long ID { get; set; }

    [ScheKey("user_id")]
    public override long UserId { get; set; }
    
    [ScheData("login_provider")]
    public override string LoginProvider { get; set; }
    
    [ScheData("name")]
    public override string Name { get; set; }
    
    [ScheData("value")]
    public override string Value { get; set; }
    
    [ScheData("deleted")]
    public bool Deleted { get; set; }
    
    [ScheData("created")]
    public DateTime Created { get; set; }
    
    [ScheData("modified")]
    public DateTime Modified { get; set; }
    
}