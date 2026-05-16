using AuthBlocksModels.Entities;
using AuthBlocksModels.InputModels;
using AuthBlocksModels.Models;
using Models.Shared.Converters;
using Models.Shared.Models;

namespace AuthBlocksModels.Converters;

public class PendingRegistrationEntityToModelConverter : IEntityToModelConverter<PendingRegistration, PendingRegistrationModel>
{
    public static PendingRegistrationModel Convert(PendingRegistration entity)
    {
        return new PendingRegistrationModel()
        {
            Id = entity.Id,
            PendingUserEmail = entity.PendingUserEmail,
            ExpiresAt = entity.ExpiresAt,
            IsConsumed = entity.IsConsumed,
            ConsumedAt = entity.ConsumedAt,
            Roles = entity.Roles?.Select(RoleEntityToModelConverter.Convert).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public static PendingRegistration Convert(PendingRegistrationModel model)
    {
        return new PendingRegistration()
        {
            Id = model.Id,
            PendingUserEmail = model.PendingUserEmail,
            ExpiresAt = model.ExpiresAt,
            IsConsumed = model.IsConsumed,
            ConsumedAt = model.ConsumedAt,
            RoleIds = model.Roles?.Select(r => r.Id).ToArray(),
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,       
        };
    }
}

public class PendingRegistrationModelToInputConverter : IModelToInputConverter<PendingRegistrationModel, PendingRegistrationInputModel>
{
    public static PendingRegistrationInputModel Convert(PendingRegistrationModel model)
    {
        return new PendingRegistrationInputModel()
        {
            Id = model.Id,
            Email = model.PendingUserEmail,
            ExpiresAt = model.ExpiresAt,
            IsConsumed = model.IsConsumed,
            ConsumedAt = model.ConsumedAt,
            Roles = model.Roles?.Select(RoleModelToInputConverter.Convert).ToList(),
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
        };
    }

    public static PendingRegistrationModel Convert(PendingRegistrationInputModel input)
    {
        return new PendingRegistrationModel()
        {
            Id = input.Id,
            PendingUserEmail = input.Email,
            ExpiresAt = input.ExpiresAt,
            IsConsumed = input.IsConsumed,
            ConsumedAt = input.ConsumedAt,
            Roles = input.Roles?.Select(RoleModelToInputConverter.Convert).ToList(),
            CreatedAt = input.CreatedAt,
            UpdatedAt = input.UpdatedAt,
        };
    }
}