using AuthBlocksModels.ApiModels;
using AuthBlocksWeb.ApiClients;
using AuthBlocksWeb.Services;
using Microsoft.AspNetCore.Components;

namespace AuthBlocksWeb.Components.Pages.UserAdmin.Users;

public partial class NewUserForm : ComponentBase
{
    private bool _isLoading;
    private List<RoleInfo> _availableRoles = new();

    private string? Message { get; set; }

    [SupplyParameterFromForm]
    public AdminRegisterRequest Input { get; set; } = new();

    [Inject]
    public required IAuthApiClient AuthApiClient { get; set; }

    [Inject]
    public required IAuthSession AuthSession { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var token = await AuthSession.GetValidTokenAsync();
        if (token is null)
        {
            Message = "Roles could not be loaded (session unavailable). You can still create the account without assigning roles.";
            return;
        }

        var rolesResult = await AuthApiClient.GetRolesAsync(token);
        if (rolesResult.Success && rolesResult.Value is not null)
        {
            _availableRoles = rolesResult.Value;
        }
        else
        {
            Message = "Roles could not be loaded. You can still create the account without assigning roles.";
        }
    }

    private void OnRolesChanged(IEnumerable<long> values)
    {
        Input.RoleIds = values.ToList();
    }

    private async Task CreateUser()
    {
        _isLoading = true;
        Message = null;
        StateHasChanged();

        try
        {
            var token = await AuthSession.GetValidTokenAsync();
            if (token is null)
            {
                Message = "Your session has expired. Please log in again.";
                return;
            }

            var result = await AuthApiClient.AdminRegisterAsync(Input, token);
            if (result.Success)
            {
                Navigation.NavigateTo("/useradmin/users", forceLoad: true);
            }
            else
            {
                var serverMessage = string.Join("; ", result.Messages.Select(m => m.Message));
                Message = string.IsNullOrWhiteSpace(serverMessage)
                    ? "Registration failed. Please check your input and try again."
                    : serverMessage;
            }
        }
        catch (Exception)
        {
            Message = "An error occurred during registration. Please try again.";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}
