> Historical transition log — AuthBlocksAPI was subsequently merged into SkipperAPI as AuthBlocksLib.

# AuthBlocksWeb API Transition Summary

## Overview
AuthBlocksWeb has been successfully transitioned from Identity cookie-based authentication to JWT API-based authentication using the AuthBlocksAPI.

## What Was Implemented

### 1. API Client Infrastructure
- **`ApiClients/IAuthApiClient.cs`**: Interface for authentication API operations
- **`ApiClients/AuthApiClient.cs`**: HTTP client implementation for AuthBlocksAPI communication
- **`Models/Api/ApiModels.cs`**: Request/response models matching AuthBlocksAPI structure

### 2. JWT Token Management
- **`Services/ITokenService.cs`**: Interface for JWT token operations
- **`Services/TokenService.cs`**: Browser localStorage-based token storage and validation
- **`Services/JwtAuthenticationStateProvider.cs`**: JWT-based authentication state provider replacing Identity version

### 3. Simplified Authentication Pages
- **`Components/Account/Pages/SimpleLogin.razor`**: Clean login page using API
- **`Components/Account/Pages/SimpleRegister.razor`**: Clean registration page using API
- **`Components/Account/Shared/LogoutButton.razor`**: Reusable logout component
- **`Components/Account/Shared/StatusMessage.razor`**: Simple message display component

### 4. Updated Configuration
- **`Startup.cs`**: Completely rewritten to use JWT authentication services
- **`AuthBlocksWeb.csproj`**: Added System.IdentityModel.Tokens.Jwt package
- **`Components/Layout/AccountNavMenu.razor`**: Updated to use new authentication system

## What Was Removed

### Removed Pages (Identity-based)
- `Login.razor` (replaced by SimpleLogin.razor)
- `Register.razor` (replaced by SimpleRegister.razor)
- `ExternalLogin.razor` (external login not in API)
- All email confirmation pages (API handles email)
- All password reset pages (API handles password reset)
- All 2FA pages (not implemented in current API)
- All management pages (Identity-based user management)

### Removed Services
- `IdentityUserAccessor.cs`
- `IdentityRedirectManager.cs`
- `IdentityRevalidatingAuthenticationStateProvider.cs`
- `IdentityComponentsEndpointRouteBuilderExtensions.cs`
- All Identity email sender implementations

### Removed Components
- `ManageNavMenu.razor`
- `ExternalLoginPicker.razor`
- `ManageLayout.razor`
- `ShowRecoveryCodes.razor`

## How to Use

### 1. Service Configuration
In your consuming application's `Program.cs`:

```csharp
// Add AuthBlocksWeb services with your API base URL
AuthBlocksWeb.Startup.ConfigureServices(services, "https://your-api-base-url");
```

### 2. Authentication Flow
1. Users visit `/account/login` for authentication
2. Successful login stores JWT tokens in browser localStorage
3. `JwtAuthenticationStateProvider` automatically handles token validation and refresh
4. `AuthorizeView` components work normally with JWT claims
5. Logout clears tokens and notifies API

### 3. Token Management
- Access tokens are automatically included in API requests
- Refresh tokens are used when access tokens expire
- Tokens are stored securely in browser localStorage
- Token validation includes expiration checking

## API Endpoints Used
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration  
- `POST /api/auth/refresh` - Token refresh
- `POST /api/auth/logout` - User logout
- `GET /api/auth/me` - Current user info

## Key Features
- **Automatic Token Refresh**: Expired tokens are automatically refreshed
- **Secure Storage**: Tokens stored in browser localStorage with proper cleanup
- **Error Handling**: Comprehensive error handling for network and API failures
- **Loading States**: UI shows loading indicators during authentication operations
- **Clean Architecture**: Separation of concerns with interfaces and services

## Migration Benefits
1. **Reduced Complexity**: Eliminated complex Identity infrastructure
2. **API-First**: All authentication handled by dedicated API
3. **Scalability**: JWT tokens enable stateless authentication
4. **Maintainability**: Single source of truth for authentication logic
5. **Flexibility**: Easy to add new authentication features via API

## Future Enhancements
- Password reset functionality (when added to API)
- Two-factor authentication (when added to API)
- User profile management (when added to API)
- External login providers (when added to API)
- Real-time token refresh notifications

## Technical Notes
- JWT tokens include user claims (ID, email, roles)
- Automatic redirect to login for unauthorized access
- Compatible with existing `AuthorizeView` and `[Authorize]` attributes
- Token expiration handled gracefully with automatic refresh
- Logout properly clears all authentication state

This transition provides a solid foundation for JWT-based authentication while maintaining the familiar Blazor authorization experience. 