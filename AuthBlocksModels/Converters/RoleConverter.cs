using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.InputModels;
using AuthBlocksModels.Models;
using Models.Shared.Converters;

namespace AuthBlocksModels.Converters;

public class RoleEntityToModelConverter : IEntityToModelConverter<ApplicationRole, RoleModel>
{
    public static RoleModel Convert(ApplicationRole entity)
    {
        return new RoleModel
        {
            Id = entity.Id,
            Name = entity.Name ?? string.Empty,
            NormalizedName = entity.NormalizedName ?? string.Empty,
            ConcurrencyStamp = entity.ConcurrencyStamp,
            ParentRole = entity.ParentRole is null ? null : Convert(entity.ParentRole),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public static ApplicationRole Convert(RoleModel model)
    {
        return new ApplicationRole
        {
            Id = model.Id,
            Name = model.Name,
            NormalizedName = model.NormalizedName,
            ConcurrencyStamp = model.ConcurrencyStamp,
            ParentRoleId = model.ParentRole?.Id,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
        };
    }
}

public class RoleModelToInputConverter : IModelToInputConverter<RoleModel, RoleInputModel>
{
    public static RoleInputModel Convert(RoleModel model)
    {
        return new RoleInputModel
        {
            Id = model.Id,
            Name = model.Name,
            NormalizedName = model.NormalizedName,
            ConcurrencyStamp = model.ConcurrencyStamp,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            ParentRole = model.ParentRole is null ? null : Convert(model.ParentRole),
        };
    }

    public static RoleModel Convert(RoleInputModel input)
    {
        return new RoleModel
        {
            Id = input.Id,
            Name = input.Name,
            NormalizedName = input.NormalizedName,
            ConcurrencyStamp = input.ConcurrencyStamp,
            CreatedAt = input.CreatedAt,
            UpdatedAt = input.UpdatedAt,
            ParentRole = input.ParentRole is null ? null : Convert(input.ParentRole),
        };
    }
}