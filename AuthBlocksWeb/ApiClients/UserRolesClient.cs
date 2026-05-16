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
                           ITokenService tokenService)
        : base(config, options, tokenService)
    {
    }

    public async Task<ApiResult<List<RoleInfo>>> GetRolesForUser(long userId)
    {
        try
        {
            if (await AddAuthorizationHeader() is { Success: false } error)
            {
                return ApiResult<List<RoleInfo>>.From(error);
            }

            var dtoResult = await http.GetFromJsonAsync<ApiResultDto<List<RoleInfo>>>(
                                $"api/{config.ControllerName}/user/{userId}", Options)
                            ?? throw new HttpRequestException("Failed to deserialize response");

            return dtoResult.From();
        }
        catch (Exception e)
        {
            return ApiResult<List<RoleInfo>>.CreateFailResult(e.Message);
        }
        finally
        {
            ClearAuthorizationHeader();
        }
    }

    public async Task<ApiResult> AddUserToRole(long userId, string roleName)
    {
        try
        {
            if (await AddAuthorizationHeader() is { Success: false } error)
            {
                return ApiResult.From(error);
            }

            var request = new UserRoleRequest { RoleName = roleName };
            var response = await http.PostAsJsonAsync(
                $"api/{config.ControllerName}/user/{userId}", request, Options);

            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto>(Options)
                            ?? throw new HttpRequestException("Failed to deserialize response");

            return dtoResult.From();
        }
        catch (Exception e)
        {
            return ApiResult.CreateFailResult(e.Message);
        }
        finally
        {
            ClearAuthorizationHeader();
        }
    }

    public async Task<ApiResult> RemoveUserFromRole(long userId, string roleName)
    {
        try
        {
            if (await AddAuthorizationHeader() is { Success: false } error)
            {
                return ApiResult.From(error);
            }

            // HttpClient.DeleteAsync does not natively support a body, so build the request explicitly.
            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"api/{config.ControllerName}/user/{userId}")
            {
                Content = JsonContent.Create(new UserRoleRequest { RoleName = roleName }, options: Options)
            };

            var response = await http.SendAsync(request);
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto>(Options)
                            ?? throw new HttpRequestException("Failed to deserialize response");

            return dtoResult.From();
        }
        catch (Exception e)
        {
            return ApiResult.CreateFailResult(e.Message);
        }
        finally
        {
            ClearAuthorizationHeader();
        }
    }
}
