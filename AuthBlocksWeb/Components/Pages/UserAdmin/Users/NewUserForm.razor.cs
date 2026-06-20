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
            return;
        }

        var rolesResult = await AuthApiClient.GetRolesAsync(token);
        if (rolesResult.Success && rolesResult.Value is not null)
        {
            _availableRoles = rolesResult.Value;
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
                Message = "Registration failed. Please check your input and try again.";
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
