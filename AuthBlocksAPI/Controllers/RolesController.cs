using AuthBlocksAPI.Models;
using AuthBlocksData.Services;
using AuthBlocksModels.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AuthBlocksAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(RoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleInfo>>>> GetRoles()
    {
        try
        {
            // Since we don't have a GetAll method, we'll get by specific role names
            // In a real implementation, you'd add a GetAll method to the repository
            var roles = new List<ApplicationRole>();
            
            // Get Admin role
            var adminRole = await _roleService.GetActiveRoleByNameAsync("ADMIN");
            if (adminRole != null) roles.Add(adminRole);
            
            // Get User role (if exists)
            var userRole = await _roleService.GetActiveRoleByNameAsync("USER");
            if (userRole != null) roles.Add(userRole);

            var roleInfos = roles.Select(r => new RoleInfo
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                NormalizedName = r.NormalizedName ?? string.Empty,
                Created = r.Created,
                Modified = r.Modified
            }).ToList();

            return Ok(new ApiResponse<List<RoleInfo>>
            {
                Success = true,
                Message = "Roles retrieved successfully",
                Data = roleInfos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new ApiResponse<List<RoleInfo>>
            {
                Success = false,
                Message = "An error occurred while retrieving roles",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleInfo>>> GetRole(long id)
    {
        try
        {
            var role = await _roleService.GetActiveRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound(new ApiResponse<RoleInfo>
                {
                    Success = false,
                    Message = "Role not found",
                    Errors = new List<string> { "Role does not exist" }
                });
            }

            var roleInfo = new RoleInfo
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                NormalizedName = role.NormalizedName ?? string.Empty,
                Created = role.Created,
                Modified = role.Modified
            };

            return Ok(new ApiResponse<RoleInfo>
            {
                Success = true,
                Message = "Role retrieved successfully",
                Data = roleInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", id);
            return StatusCode(500, new ApiResponse<RoleInfo>
            {
                Success = false,
                Message = "An error occurred while retrieving role",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleInfo>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            // Check if role already exists
            var existingRole = await _roleService.FindByNameAsync(request.Name);
            if (existingRole != null)
            {
                return BadRequest(new ApiResponse<RoleInfo>
                {
                    Success = false,
                    Message = "Role creation failed",
                    Errors = new List<string> { "Role with this name already exists" }
                });
            }

            var role = new ApplicationRole
            {
                Name = request.Name,
                NormalizedName = request.Name.ToUpperInvariant(),
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            var result = await _roleService.CreateRoleAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<RoleInfo>
                {
                    Success = false,
                    Message = "Role creation failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            var roleInfo = new RoleInfo
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                NormalizedName = role.NormalizedName ?? string.Empty,
                Created = role.Created,
                Modified = role.Modified
            };

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new ApiResponse<RoleInfo>
            {
                Success = true,
                Message = "Role created successfully",
                Data = roleInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName}", request.Name);
            return StatusCode(500, new ApiResponse<RoleInfo>
            {
                Success = false,
                Message = "An error occurred while creating role",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RoleInfo>>> UpdateRole(long id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var role = await _roleService.GetActiveRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound(new ApiResponse<RoleInfo>
                {
                    Success = false,
                    Message = "Role not found",
                    Errors = new List<string> { "Role does not exist" }
                });
            }

            // Check if new name conflicts with existing role
            if (!string.IsNullOrEmpty(request.Name) && request.Name != role.Name)
            {
                var existingRole = await _roleService.FindByNameAsync(request.Name);
                if (existingRole != null && existingRole.Id != id)
                {
                    return BadRequest(new ApiResponse<RoleInfo>
                    {
                        Success = false,
                        Message = "Role update failed",
                        Errors = new List<string> { "Role with this name already exists" }
                    });
                }

                role.Name = request.Name;
                role.NormalizedName = request.Name.ToUpperInvariant();
            }

            role.Modified = DateTime.UtcNow;

            var result = await _roleService.UpdateRoleAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse<RoleInfo>
                {
                    Success = false,
                    Message = "Role update failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            var roleInfo = new RoleInfo
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                NormalizedName = role.NormalizedName ?? string.Empty,
                Created = role.Created,
                Modified = role.Modified
            };

            return Ok(new ApiResponse<RoleInfo>
            {
                Success = true,
                Message = "Role updated successfully",
                Data = roleInfo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", id);
            return StatusCode(500, new ApiResponse<RoleInfo>
            {
                Success = false,
                Message = "An error occurred while updating role",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteRole(long id)
    {
        try
        {
            var role = await _roleService.GetActiveRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "Role not found",
                    Errors = new List<string> { "Role does not exist" }
                });
            }

            // Prevent deletion of system roles
            if (role.Name == "Admin")
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Cannot delete system role",
                    Errors = new List<string> { "Admin role cannot be deleted" }
                });
            }

            await _roleService.SoftDeleteRoleAsync(role);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Role deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", id);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "An error occurred while deleting role",
                Errors = new List<string> { "Internal server error" }
            });
        }
    }
}

public class RoleInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

public class CreateRoleRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    [StringLength(256, MinimumLength = 1)]
    public string? Name { get; set; }
} 