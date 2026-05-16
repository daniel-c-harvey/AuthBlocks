using Web.Shared.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public class AuthClientConfig : ModelClientConfig
{
    public AuthClientConfig(string baseURL, int port) : base(baseURL, port, "auth")
    {
    }

    public AuthClientConfig(string url) : base(url, "auth")
    {
    }
}