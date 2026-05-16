using Models.Shared.InputModels;

namespace AuthBlocksModels.InputModels;

public class RoleInputModel : IInputModel, IEquatable<RoleInputModel>
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? ConcurrencyStamp { get; set; }
    public RoleInputModel? ParentRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool Equals(RoleInputModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && 
               Name == other.Name && 
               NormalizedName == other.NormalizedName && 
               ConcurrencyStamp == other.ConcurrencyStamp && 
               Equals(ParentRole, other.ParentRole) && 
               CreatedAt.Equals(other.CreatedAt) && 
               UpdatedAt.Equals(other.UpdatedAt);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((RoleInputModel)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, NormalizedName, ConcurrencyStamp, ParentRole, CreatedAt, UpdatedAt);
    }

    public override string ToString()
    {
        return Name;
    }
} 