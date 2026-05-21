using System.Net.Http.Json;
using System.Text.Json;
using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Models;
using AuthBlocksWeb.Services;
using Microsoft.Extensions.Options;
using NetBlocks.Models;

namespace AuthBlocksWeb.ApiClients;

public class PendingRegistrationClient : AuthorizingModelClient<PendingRegistrationModel, PendingRegistrationClientConfig>, IPendingRegistrationClient
{
    public PendingRegistrationClient(PendingRegistrationClientConfig config,
                                     IOptions<JsonSerializerOptions> options,
                                     ITokenService tokenService,
                                     IAuthApiClient authApiClient,
                                     ISessionExpiredAction sessionExpiredAction)
    : base(config, options, tokenService, authApiClient, sessionExpiredAction)
    {
    }

    public async Task<RegistrationCreatedResult> CreatePendingRegistration(string email, IEnumerable<RoleModel>? roles, string returnUrl)
    {
        try
        {
            await AddAuthorizationHeader();
            var request = new CreatePendingRegistrationRequest { Email = email, Roles = roles?.ToArray(), ReturnHost = returnUrl };
            var response = await http.PostAsJsonAsync($"api/{config.ControllerName}/create", request, Options);
            if (response == null) throw new HttpRequestException("Failed to get response");

            var result =
                await response.Content.ReadFromJsonAsync<RegistrationCreatedResult.RegistrationCreatedResultDto>(
                    Options)
                ?? throw new HttpRequestException("Failed to deserialize response");

            return result.From();
        }
        catch (Exception)
        {
            return RegistrationCreatedResult.CreateFailResult("Registration could not be completed. Please try again or contact support.");
        }
        finally
        {
            ClearAuthorizationHeader();
        }
    }
}
