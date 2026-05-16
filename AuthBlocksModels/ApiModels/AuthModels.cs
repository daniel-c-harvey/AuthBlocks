using System.ComponentModel.DataAnnotations;

namespace AuthBlocksModels.ApiModels;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    public string RegistrationCode { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string UserName { get; set; } = string.Empty;
}

public class AdminRegisterRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public List<long> RoleIds { get; set; } = new();
}

public class RefreshTokenRequest
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class RoleInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public long? ParentRoleId { get; set; }
    public string? ParentRoleName { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

public class UpdateUserRequest
{
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(256)]
    public string? UserName { get; set; }
}

public class UserRoleRequest
{
    [Required]
    [StringLength(256)]
    public string RoleName { get; set; } = string.Empty;
}

public class CreateRoleRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    public long? ParentRoleId { get; set; }
}

public class UpdateRoleRequest
{
    [StringLength(256, MinimumLength = 1)]
    public string? Name { get; set; }
    public long? ParentRoleId { get; set; }
} 