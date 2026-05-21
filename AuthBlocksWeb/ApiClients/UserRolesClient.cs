using System.Net.Http.Json;
using System.Text.Json;
using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Models;
using AuthBlocksWeb.Services;
using Microsoft.Extensions.Options;
using NetBlocks.Models;

namespace AuthBlocksWeb.ApiClients;

public class UserRolesClient : AuthorizingModelClient<UserRoleModel, UserRolesClientConfig>, IUserRolesClient
{
    public UserRolesClient(UserRolesClientConfig config,
                           IOptions<JsonSerializerOptions> options,
                           ITokenService tokenService,
                           IAuthApiClient authApiClient,
                           ISessionExpiredAction sessionExpiredAction)
        : base(config, options, tokenService, authApiClient, sessionExpiredAction)
    {
    }

    public Task<ApiResult<List<RoleInfo>>> GetRolesForUser(long userId)
        => SendWithAuth(
            () => http.GetAsync($"api/{config.ControllerName}/user/{userId}"),
            DeserializeApiResult<List<RoleInfo>>);

    public Task<ApiResult> AddUserToRole(long userId, string roleName)
        => SendWithAuth(
            () => http.PostAsJsonAsync($"api/{config.ControllerName}/user/{userId}", new UserRoleRequest { RoleName = roleName }, Options),
            DeserializeApiResult);

    public Task<ApiResult> RemoveUserFromRole(long userId, string roleName)
        => SendWithAuth(
            () =>
            {
                // HttpClient.DeleteAsync does not natively support a body, so build the request explicitly.
                // A fresh HttpRequestMessage is created on each invocation because the content stream
                // is consumed on first send — the delegate is called twice on the 401-retry path.
                var req = new HttpRequestMessage(HttpMethod.Delete, $"api/{config.ControllerName}/user/{userId}")
                {
                    Content = JsonContent.Create(new UserRoleRequest { RoleName = roleName }, options: Options)
                };
                return http.SendAsync(req);
            },
            DeserializeApiResult);
}
