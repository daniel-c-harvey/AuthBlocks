using AuthBlocksData.Data.Repositories;
using AuthBlocksModels.Converters;
using AuthBlocksModels.Entities;
using AuthBlocksModels.Models;
using Data.Shared.Managers;
using Microsoft.Extensions.Logging;
using NetBlocks.Models;

namespace AuthBlocksData.Services;

public class PendingRegistrationService : Manager<PendingRegistration, PendingRegistrationModel, IPendingRegistrationRepository, PendingRegistrationEntityToModelConverter>, IPendingRegistrationService
{
    public PendingRegistrationService(IPendingRegistrationRepository repository, ILogger<Manager<PendingRegistration, PendingRegistrationModel, IPendingRegistrationRepository, PendingRegistrationEntityToModelConverter>> logger) : base(repository, logger)
    {
    }
    
    public async Task<ResultContainer<PendingRegistrationModel>> FindByEmail(string email)
    {
        try
        {
            var matches = await Repository.FindAsync(x => x.PendingUserEmail == email);
            if (matches.Count() != 1 || matches.FirstOrDefault() is not PendingRegistration pendingRegistration)
            {
                return ResultContainer<PendingRegistrationModel>.CreateFailResult("No pending registration found");
            }
            
            return ResultContainer<PendingRegistrationModel>.CreatePassResult
            (
                PendingRegistrationEntityToModelConverter.Convert(pendingRegistration)
            );
        }
        catch (Exception e)
        {
            return ResultContainer<PendingRegistrationModel>.CreateFailResult(e.Message);
        }
    }

    public async Task<Result> Create(string tokenHash, PendingRegistrationModel model)
    {
        
        try
        {
            var entity = PendingRegistrationEntityToModelConverter.Convert(model);
            entity.TokenHash = tokenHash;
            await Repository.AddAsync(entity);
            return Result.CreatePassResult();
        }
        catch (Exception ex)
        {
            return Result.CreateFailResult(ex.Message);
        }
    }
}