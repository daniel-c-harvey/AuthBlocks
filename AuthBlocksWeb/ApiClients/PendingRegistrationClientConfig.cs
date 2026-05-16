using Web.Shared.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public class PendingRegistrationClientConfig : ModelClientConfig
{
    public PendingRegistrationClientConfig(string baseURL, int port) : base(baseURL, port, "pendingregistration")
    {
    }

    public PendingRegistrationClientConfig(string url) : base(url, "pendingregistration")
    {
    }
}