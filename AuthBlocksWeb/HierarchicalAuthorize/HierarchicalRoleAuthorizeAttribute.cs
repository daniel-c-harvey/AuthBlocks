using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AuthBlocksWeb.HierarchicalAuthorize;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HierarchicalRoleAuthorizeAttribute : AuthorizeAttribute
{
    public HierarchicalRoleAuthorizeAttribute(params string[]? roles)
    {
        // Only set Roles if roles are actually specified
        // If no roles are specified, it works like [Authorize] - just requires authentication
        if (roles != null && roles.Length > 0)
        {
            Roles = string.Join(",", roles);
        }
        // If roles is null or empty, don't set Roles property - this makes it work like [Authorize]
    }
}

public class HierarchicalRoleAuthorizeView : ComponentBase
{
    [Parameter]
    public string[] RolesList { get; set; } = [];

    [Parameter]
    public RenderFragment<AuthenticationState>? Authorized { get; set; }

    [Parameter]
    public RenderFragment<AuthenticationState>? NotAuthorized { get; set; }

    [Parameter]
    public RenderFragment? Authorizing { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [Inject]
    private IAuthorizationService AuthorizationService { get; set; } = default!;

    private bool _isAuthorized = false;
    private bool _isAuthorizing = true;
    private AuthenticationState? _currentAuthState;

    protected override async Task OnInitializedAsync()
    {
        await CheckAuthorizationAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await CheckAuthorizationAsync();
    }

    private async Task CheckAuthorizationAsync()
    {
        if (AuthenticationState == null)
        {
            _isAuthorized = false;
            _isAuthorizing = false;
            return;
        }

        _isAuthorizing = true;
        StateHasChanged();

        try
        {
            var authState = await AuthenticationState;
            _currentAuthState = authState;
            var user = authState.User;

            if (!(user.Identity?.IsAuthenticated ?? false))
            {
                _isAuthorized = false;
            }
            else if (RolesList.Length == 0)
            {
                // No roles specified, just need to be authenticated
                _isAuthorized = true;
            }
            else
            {
                // Check hierarchical role authorization using the authorization service
                // Create a custom requirement with the specific roles we need to check
                var requirement = new RolesAuthorizationRequirement(RolesList);
                var result = await AuthorizationService.AuthorizeAsync(user, null, requirement);
                _isAuthorized = result.Succeeded;
            }
        }
        catch
        {
            _isAuthorized = false;
        }
        finally
        {
            _isAuthorizing = false;
            StateHasChanged();
        }
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        if (_isAuthorizing)
        {
            builder.AddContent(0, Authorizing);
        }
        else if (_isAuthorized && _currentAuthState != null)
        {
            builder.AddContent(1, Authorized?.Invoke(_currentAuthState));
        }
        else if (_currentAuthState != null)
        {
            builder.AddContent(2, NotAuthorized?.Invoke(_currentAuthState));
        }
    }
} 