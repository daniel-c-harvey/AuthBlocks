using DataBlocks.DataAccess;
using Microsoft.AspNetCore.Identity;
using ScheMigrator.Migrations;

namespace AuthBlocks.DataModels.Identity;

[ScheModel]
public class ApplicationUserRole : IdentityUserRole<long>, IModel
{
    public static DataSchema Schema { get; } = DataSchema.Create<ApplicationUserRole>("users");

    [ScheKey("id")]
    public long Id { get; set; }
    
    [ScheData("user_id")]
    public override long UserId { get; set; }
    
    [ScheData("role_id")]
    public override long RoleId { get; set; }
    
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