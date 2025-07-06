# AuthBlocksData - Entity Framework Implementation

This project contains the refactored data layer using Entity Framework Core with PostgreSQL, replacing the previous custom ORM implementation.

## Architecture Overview

### 1. **Entities** (`AuthBlocksModels/Entities/Identity`)
- Simple POCO classes that inherit from Identity base classes
- Removed custom ORM attributes and interfaces
- Added soft delete support with `Deleted`, `Created`, and `Modified` properties

### 2. **Entity Framework Configuration** (`AuthBlocksData/Data/Configurations`)
- Separate configuration classes for each entity
- PostgreSQL-specific SQL functions (`NOW()` instead of `GETUTCDATE()`)
- Proper indexing for soft delete queries
- Maintains Identity table naming conventions

### 3. **DbContext** (`AuthBlocksData/Data/AuthDbContext.cs`)
- Inherits from `IdentityDbContext` for built-in Identity support
- Automatically applies all configurations from the assembly
- Configured for PostgreSQL

### 4. **Repositories** (`AuthBlocksData/Data/Repositories`)
- **IUserRepository/UserRepository**: Custom user operations with soft delete support
- **IRoleRepository/RoleRepository**: Custom role operations with soft delete support
- Handle business logic like soft deletes without exposing DbContext

### 5. **Services** (`AuthBlocksData/Services`)
- **UserService**: Combines `UserManager<ApplicationUser>` with `IUserRepository`
- **RoleService**: Combines `RoleManager<ApplicationRole>` with `IRoleRepository`
- Use Identity managers for standard operations
- Use repositories for custom operations (soft deletes, etc.)

## Usage

### Service Registration
```csharp
// In Program.cs or Startup.cs
services.AddAuthBlocksData(connectionString);
```

This registers:
- Entity Framework DbContext with PostgreSQL
- ASP.NET Core Identity with EF stores
- Custom repositories
- Combined services

### Using the Services

```csharp
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    
    public UserController(UserService userService)
    {
        _userService = userService;
    }
    
    // Standard Identity operations
    public async Task<IActionResult> CreateUser(CreateUserModel model)
    {
        var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
        var result = await _userService.CreateUserAsync(user, model.Password);
        return result.Succeeded ? Ok() : BadRequest(result.Errors);
    }
    
    // Custom operations with soft delete
    public async Task<IActionResult> GetActiveUser(long id)
    {
        var user = await _userService.GetActiveUserByIdAsync(id); // Excludes soft-deleted
        return user != null ? Ok(user) : NotFound();
    }
    
    public async Task<IActionResult> SoftDeleteUser(long id)
    {
        var user = await _userService.GetActiveUserByIdAsync(id);
        if (user != null)
        {
            await _userService.SoftDeleteUserAsync(user);
            return Ok();
        }
        return NotFound();
    }
}
```

## Key Benefits

1. **Simpler Architecture**: Removed custom ORM complexity
2. **Standard Identity**: Uses built-in EF Identity stores
3. **Separation of Concerns**: 
   - Standard Identity operations via `UserManager`/`RoleManager`
   - Custom business logic via repositories
4. **Soft Delete Support**: Maintained through repositories
5. **PostgreSQL Optimized**: Uses PostgreSQL-specific features
6. **Testable**: Clear interfaces and dependency injection
7. **Future-proof**: Easy to upgrade ASP.NET Core versions

## Migration from Custom ORM

The old custom stores have been removed. Instead:
- Use `UserManager<ApplicationUser>` for standard Identity operations
- Use `UserService` for operations requiring soft delete awareness
- Use `RoleService` for role operations with soft delete support 