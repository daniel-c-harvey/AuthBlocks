using AuthBlocksAPI.Models;
using AuthBlocksData.Services;
using AuthBlocksModels.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthBlocksAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserService userService,
        UserManager<ApplicationUser> userManager,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserInfo>>>> GetUsers()
    {
        try
        {
            // For now, we'll get users by role since we don't have a GetAll method
            // In a real implementation, you'd add pagination and filtering
            var allUsers = await _userService.GetActiveUsersInRoleAsync("Admin");
            var regularUsers = await _userService.GetActiveUsersInRoleAsync("User");
            
            var combinedUsers = allUsers.Concat(regularUsers).Distinct().ToList();
            
            var userInfos = new List<UserInfo>();
            
            foreach (var user in combinedUsers)
            {
                var roles = await _userService.GetActiveUserRolesAsync(user);
                userInfos.Add(new UserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles.ToList()
                });
            }

            return Ok(new ApiResponse<List<UserInfo>>
            {
                Success = true,
                Message = "Users retrieved successfully",
                Data = userInfos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new ApiResponse<List<UserInfo>>
            {
                Success = false,
                Message = "An error occurred while retrieving users",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetUser(long id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            // Users can only view their own profile unless they're admin
            if (currentUserId != id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var user = await _userService.GetActiveUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User does not exist" }
                });
            }

            var roles = await _userService.GetActiveUserRolesAsync(user);
            var userInfo = new UserInfo
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };

            return Ok(new ApiResponse<UserInfo>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = userInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new ApiResponse<UserInfo>
            {
                Success = false,
                Message = "An error occurred while retrieving user",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserInfo>>> UpdateUser(long id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            // Users can only update their own profile unless they're admin
            if (currentUserId != id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var user = await _userService.GetActiveUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User does not exist" }
                });
            }

            // Update user properties
            user.UserName = request.UserName ?? user.UserName;
            user.Email = request.Email ?? user.Email;
            user.Modified = DateTime.UtcNow;

            var result = await _userService.UpdateUserAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "Update failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            var roles = await _userService.GetActiveUserRolesAsync(user);
            var userInfo = new UserInfo
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };

            return Ok(new ApiResponse<UserInfo>
            {
                Success = true,
                Message = "User updated successfully",
                Data = userInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new ApiResponse<UserInfo>
            {
                Success = false,
                Message = "An error occurred while updating user",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteUser(long id)
    {
        try
        {
            var user = await _userService.GetActiveUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User does not exist" }
                });
            }

            // Prevent admin from deleting themselves
            var currentUserId = GetCurrentUserId();
            if (currentUserId == id)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Cannot delete your own account",
                    Errors = new List<string> { "Self-deletion not allowed" }
                });
            }

            await _userService.SoftDeleteUserAsync(user);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "User deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "An error occurred while deleting user",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> AddUserToRole(long id, [FromBody] UserRoleRequest request)
    {
        try
        {
            var user = await _userService.GetActiveUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User does not exist" }
                });
            }

            await _userService.AddUserToRoleAsync(user, request.RoleName);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = $"User added to role '{request.RoleName}' successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to role {RoleName}", id, request.RoleName);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "An error occurred while adding user to role",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpDelete("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> RemoveUserFromRole(long id, [FromBody] UserRoleRequest request)
    {
        try
        {
            var user = await _userService.GetActiveUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User does not exist" }
                });
            }

            await _userService.RemoveUserFromRoleAsync(user, request.RoleName);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = $"User removed from role '{request.RoleName}' successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from role {RoleName}", id, request.RoleName);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "An error occurred while removing user from role",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
    }
} 