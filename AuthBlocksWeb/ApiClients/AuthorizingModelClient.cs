using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthBlocksWeb.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Models.Common;
using Models.Models;
using NetBlocks.Models;
using Web.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public abstract class AuthorizingModelClient<TModel, TConfig> : ModelClient<TModel, TConfig>
    where TModel : class, IModel, new()
    where TConfig : ModelClientConfig
{
    // Signal string consumers (Skipper ViewModels) match on to distinguish
    // an auth-session failure from a domain API error. NetBlocks' ApiResult
    // has no dedicated error-code surface, so the message text is the contract.
    public const string SessionExpiredMessage = "Session expired";

    protected readonly IAuthSession _authSession;

    protected AuthorizingModelClient(
        TConfig config,
        IOptions<JsonSerializerOptions> options,
        IAuthSession authSession) : base(config, options)
    {
        _authSession = authSession;
    }

    // Header helpers are protected (not private) so that the rare endpoint
    // which can't be expressed through SendWithAuth (different result type,
    // custom DTO shape, etc.) can still participate in the auth lifecycle by
    // composing GetValidTokenAsync + ForceRefreshAsync from IAuthSession.
    // See PendingRegistrationClient.CreatePendingRegistration for an example.
    protected void SetAuthorizationHeader(string token)
        => http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    protected void ClearAuthorizationHeader()
        => http.DefaultRequestHeaders.Authorization = null;

    /// <summary>
    /// Runs <paramref name="send"/> wrapped in the auth lifecycle: proactive
    /// token validity check, header attach, single retry on a 401 response,
    /// and guaranteed header clear. <paramref name="send"/> performs the
    /// HTTP call and returns the raw <see cref="HttpResponseMessage"/> so we
    /// can inspect the status code without depending on the base
    /// <see cref="ModelClient{TModel, TConfig}"/> swallowing exceptions.
    /// </summary>
    protected async Task<ApiResult<TResult>> SendWithAuth<TResult>(
        Func<Task<HttpResponseMessage>> send,
        Func<HttpResponseMessage, Task<ApiResult<TResult>>> deserialize)
    {
        try
        {
            // Proactive: AuthSession handles expiry check, refresh, and
            // cascade notification when no usable token can be obtained.
            var token = await _authSession.GetValidTokenAsync();
            if (token == null)
            {
                return ApiResult<TResult>.CreateFailResult(SessionExpiredMessage);
            }

            SetAuthorizationHeader(token);
            var response = await send();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Reactive: token was valid locally but the server rejected
                // it (revoked, key rotation, etc.). Force a refresh and retry
                // once. AuthSession handles cascade notification on failure.
                var refreshedToken = await _authSession.ForceRefreshAsync();
                if (refreshedToken == null)
                {
                    return ApiResult<TResult>.CreateFailResult(SessionExpiredMessage);
                }

                SetAuthorizationHeader(refreshedToken);
                response = await send();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Refresh succeeded but the server still 401'd — this is
                    // an authorization issue, not session expiry. Don't log
                    // the user out; let the caller surface the error.
                    return ApiResult<TResult>.CreateFailResult("Authorization failed");
                }
            }

            return await deserialize(response);
        }
        catch (Exception e)
        {
            return ApiResult<TResult>.CreateFailResult(e.Message);
        }
        finally
        {
            ClearAuthorizationHeader();
        }
    }

    /// <summary>
    /// Non-generic sibling of <see cref="SendWithAuth{TResult}"/> for
    /// endpoints that return a bare <see cref="ApiResult"/>.
    /// </summary>
    protected async Task<ApiResult> SendWithAuth(
        Func<Task<HttpResponseMessage>> send,
        Func<HttpResponseMessage, Task<ApiResult>> deserialize)
    {
        try
        {
            var token = await _authSession.GetValidTokenAsync();
            if (token == null)
            {
                return ApiResult.CreateFailResult(SessionExpiredMessage);
            }

            SetAuthorizationHeader(token);
            var response = await send();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshedToken = await _authSession.ForceRefreshAsync();
                if (refreshedToken == null)
                {
                    return ApiResult.CreateFailResult(SessionExpiredMessage);
                }

                SetAuthorizationHeader(refreshedToken);
                response = await send();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return ApiResult.CreateFailResult("Authorization failed");
                }
            }

            return await deserialize(response);
        }
        catch (Exception e)
        {
            return ApiResult.CreateFailResult(e.Message);
        }
        finally
        {
            ClearAuthorizationHeader();
        }
    }

    private static string BuildPagedQueryUri(string basePath, PagedQuery query)
    {
        var queryMap = new Dictionary<string, string?>
        {
            { nameof(query.Page).ToLower(), query.Page.ToString() },
            { nameof(query.PageSize).ToLower(), query.PageSize.ToString() },
            { nameof(query.Search).ToLower(), query.Search },
            { nameof(query.Sort).ToLower(), query.Sort },
            { nameof(query.Desc).ToLower(), query.Desc.ToString() },
        };
        var filteredQuery = queryMap.Where(kv => kv.Value != null)
                                    .ToDictionary(kv => kv.Key, kv => kv.Value);
        return QueryHelpers.AddQueryString(basePath, filteredQuery);
    }

    protected async Task<ApiResult<T>> DeserializeApiResult<T>(HttpResponseMessage response)
    {
        var dto = await response.Content.ReadFromJsonAsync<ApiResultDto<T>>(Options)
                  ?? throw new HttpRequestException("Failed to deserialize response");
        return dto.From();
    }

    protected async Task<ApiResult> DeserializeApiResult(HttpResponseMessage response)
    {
        var dto = await response.Content.ReadFromJsonAsync<ApiResultDto>(Options)
                  ?? throw new HttpRequestException("Failed to deserialize response");
        return dto.From();
    }

    /* Model Client Overrides */
    public override Task<ApiResult<TModel>> GetById(long id)
        => SendWithAuth(
            () => http.GetAsync($"api/{config.ControllerName}/{id}"),
            DeserializeApiResult<TModel>);

    public override Task<ApiResult<IEnumerable<TModel>>> GetAll()
        => SendWithAuth(
            () => http.GetAsync($"api/{config.ControllerName}/all"),
            DeserializeApiResult<IEnumerable<TModel>>);

    public override Task<ApiResult<PagedResult<TModel>>> GetByPage(PagedQuery query)
        => SendWithAuth(
            () => http.GetAsync(BuildPagedQueryUri($"api/{config.ControllerName}", query)),
            DeserializeApiResult<PagedResult<TModel>>);

    public override Task<ApiResult<ItemCount>> GetPageCount(PagedQuery query)
        => SendWithAuth(
            () => http.GetAsync(BuildPagedQueryUri($"api/{config.ControllerName}/count", query)),
            DeserializeApiResult<ItemCount>);

    // Note: the send delegate is called twice on the 401-retry path.
    // This is safe here because ModelController.Post is an upsert keyed on model.Id.
    // Subclass overrides of Update that add non-idempotent side effects must account for this.
    // Note: LastUpdateOutcome (base class side-channel) is not populated — base.Update is bypassed intentionally.
    public override Task<ApiResult<TModel>> Update(TModel model)
        => SendWithAuth(
            () => http.PostAsJsonAsync($"api/{config.ControllerName}", model, Options),
            DeserializeApiResult<TModel>);

    // Note: LastDeleteOutcome (base class side-channel) is not populated — base.Delete is bypassed intentionally.
    public override Task<ApiResult> Delete(TModel model)
        => SendWithAuth(
            () => http.DeleteAsync($"api/{config.ControllerName}/{model.Id}"),
            DeserializeApiResult);
}
