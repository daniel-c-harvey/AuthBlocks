using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AuthBlocksModels.ApiModels;
using AuthBlocksWeb.Services;
using MudBlazor;
using NetBlocks.Models;

namespace AuthBlocksWeb.ApiClients;

public class AuthApiClient : ApiClient<AuthClientConfig>, IAuthApiClient
{
    private readonly ITokenService _tokenService;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthApiClient(AuthClientConfig config, ITokenService tokenService) : base(config)
    {
        _tokenService = tokenService;
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
            var result = dtoResult.From();
            
            if (result is {Success: true, Value: AuthResponse authReponse} )
            {
                // Store tokens when login is successful
                await _tokenService.SetTokensAsync(authReponse.AccessToken, authReponse.RefreshToken);
            }
            
            return result;
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
            var result = dtoResult.From();

            if (result is {Success: true, Value: AuthResponse authResponse} )
            {
                // Store tokens when registration is successful
                await _tokenService.SetTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            return ApiResult<AuthResponse>.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult<AuthResponse>> AdminRegisterAsync(AdminRegisterRequest request)
    {
        try
        {
            // Admin must be authenticated; attach their bearer token.
            // Do NOT store the new user's tokens — the admin stays logged in as themselves.
            var token = await _tokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return ApiResult<AuthResponse>.CreateFailResult("No access token available");
            }

            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
            var result = dtoResult.From();

            if (result is {Success: true, Value: AuthResponse authResponse} )
            {
                // Update stored tokens when refresh is successful
                await _tokenService.SetTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
            }

            return result;
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
            // Add authorization header for logout
            var token = await _tokenService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                http.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await http.PostAsJsonAsync($"api/{config.ControllerName}/logout", request);
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto>(_jsonOptions);

            if (dtoResult == null) return ApiResult.CreateFailResult("Failed to parse response");
            var result = dtoResult.From();

            if (result.Success)
            {
                // Clear tokens when logout is successful
                await _tokenService.ClearTokensAsync();
            }

            // Clear authorization header
            http.DefaultRequestHeaders.Authorization = null;

            return result;
        }
        catch (Exception ex)
        {
            return ApiResult.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult<UserInfo>> GetCurrentUserAsync()
    {
        try
        {
            // Add authorization header
            var token = await _tokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return ApiResult<UserInfo>.CreateFailResult("No access token available");
            }

            http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await http.GetAsync($"api/{config.ControllerName}/me");
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto<UserInfo>>(_jsonOptions);

            // Clear authorization header
            http.DefaultRequestHeaders.Authorization = null;

            if (dtoResult == null) return ApiResult<UserInfo>.CreateFailResult("Failed to parse response");
            var result = dtoResult.From();

            return result;
        }
        catch (Exception ex)
        {
            return ApiResult<UserInfo>.CreateFailResult(ex.Message);
        }
    }

    public async Task<ApiResult<List<RoleInfo>>> GetRolesAsync()
    {
        try
        {
            // Add authorization header
            var token = await _tokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return ApiResult<List<RoleInfo>>.CreateFailResult("No access token available");
            }
    
            http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    
            var response = await http.GetAsync($"api/{config.ControllerName}/roles");
            var dtoResult = await response.Content.ReadFromJsonAsync<ApiResultDto<List<RoleInfo>>>(_jsonOptions);
    
            // Clear authorization header
            http.DefaultRequestHeaders.Authorization = null;
    
            if (dtoResult == null) return ApiResult<List<RoleInfo>>.CreateFailResult("Failed to parse response");
            var result = dtoResult.From();
    
            return result;
        }
        catch (Exception ex)
        {
            return ApiResult<List<RoleInfo>>.CreateFailResult(ex.Message);
        }
    }
} 