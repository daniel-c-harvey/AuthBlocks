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
        IAuthSession authSession)
        : base(config, options, authSession)
    {
    }
}
