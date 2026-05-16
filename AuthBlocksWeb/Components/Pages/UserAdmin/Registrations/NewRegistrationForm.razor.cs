using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Converters;
using AuthBlocksModels.InputModels;
using AuthBlocksWeb.ApiClients;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace AuthBlocksWeb.Components.Pages.UserAdmin.Registrations;

public partial class NewRegistrationForm : ComponentBase
{
    private IEnumerable<RoleInputModel>? _roles;
    
    [SupplyParameterFromForm]
    public PendingRegistrationInputModel Input { get; set; } = new();
    
    [Inject]
    public required PendingRegistrationClient Client { get; set; }

    [Inject]
    public required RoleClient RoleClient { get; set; }
    
    [Inject]
    public required NavigationManager Navigation { get; set; }
    
    [Inject]
    public required IDialogService DialogService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var result = await RoleClient.GetAll();
        if (result is { Success: false } or { Value: null }) 
            return;
        
        _roles = result.Value.Select(RoleModelToInputConverter.Convert);
    }

    private async Task CreatePendingRegistration(EditContext arg)
    {
        Task<RegistrationCreatedResult> resultTask = Task.Run(async () => 
            await Client.CreatePendingRegistration(Input.Email, 
                                                   Input.Roles?.Select(RoleModelToInputConverter.Convert),
                                                   Navigation.ToAbsoluteUri("account/register").AbsoluteUri));

        var parameters = new DialogParameters<RegistrationSubmittedModal>
        {
            { x => x.ResultTask, resultTask },
        };
        var options = new DialogOptions { CloseButton = true, FullWidth = true };

        // Post and show results
        var dialog = await DialogService.ShowAsync<RegistrationSubmittedModal>($"Sending Account Registration", parameters, options);
        var dialogResult = await dialog.Result;

        if(dialogResult != null && 
           !dialogResult.Canceled && 
           dialogResult.Data is NetBlocks.Models.Result result &&
           result.Success)
        {
            Navigation.NavigateTo($"/useradmin/registrations", forceLoad: true);
        }
    }
}