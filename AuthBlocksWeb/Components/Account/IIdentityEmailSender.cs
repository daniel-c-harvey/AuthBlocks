using Microsoft.AspNetCore.Identity;

namespace AuthBlocksWeb.Components.Account;

public interface IIdentityEmailSender<TUser> : IEmailSender<TUser>
where TUser : class
{
    Task SendConfirmationAndProfileLink(TUser user, string email, string linkingLink);
}