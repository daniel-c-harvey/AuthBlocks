using AuthBlocksModels.Entities;
using AuthBlocksModels.Models;
using Data.Shared.Managers;
using NetBlocks.Models;

namespace AuthBlocksData.Services;

public interface IPendingRegistrationService : IManager<PendingRegistration, PendingRegistrationModel>
{
    Task<ResultContainer<PendingRegistrationModel>> FindByEmail(string email);
    Task<Result> Create(string tokenHash, PendingRegistrationModel model);
}