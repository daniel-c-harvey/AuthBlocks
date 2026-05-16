using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Models;
using Web.Shared.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public interface IPendingRegistrationClient : IModelClient<PendingRegistrationModel>
{
    Task<RegistrationCreatedResult> CreatePendingRegistration(string email, IEnumerable<RoleModel>? roles, string returnUrl);
}