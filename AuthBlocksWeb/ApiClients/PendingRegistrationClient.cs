using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Models;
using AuthBlocksWeb.Services;
using Microsoft.Extensions.Options;

namespace AuthBlocksWeb.ApiClients;

public class PendingRegistrationClient : AuthorizingModelClient<PendingRegistrationModel, PendingRegistrationClientConfig>, IPendingRegistrationClient
{
    private readonly ITokenService _tokenService;

    public PendingRegistrationClient(PendingRegistrationClientConfig config,
                                     IOptions<JsonSerializerOptions> options,
                                     ITokenService tokenService)
        : base(config, options, tokenService)
    {
        _tokenService = tokenService;
    }

    // Cannot use SendWithAuth because the endpoint returns RegistrationCreatedResult
    // (a NetBlocks ResultBase<T> sibling of ApiResult, not an ApiResult itself), so
    // we run the auth lifecycle inline via ITokenService. Same proactive-then-reactive
    // pattern as SendWithAuth; TokenService handles cascade notification on failure.
    public async Task<RegistrationCreatedResult> CreatePendingRegistration(string email, IEnumerable<RoleModel>? roles, string returnUrl)
    {
        try
        {
            var token = await _tokenService.GetValidTokenAsync();
            if (token == null)
            {
                return RegistrationCreatedResult.CreateFailResult(SessionExpiredMessage);
            }

            SetAuthorizationHeader(token);
            var request = new CreatePendingRegistrationRequest { Email = email, Roles = roles?.ToArray(), ReturnHost = returnUrl };
            var response = await http.PostAsJsonAsync($"api/{config.ControllerName}/create", request, Options);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshedToken = await _tokenService.ForceRefreshAsync();
                if (refreshedToken == null)
                {
                    return RegistrationCreatedResult.CreateFailResult(SessionExpiredMessage);
                }

                SetAuthorizationHeader(refreshedToken);
                response = await http.PostAsJsonAsync($"api/{config.ControllerName}/create", request, Options);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RegistrationCreatedResult.CreateFailResult("Authorization failed");
                }
            }

            var dto = await response.Content.ReadFromJsonAsync<RegistrationCreatedResult.RegistrationCreatedResultDto>(Options)
                      ?? throw new HttpRequestException("Failed to deserialize response");
            return dto.From();
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
