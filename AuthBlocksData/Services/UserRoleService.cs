using AuthBlocksData.Data.Repositories;
using AuthBlocksModels.Converters;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using Data.Shared.Managers;
using Microsoft.Extensions.Logging;
using NetBlocks.Models;

namespace AuthBlocksData.Services;

public class UserRoleService : Manager<ApplicationUserRole, UserRoleModel, IUserRoleRepository, UserRoleEntityToModelConverter>, IUserRoleService
{
    public UserRoleService(IUserRoleRepository repository, ILogger<Manager<ApplicationUserRole, UserRoleModel, IUserRoleRepository, UserRoleEntityToModelConverter>> logger) : base(repository, logger)
    {
    }

    public async Task<Result> AddUserToRoleAsync(UserModel user, string roleName)
    {
        try
        {
            await Repository.AddToRoleAsync(UserEntityToModelConverter.Convert(user), roleName);
            return Result.CreatePassResult();
        }
        catch (Exception ex)
        {
            return Result.CreateFailResult(ex.Message);
        }
    }

    public async Task<Result> RemoveUserFromRoleAsync(UserModel user, string roleName)
    {
        try
        {
            await Repository.RemoveFromRoleAsync(UserEntityToModelConverter.Convert(user), roleName);
            return Result.CreatePassResult();
        }
        catch (Exception e)
        {
            return Result.CreateFailResult(e.Message);
        }
        
    }

    public async Task<ResultContainer<IEnumerable<RoleModel>>> GetByUser(UserModel user)
    {
        try
        {
            var entities = await Repository.GetRolesAsync(UserEntityToModelConverter.Convert(user));
            return ResultContainer<IEnumerable<RoleModel>>.CreatePassResult(entities.Select(RoleEntityToModelConverter.Convert));
        }
        catch (Exception ex)
        {
            return ResultContainer<IEnumerable<RoleModel>>.CreateFailResult(ex.Message);
        }
    }
}