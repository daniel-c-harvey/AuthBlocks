using Web.Shared.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public class RolesClientConfig : ModelClientConfig
{
    public RolesClientConfig(string baseURL, int port) : base(baseURL, port, "roles")
    {
    }

    public RolesClientConfig(string url) : base(url, "roles")
    {
    }
}