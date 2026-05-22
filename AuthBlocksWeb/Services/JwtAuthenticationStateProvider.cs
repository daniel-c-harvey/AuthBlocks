using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthBlocksWeb.ApiClients;
using AuthBlocksModels.ApiModels;
using Microsoft.AspNetCore.Identity;
using AuthBlocksWeb.HierarchicalAuthorize;
using NetBlocks.Models;

namespace AuthBlocksWeb.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider, ISessionExpiredAction
{
    private readonly ITokenService _tokenService;
    private readonly IAuthApiClient _authApiClient;
    private readonly JwtSecurityTokenHandler _jwtHandler;
    private readonly IHierarchicalRoleService _hierarchicalRoleService;

    public JwtAuthenticationStateProvider(
        ITokenService tokenService, 
        IAuthApiClient authApiClient,
        IHierarchicalRoleService hierarchicalRoleService)
    {
        _tokenService = tokenService;
        _authApiClient = authApiClient;
        _hierarchicalRoleService = hierarchicalRoleService;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // TokenService.GetAccessTokenAsync is already defensive: it catches all JS interop
        // exceptions internally and returns null. The null check below handles that path.
        var token = await _tokenService.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            // Try to parse the JWT token
            var jwtToken = _jwtHandler.ReadJwtToken(token);
            
            // Check if token is expired
            if (jwtToken.ValidTo <= DateTime.UtcNow)
            {
                // Try to refresh the token
                var refreshResult = await TryRefreshTokenAsync();
                if (!refreshResult.success)
                {
                    await _tokenService.ClearTokensAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
                
                // Use the new token
                jwtToken = _jwtHandler.ReadJwtToken(refreshResult.newToken);
            }

            // Create claims from JWT token
            var claims = jwtToken.Claims.ToList();
            // Use "Blazor" as the authentication type to match our authentication scheme
            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            // If token parsing fails, clear tokens and return anonymous user
            await _tokenService.ClearTokensAsync();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
    public async Task<LoginResult> LoginAsync(LoginRequest loginRequest)
    {
        try
        {
            var response = await _authApiClient.LoginAsync(loginRequest);
            if (response.Success && response.Value != null)
            {
                // AuthApiClient no longer touches the token store, so the caller
                // owns persistence. Store before notifying the cascade so any
                // observer that re-reads the token sees the freshly-issued pair.
                await _tokenService.SetTokensAsync(response.Value.AccessToken, response.Value.RefreshToken);

                // Clear the role cache when a new user logs in
                _hierarchicalRoleService.ClearCache();

                // Pre-compute the authentication state to ensure it's ready
                var authState = await GetAuthenticationStateAsync();
                NotifyAuthenticationStateChanged(Task.FromResult(authState));
                return LoginResult.CreatePassResult();
            }
            return LoginResult.From(response);
        }
        catch
        {
            return LoginResult.CreateFailResult("Failed to login", LoginFailureReason.SystemError);
        }
    }

    public async Task<Result> RegisterAsync(RegisterRequest registerRequest)
    {
        try
        {
            var response = await _authApiClient.RegisterAsync(registerRequest);
            if (response.Success && response.Value != null)
            {
                // AuthApiClient no longer touches the token store, so the caller
                // owns persistence. Store before notifying the cascade.
                await _tokenService.SetTokensAsync(response.Value.AccessToken, response.Value.RefreshToken);

                // Clear the role cache when a new user registers
                _hierarchicalRoleService.ClearCache();

                // Pre-compute the authentication state to ensure it's ready
                var authState = await GetAuthenticationStateAsync();
                NotifyAuthenticationStateChanged(Task.FromResult(authState));
            }
            return Result.From(response);
        }
        catch
        {
            return Result.CreateFailResult("Registration Failed");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();
            var refreshToken = await _tokenService.GetRefreshTokenAsync();
            
            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                await _authApiClient.LogoutAsync(new RefreshTokenRequest 
                { 
                    AccessToken = accessToken, 
                    RefreshToken = refreshToken 
                });
            }
        }
        catch
        {
            // Continue with logout even if the API call fails
        }
        finally
        {
            // Clear the role cache when user logs out
            _hierarchicalRoleService.ClearCache();
            
            await _tokenService.ClearTokensAsync();
            // Pre-compute the authentication state to ensure it's ready
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }
    }

    /// <inheritdoc/>
    public async Task HandleAsync()
    {
        _hierarchicalRoleService.ClearCache();
        await _tokenService.ClearTokensAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private async Task<(bool success, string newToken)> TryRefreshTokenAsync()
    {
        try
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();
            var refreshToken = await _tokenService.GetRefreshTokenAsync();
            
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return (false, string.Empty);
            }

            var refreshRequest = new RefreshTokenRequest 
            { 
                AccessToken = accessToken, 
                RefreshToken = refreshToken 
            };

            var response = await _authApiClient.RefreshTokenAsync(refreshRequest);
            if (response.Success && response.Value != null)
            {
                // AuthApiClient no longer persists the refreshed pair; store it
                // here so the next GetAccessTokenAsync sees the new token.
                await _tokenService.SetTokensAsync(response.Value.AccessToken, response.Value.RefreshToken);

                var authState = await GetAuthenticationStateAsync();
                NotifyAuthenticationStateChanged(Task.FromResult(authState));
                return (true, response.Value.AccessToken);
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }
} 