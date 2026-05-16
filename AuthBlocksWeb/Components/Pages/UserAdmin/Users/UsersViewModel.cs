using AuthBlocksModels.Converters;
using AuthBlocksModels.InputModels;
using AuthBlocksModels.Models;
using AuthBlocksWeb.ApiClients;
using Web.Shared.Maintenance.Entities;

namespace AuthBlocksWeb.Components.Pages.UserAdmin.Users;

public class UsersViewModel : ModelPageViewModel<UserModel, UsersClient, UsersClientConfig>
{
    protected RoleClient RoleClient;
    
    public UsersViewModel(UsersClient client, RoleClient roleClient) : base(client)
    {
        this.RoleClient = roleClient;
    }

    public async Task ToggleDeactivated(UserInputModel item)
    {
        item.IsDeactivated = !item.IsDeactivated;
        await Client.Update(UserModelToInputConverter.Convert(item));
    }
}