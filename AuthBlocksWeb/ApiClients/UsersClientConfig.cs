using Web.Shared.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public class UsersClientConfig : ModelClientConfig
{
    public UsersClientConfig(string baseURL, int port) : base(baseURL, port, "users")
    {
    }

    public UsersClientConfig(string url) : base(url, "users")
    {
    }
}