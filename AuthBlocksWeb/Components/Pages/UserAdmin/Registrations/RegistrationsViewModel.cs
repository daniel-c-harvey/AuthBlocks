using AuthBlocksModels.Converters;
using AuthBlocksModels.InputModels;
using AuthBlocksModels.Models;
using AuthBlocksWeb.ApiClients;
using Web.Shared.Maintenance.Entities;

namespace AuthBlocksWeb.Components.Pages.UserAdmin.Registrations;

public class RegistrationsViewModel : ModelPageViewModel<PendingRegistrationModel, PendingRegistrationClient, PendingRegistrationClientConfig>
{
    public RegistrationsViewModel(PendingRegistrationClient client) : base(client)
    {
    }
}