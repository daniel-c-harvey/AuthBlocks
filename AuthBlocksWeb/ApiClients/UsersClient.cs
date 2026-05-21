using System.Text.Json;
using AuthBlocksModels.Models;
using AuthBlocksWeb.Services;
using Microsoft.Extensions.Options;

namespace AuthBlocksWeb.ApiClients;

public class UsersClient : AuthorizingModelClient<UserModel, UsersClientConfig>, IUsersApiClient
{
    public UsersClient(
        UsersClientConfig config,
        IOptions<JsonSerializerOptions> options,
        ITokenService tokenService,
        IAuthApiClient authApiClient,
        ISessionExpiredAction sessionExpiredAction)
        : base(config, options, tokenService, authApiClient, sessionExpiredAction)
    {
    }
}
