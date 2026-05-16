using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using Models.Shared.Converters;

namespace AuthBlocksModels.Converters;

public class UserRoleEntityToModelConverter : IEntityToModelConverter<ApplicationUserRole, UserRoleModel>
{
    public static UserRoleModel Convert(ApplicationUserRole entity)
    {
        return new UserRoleModel
        {
            Id = entity.Id,
            User = UserEntityToModelConverter.Convert(entity.User),
            Role = RoleEntityToModelConverter.Convert(entity.Role),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public static ApplicationUserRole Convert(UserRoleModel dto)
    {
        return new ApplicationUserRole
        {
            Id = dto.Id,
            UserId = dto.User.Id,
            RoleId = dto.Role.Id,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
        };
    }
}