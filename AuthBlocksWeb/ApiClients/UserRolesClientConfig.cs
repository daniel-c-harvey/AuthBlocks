using Web.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public class UserRolesClientConfig : ModelClientConfig
{
    public UserRolesClientConfig(string baseURL, int port) : base(baseURL, port, "userroles")
    {
    }

    public UserRolesClientConfig(string url) : base(url, "userroles")
    {
    }
}
