using System.Net.Http.Json;
using System.Text.Json;
using AuthBlocksModels.ApiModels;
using NetBlocks.Models;

namespace AuthBlocksWeb.ApiClients;

public class AuthApiClient : ApiClient<AuthClientConfig>, IAuthApiClient
{
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthApiClient(AuthClientConfig config) : base(config)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<LoginResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync($"api/{config.ControllerName}/login", request);
            var dtoResult = await response.Content.ReadFromJsonAsync<LoginResultDto<AuthResponse>>(_jsonOptions);

            if (dtoResult == null) return LoginResult<AuthResponse>.CreateFailResult("Failed to parse response", LoginFailureReason.SystemError);
            return dtoResult.From();
        }
        catch (Exception ex)
        {
            return LoginResult<AuthResponse>.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync($"api/{config.ControllerName}/register", request);
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto<AuthResponse>>(_jsonOptions);

            if (dtoResult == null) return ApiResult<AuthResponse>.CreateFailResult("Failed to parse response");
            return dtoResult.From();
        }
        catch (Exception ex)
        {
            return ApiResult<AuthResponse>.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult<AuthResponse>> AdminRegisterAsync(AdminRegisterRequest request, string accessToken)
    {
        try
        {
            // Admin must be authenticated; attach their bearer token.
            // Do NOT store the new user's tokens — the admin stays logged in as themselves.
            if (string.IsNullOrEmpty(accessToken))
            {
                return ApiResult<AuthResponse>.CreateFailResult("No access token available");
            }

            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await http.PostAsJsonAsync($"api/{config.ControllerName}/admin-register", request);
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto<AuthResponse>>(_jsonOptions);

            http.DefaultRequestHeaders.Authorization = null;

            if (dtoResult == null) return ApiResult<AuthResponse>.CreateFailResult("Failed to parse response");
            return dtoResult.From();
        }
        catch (Exception ex)
        {
            http.DefaultRequestHeaders.Authorization = null;
            return ApiResult<AuthResponse>.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync($"api/{config.ControllerName}/refresh", request);
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto<AuthResponse>>(_jsonOptions);

            if (dtoResult == null) return ApiResult<AuthResponse>.CreateFailResult("Failed to parse response");
            return dtoResult.From();
        }
        catch (Exception ex)
        {
            return ApiResult<AuthResponse>.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult> LogoutAsync(RefreshTokenRequest request)
    {
        try
        {
            // The access token rides on the request body, but the server also requires the
            // Authorization header for the [Authorize] filter on the logout endpoint.
            if (!string.IsNullOrEmpty(request.AccessToken))
            {
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.AccessToken);
            }

            var response = await http.PostAsJsonAsync($"api/{config.ControllerName}/logout", request);
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto>(_jsonOptions);

            http.DefaultRequestHeaders.Authorization = null;

            if (dtoResult == null) return ApiResult.CreateFailResult("Failed to parse response");
            return dtoResult.From();
        }
        catch (Exception ex)
        {
            http.DefaultRequestHeaders.Authorization = null;
            return ApiResult.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult<UserInfo>> GetCurrentUserAsync(string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return ApiResult<UserInfo>.CreateFailResult("No access token available");
            }

            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await http.GetAsync($"api/{config.ControllerName}/me");
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto<UserInfo>>(_jsonOptions);

            http.DefaultRequestHeaders.Authorization = null;

            if (dtoResult == null) return ApiResult<UserInfo>.CreateFailResult("Failed to parse response");
            return dtoResult.From();
        }
        catch (Exception ex)
        {
            http.DefaultRequestHeaders.Authorization = null;
            return ApiResult<UserInfo>.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult<List<RoleInfo>>> GetRolesAsync(string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return ApiResult<List<RoleInfo>>.CreateFailResult("No access token available");
            }

            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await http.GetAsync($"api/{config.ControllerName}/roles");
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto<List<RoleInfo>>>(_jsonOptions);

            http.DefaultRequestHeaders.Authorization = null;

            if (dtoResult == null) return ApiResult<List<RoleInfo>>.CreateFailResult("Failed to parse response");
            return dtoResult.From();
        }
        catch (Exception ex)
        {
            http.DefaultRequestHeaders.Authorization = null;
            return ApiResult<List<RoleInfo>>.CreateFailResult(ex.Message);
        }
    }
}
