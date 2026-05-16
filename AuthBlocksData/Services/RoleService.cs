using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksData.Data.Repositories;
using AuthBlocksModels.Converters;
using AuthBlocksModels.Models;
using Data.Shared.Managers;
using NetBlocks.Models;

namespace AuthBlocksData.Services;

public class RoleService : Manager<ApplicationRole, RoleModel, IRoleRepository, RoleEntityToModelConverter>, IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleService(RoleManager<ApplicationRole> roleManager, IRoleRepository roleRepository, ILogger<Manager<ApplicationRole, RoleModel, IRoleRepository, RoleEntityToModelConverter>> logger) : base(roleRepository, logger)
    {
        _roleManager = roleManager;
    }

    public override async Task<ResultContainer<RoleModel>> Add(RoleModel model)
    {
        try
        {
            var identityResult = await _roleManager.CreateAsync(RoleEntityToModelConverter.Convert(model));
            if (identityResult.Succeeded)
            {
                return (await Repository.GetByNameAsync(model.NormalizedName)) is ApplicationRole newRole 
                    ? ResultContainer<RoleModel>.CreatePassResult(RoleEntityToModelConverter.Convert(newRole)) 
                    : ResultContainer<RoleModel>.CreateFailResult("Role not found");
            }
            
            return ResultContainer<RoleModel>.CreateFailResult(identityResult.Errors.Select(error => error.Description).ToArray());
        }
        catch (Exception ex)
        {
            return ResultContainer<RoleModel>.CreateFailResult(ex.Message);
        }
    }

    public async Task<ResultContainer<RoleModel>> FindByNameAsync(string roleName)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) return ResultContainer<RoleModel>.CreatePassResult().Inform("Role not found");
            return ResultContainer<RoleModel>.CreatePassResult(RoleEntityToModelConverter.Convert(role));
        }
        catch (Exception e)
        {
            return ResultContainer<RoleModel>.CreateFailResult(e.Message);
        }
    }
} 