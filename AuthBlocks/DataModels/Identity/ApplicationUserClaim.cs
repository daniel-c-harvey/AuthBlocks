using DataBlocks.DataAccess;
using Microsoft.AspNetCore.Identity;
using ScheMigrator.Migrations;

namespace AuthBlocks.DataModels.Identity;

[ScheModel]
public class ApplicationUserClaim : IdentityUserClaim<long>, IModel
{
    public static DataSchema Schema { get; } = DataSchema.Create<ApplicationUserClaim>("users");

    [ScheKey("id")]
    public override int Id { get; set; }
    
    [ScheData("user_id")]
    public override long UserId { get; set; }
    
    [ScheData("claim_type")]
    public override string ClaimType { get; set; }
    
    [ScheData("claim_value")]
    public override string ClaimValue { get; set; }
    
    [ScheData("deleted")]
    public bool Deleted { get; set; }
    
    [ScheData("created")]
    public DateTime Created { get; set; }
    
    [ScheData("modified")]
    public DateTime Modified { get; set; }
    
    public long ID {
        get => Id;
        set => Id = (int)value;
    }
}