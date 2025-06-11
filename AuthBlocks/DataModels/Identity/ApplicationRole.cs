using DataBlocks.DataAccess;
using Microsoft.AspNetCore.Identity;
using ScheMigrator.Migrations;

namespace AuthBlocks.DataModels.Identity;

[ScheModel]
public class ApplicationRole : IdentityRole<long>, IModel
{
    public static DataSchema Schema { get; } = DataSchema.Create<ApplicationRole>("users");

    [ScheKey("id")]
    public long ID { get; set; }

    public override long Id
    {
        get => ID;
        set => ID = value;
    }
    
    [ScheData("name")]
    public override string? Name { get; set; }
    
    [ScheData("normalized_name")]
    public override string? NormalizedName { get; set; }
    
    [ScheData("concurrency_stamp")]
    public override string? ConcurrencyStamp { get; set; }
    
    [ScheData("deleted")]
    public bool Deleted { get; set; }

    [ScheData("created")]
    public DateTime Created { get; set; }

    [ScheData("modified")]
    public DateTime Modified { get; set; }
}