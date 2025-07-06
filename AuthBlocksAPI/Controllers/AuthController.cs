using AuthBlocksAPI.Models;
using AuthBlocksAPI.Services;
using AuthBlocksData.Services;
using AuthBlocksModels.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthBlocksAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserService userService,
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _userManager = userManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userService.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Errors = new List<string> { "User not found" }
                });
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Errors = new List<string> { "Invalid password" }
                });
            }

            var accessToken = await _jwtService.GenerateTokenAsync(user);
            var refreshToken = await _jwtService.GenerateRefreshTokenAsync();
            await _jwtService.SaveRefreshTokenAsync(refreshToken, user.Id);

            var userRoles = await _userService.GetActiveUserRolesAsync(user);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Match JWT expiry
                User = new UserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = userRoles.ToList()
                }
            };

            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Login successful",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", request.Email);
            return StatusCode(500, new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during login",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var existingUser = await _userService.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = new List<string> { "User with this email already exists" }
                });
            }

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = true, // For API, we'll skip email confirmation
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            var result = await _userService.CreateUserAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            var accessToken = await _jwtService.GenerateTokenAsync(user);
            var refreshToken = await _jwtService.GenerateRefreshTokenAsync();
            await _jwtService.SaveRefreshTokenAsync(refreshToken, user.Id);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = new List<string>() // New user has no roles initially
                }
            };

            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Registration successful",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Email}", request.Email);
            return StatusCode(500, new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during registration",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var principal = _jwtService.ValidateToken(request.AccessToken);
            if (principal == null)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Invalid access token",
                    Errors = new List<string> { "Token validation failed" }
                });
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Invalid token claims",
                    Errors = new List<string> { "User ID not found in token" }
                });
            }

            var isValidRefreshToken = await _jwtService.ValidateRefreshTokenAsync(request.RefreshToken, userId);
            if (!isValidRefreshToken)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Invalid refresh token",
                    Errors = new List<string> { "Refresh token validation failed" }
                });
            }

            var user = await _userService.GetActiveUserByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User does not exist" }
                });
            }

            // Revoke old refresh token and generate new tokens
            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);
            
            var newAccessToken = await _jwtService.GenerateTokenAsync(user);
            var newRefreshToken = await _jwtService.GenerateRefreshTokenAsync();
            await _jwtService.SaveRefreshTokenAsync(newRefreshToken, user.Id);

            var userRoles = await _userService.GetActiveUserRolesAsync(user);

            var response = new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = userRoles.ToList()
                }
            };

            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new ApiResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during token refresh",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);
            
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Logout successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "An error occurred during logout",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            {
                return BadRequest(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "Invalid user claims",
                    Errors = new List<string> { "User ID not found in token" }
                });
            }

            var user = await _userService.GetActiveUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User does not exist" }
                });
            }

            var userRoles = await _userService.GetActiveUserRolesAsync(user);

            var userInfo = new UserInfo
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = userRoles.ToList()
            };

            return Ok(new ApiResponse<UserInfo>
            {
                Success = true,
                Message = "User info retrieved successfully",
                Data = userInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user info");
            return StatusCode(500, new ApiResponse<UserInfo>
            {
                Success = false,
                Message = "An error occurred while retrieving user info",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }
} 