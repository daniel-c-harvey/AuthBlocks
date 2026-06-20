using System.Security.Claims;
using AuthBlocksData.Services;
using AuthBlocksLib.Models;
using AuthBlocksLib.Services;
using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Converters;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using AuthBlocksModels.SystemDefinitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NetBlocks.Models;

namespace AuthBlocksLib.Routes;

internal static class AuthRoutes
{
    public static void Map(IEndpointRouteBuilder root)
    {
        var group = root.MapGroup("api/auth");

        group.MapPost("login", Login);
        group.MapPost("register", Register);
        group.MapPost("admin-register", AdminRegister)
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));
        group.MapPost("refresh", RefreshToken);
        group.MapPost("logout", Logout).RequireAuthorization();
        group.MapGet("me", GetCurrentUser).RequireAuthorization();
        group.MapGet("roles", GetRoles);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        UserManager<ApplicationUser> userManager,
        IUserRoleService userRoleService,
        IJwtService jwtService,
        JwtSettings jwtSettings,
        ILogger<AuthLogger> logger)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                var emailResult = LoginResult<AuthResponse>.CreateFailResult("Invalid email", LoginFailureReason.InvalidCredentials);
                return Results.Ok(new LoginResultDto<AuthResponse>(emailResult));
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                var passwordResult = LoginResult<AuthResponse>.CreateFailResult("Invalid password", LoginFailureReason.InvalidCredentials);
                return Results.Ok(new LoginResultDto<AuthResponse>(passwordResult));
            }

            if (user.IsDeactivated)
            {
                var deactivatedResult = LoginResult<AuthResponse>.CreateFailResult("User account is deactivated", LoginFailureReason.UserNotActive);
                return Results.Ok(new LoginResultDto<AuthResponse>(deactivatedResult));
            }

            var userModel = UserEntityToModelConverter.Convert(user);

            var accessToken = await jwtService.GenerateTokenAsync(userModel);
            var refreshToken = await jwtService.GenerateRefreshTokenAsync();
            await jwtService.SaveRefreshTokenAsync(refreshToken, user.Id);

            var rolesResult = await userRoleService.GetByUser(userModel);
            if (rolesResult is null or { Success: false } or { Value: null })
            {
                var resultFailure = LoginResult<AuthResponse>.CreateFailResult("Role check failed", LoginFailureReason.SystemError)
                    .Fail("User roles could not be loaded");
                return Results.Ok(new LoginResultDto<AuthResponse>(resultFailure));
            }

            var roles = rolesResult.Value!;
            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
                User = new UserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.Select(r => r.Name!).ToList()
                }
            };

            var resultSuccess = LoginResult<AuthResponse>.CreatePassResult(response).Inform("Login successful");
            return Results.Ok(new LoginResultDto<AuthResponse>(resultSuccess));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for user {Email}", request.Email);
            var result = LoginResult<AuthResponse>.CreateFailResult("An error occurred during login", LoginFailureReason.SystemError)
                .Fail("Internal server error");
            return Results.Json(new LoginResultDto<AuthResponse>(result), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        IUserService userService,
        IUserRoleService userRoleService,
        IJwtService jwtService,
        IRegistrationTokenService registrationTokenService,
        JwtSettings jwtSettings,
        ILogger<AuthLogger> logger)
    {
        try
        {
            var existingUser = await userService.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                var resultFailure = ApiResult<AuthResponse>.CreateFailResult("Registration failed")
                    .Fail("User with this email already exists");
                return Results.BadRequest(new ApiResultDto<AuthResponse>(resultFailure));
            }

            var tokenValidationResult = await registrationTokenService.ValidateTokenAsync(request.Email, request.RegistrationCode);
            if (tokenValidationResult is null or { Success: false })
            {
                var validationResult = ApiResult<AuthResponse>.CreateFailResult("Invalid registration code");
                return Results.BadRequest(new ApiResultDto<AuthResponse>(validationResult));
            }

            var user = new UserModel
            {
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await userService.Add(user, request.Password);
            if (!createResult.Success || createResult.Value is null)
            {
                var resultFailure = ApiResult<AuthResponse>.From(createResult);
                return Results.Json(new ApiResultDto<AuthResponse>(resultFailure), statusCode: StatusCodes.Status500InternalServerError);
            }
            var createdUser = createResult.Value;

            foreach (var role in tokenValidationResult.Roles ?? Enumerable.Empty<RoleModel>())
            {
                var addToRoleResult = await userRoleService.AddUserToRoleAsync(createdUser, role.Name!);
                if (addToRoleResult.Success) continue;

                var resultFailure = ApiResult<AuthResponse>.From(addToRoleResult);
                return Results.Json(new ApiResultDto<AuthResponse>(resultFailure), statusCode: StatusCodes.Status500InternalServerError);
            }

            var consumeTokenResult = await registrationTokenService.ConsumeTokenAsync(createdUser.Email!, request.RegistrationCode);
            if (!consumeTokenResult.Success)
            {
                var resultFailure = ApiResult<AuthResponse>.From(consumeTokenResult);
                return Results.Json(new ApiResultDto<AuthResponse>(resultFailure), statusCode: StatusCodes.Status500InternalServerError);
            }

            var accessToken = await jwtService.GenerateTokenAsync(createdUser);
            var refreshToken = await jwtService.GenerateRefreshTokenAsync();
            await jwtService.SaveRefreshTokenAsync(refreshToken, createdUser.Id);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
                User = new UserInfo
                {
                    Id = createdUser.Id,
                    UserName = createdUser.UserName ?? string.Empty,
                    Email = createdUser.Email ?? string.Empty,
                    Roles = new List<string>()
                }
            };

            var resultSuccess = ApiResult<AuthResponse>.CreatePassResult(response).Inform("Registration successful");
            return Results.Ok(new ApiResultDto<AuthResponse>(resultSuccess));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for user {Email}", request.Email);
            var resultError = ApiResult<AuthResponse>.CreateFailResult("An error occurred during registration");
            return Results.Json(new ApiResultDto<AuthResponse>(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> AdminRegister(
        [FromBody] AdminRegisterRequest request,
        IUserService userService,
        IRoleService roleService,
        IUserRoleService userRoleService,
        IJwtService jwtService,
        JwtSettings jwtSettings,
        ILogger<AuthLogger> logger)
    {
        try
        {
            var existingUser = await userService.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                var resultFailure = ApiResult<AuthResponse>.CreateFailResult("Registration failed")
                    .Fail("User with this email already exists");
                return Results.BadRequest(new ApiResultDto<AuthResponse>(resultFailure));
            }

            // Resolve role IDs up front so we fail fast on bad input rather than half-creating a user.
            var roleNames = new List<string>();
            foreach (var roleId in request.RoleIds ?? new List<long>())
            {
                var roleResult = await roleService.GetById(roleId);
                if (roleResult is not { Success: true, Value: { } role } || string.IsNullOrEmpty(role.Name))
                {
                    var resultFailure = ApiResult<AuthResponse>.CreateFailResult("Registration failed")
                        .Fail($"Role with id {roleId} not found");
                    return Results.BadRequest(new ApiResultDto<AuthResponse>(resultFailure));
                }
                roleNames.Add(role.Name);
            }

            var user = new UserModel
            {
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await userService.Add(user, request.Password);
            if (!createResult.Success || createResult.Value is null)
            {
                var resultFailure = ApiResult<AuthResponse>.From(createResult);
                return Results.Json(new ApiResultDto<AuthResponse>(resultFailure), statusCode: StatusCodes.Status500InternalServerError);
            }
            var createdUser = createResult.Value;

            foreach (var roleName in roleNames)
            {
                var addToRoleResult = await userRoleService.AddUserToRoleAsync(createdUser, roleName);
                if (addToRoleResult.Success) continue;

                await userService.Delete(createdUser.Id);
                var resultFailure = ApiResult<AuthResponse>.From(addToRoleResult);
                return Results.Json(new ApiResultDto<AuthResponse>(resultFailure), statusCode: StatusCodes.Status500InternalServerError);
            }

            var accessToken = await jwtService.GenerateTokenAsync(createdUser);
            var refreshToken = await jwtService.GenerateRefreshTokenAsync();
            await jwtService.SaveRefreshTokenAsync(refreshToken, createdUser.Id);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
                User = new UserInfo
                {
                    Id = createdUser.Id,
                    UserName = createdUser.UserName ?? string.Empty,
                    Email = createdUser.Email ?? string.Empty,
                    Roles = roleNames
                }
            };

            var resultSuccess = ApiResult<AuthResponse>.CreatePassResult(response).Inform("Registration successful");
            return Results.Ok(new ApiResultDto<AuthResponse>(resultSuccess));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during admin registration for user {Email}", request.Email);
            var resultError = ApiResult<AuthResponse>.CreateFailResult("An error occurred during registration");
            return Results.Json(new ApiResultDto<AuthResponse>(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        IUserService userService,
        IUserRoleService userRoleService,
        IJwtService jwtService,
        JwtSettings jwtSettings,
        ILogger<AuthLogger> logger)
    {
        try
        {
            var principal = jwtService.ValidateExpiredToken(request.AccessToken);
            if (principal == null)
            {
                var resultFailure = ApiResult<AuthResponse>.CreateFailResult("Invalid access token")
                    .Fail("Token validation failed");
                return Results.BadRequest(new ApiResultDto<AuthResponse>(resultFailure));
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            {
                var resultFailure = ApiResult<AuthResponse>.CreateFailResult("Invalid token claims")
                    .Fail("User ID not found in token");
                return Results.BadRequest(new ApiResultDto<AuthResponse>(resultFailure));
            }

            var isValidRefreshToken = await jwtService.ValidateRefreshTokenAsync(request.RefreshToken, userId);
            if (!isValidRefreshToken)
            {
                var resultFailure = ApiResult<AuthResponse>.CreateFailResult("Invalid refresh token")
                    .Fail("Refresh token validation failed");
                return Results.BadRequest(new ApiResultDto<AuthResponse>(resultFailure));
            }

            var userResult = await userService.GetById(userId);
            switch (userResult)
            {
                case { Success: false }:
                    return Results.Json(new ApiResultDto<AuthResponse>(ApiResult<AuthResponse>.From(userResult)),
                        statusCode: StatusCodes.Status500InternalServerError);
                case { Value: null }:
                    var resultFailure = ApiResult<AuthResponse>.CreateFailResult("User not found");
                    return Results.Json(new ApiResultDto<AuthResponse>(resultFailure),
                        statusCode: StatusCodes.Status500InternalServerError);
            }

            var user = userResult.Value!;

            await jwtService.RevokeRefreshTokenAsync(request.RefreshToken);

            var newAccessToken = await jwtService.GenerateTokenAsync(user);
            var newRefreshToken = await jwtService.GenerateRefreshTokenAsync();
            await jwtService.SaveRefreshTokenAsync(newRefreshToken, user.Id);

            var rolesResult = await userRoleService.GetByUser(user);
            if (rolesResult is null or { Success: false } or { Value: null })
            {
                var roleFailure = ApiResult<AuthResponse>.CreateFailResult("Refresh failed")
                    .Fail("User roles could not be determined");
                return Results.BadRequest(new ApiResultDto<AuthResponse>(roleFailure));
            }

            var roles = rolesResult.Value!;
            var response = new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(jwtSettings.ExpiryMinutes),
                User = new UserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.Select(r => r.Name!).ToList()
                }
            };

            var resultSuccess = ApiResult<AuthResponse>.CreatePassResult(response).Inform("Token refreshed successfully");
            return Results.Ok(new ApiResultDto<AuthResponse>(resultSuccess));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token refresh");
            var resultError = ApiResult<AuthResponse>.CreateFailResult("An error occurred during token refresh")
                .Fail("Internal server error");
            return Results.Json(new ApiResultDto<AuthResponse>(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> Logout(
        [FromBody] RefreshTokenRequest request,
        IJwtService jwtService,
        ILogger<AuthLogger> logger)
    {
        try
        {
            await jwtService.RevokeRefreshTokenAsync(request.RefreshToken);

            var resultSuccess = ApiResult.CreatePassResult().Inform("Logout successful");
            return Results.Ok(new ApiResultDto(resultSuccess));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
            var resultError = ApiResult.CreateFailResult("An error occurred during logout")
                .Fail("Internal server error");
            return Results.Json(new ApiResultDto(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetCurrentUser(
        ClaimsPrincipal user,
        IUserService userService,
        IUserRoleService userRoleService,
        ILogger<AuthLogger> logger)
    {
        try
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            {
                var resultFailure = ApiResult<UserInfo>.CreateFailResult("Invalid user claims")
                    .Fail("User ID not found in token");
                return Results.BadRequest(new ApiResultDto<UserInfo>(resultFailure));
            }

            var userResult = await userService.GetById(userId);
            if (userResult is { Success: false } or { Value: null })
            {
                var resultFailure = ApiResult<UserInfo>.CreateFailResult("User not found")
                    .Fail("User does not exist");
                return Results.NotFound(new ApiResultDto<UserInfo>(resultFailure));
            }

            var resolvedUser = userResult.Value!;
            var rolesResult = await userRoleService.GetByUser(resolvedUser);
            if (rolesResult is { Success: false } or { Value: null })
            {
                var resultFailure = ApiResult<AuthResponse>.CreateFailResult("Refresh failed")
                    .Fail("User roles could not be determined");
                return Results.BadRequest(new ApiResultDto<AuthResponse>(resultFailure));
            }

            var roles = rolesResult.Value!;

            var userInfo = new UserInfo
            {
                Id = resolvedUser.Id,
                UserName = resolvedUser.UserName ?? string.Empty,
                Email = resolvedUser.Email ?? string.Empty,
                Roles = roles.Select(r => r.Name!).ToList()
            };

            var resultSuccess = ApiResult<UserInfo>.CreatePassResult(userInfo).Inform("User info retrieved successfully");
            return Results.Ok(new ApiResultDto<UserInfo>(resultSuccess));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving current user info");
            var resultError = ApiResult<UserInfo>.CreateFailResult("An error occurred while retrieving user info")
                .Fail("Internal server error");
            return Results.Json(new ApiResultDto<UserInfo>(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetRoles(IRoleService roleService, ILogger<AuthLogger> logger)
    {
        try
        {
            var rolesResult = await roleService.Get();

            if (rolesResult is { Success: false } or { Value: null })
            {
                return Results.Json(new ApiResultDto<List<RoleInfo>>(ApiResult<List<RoleInfo>>.From(rolesResult)),
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            var roles = rolesResult.Value!;

            var roleInfos = roles.Select(r => new RoleInfo
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                NormalizedName = r.NormalizedName ?? string.Empty,
                ParentRoleName = r.ParentRole?.Name,
                ParentRoleId = r.ParentRole?.Id,
                Created = r.CreatedAt,
                Modified = r.UpdatedAt
            }).ToList();

            var resultSuccess = ApiResult<List<RoleInfo>>.CreatePassResult(roleInfos).Inform("Roles retrieved successfully");
            return Results.Ok(new ApiResultDto<List<RoleInfo>>(resultSuccess));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving roles");
            var resultError = ApiResult<List<RoleInfo>>.CreateFailResult("An error occurred while retrieving roles")
                .Fail("Internal server error");
            return Results.Json(new ApiResultDto<List<RoleInfo>>(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Type tag used purely so ILogger&lt;T&gt; gives a clean category name for AuthBlocks auth routes.</summary>
    internal sealed class AuthLogger { }
}
