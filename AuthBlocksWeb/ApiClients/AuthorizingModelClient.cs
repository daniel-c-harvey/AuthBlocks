using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthBlocksModels.ApiModels;
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

    private readonly ITokenService _tokenService;
    private readonly IAuthApiClient _authApiClient;

    protected AuthorizingModelClient(
        TConfig config,
        IOptions<JsonSerializerOptions> options,
        ITokenService tokenService,
        IAuthApiClient authApiClient) : base(config, options)
    {
        _tokenService = tokenService;
        _authApiClient = authApiClient;
    }

    protected async Task<Result> AddAuthorizationHeader()
    {
        var token = await _tokenService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return Result.CreateFailResult(SessionExpiredMessage);
        }

        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return Result.CreatePassResult();
    }

    protected void ClearAuthorizationHeader()
    {
        http.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Verifies the access token is still valid (proactive check). If the
    /// JWT has expired or is missing, attempts a single refresh via the
    /// stored refresh token. Returns a failing <see cref="Result"/> with
    /// <see cref="SessionExpiredMessage"/> when no usable token can be
    /// obtained so callers can short-circuit the request.
    /// </summary>
    private async Task<Result> EnsureValidTokenAsync()
    {
        if (await _tokenService.IsTokenValidAsync())
        {
            return Result.CreatePassResult();
        }

        return await TryRefreshTokensAsync()
            ? Result.CreatePassResult()
            : Result.CreateFailResult(SessionExpiredMessage);
    }

    /// <summary>
    /// Calls the refresh endpoint with the currently stored access + refresh
    /// tokens. On success <see cref="AuthApiClient"/> writes the new pair to
    /// the token store before returning, so no duplicate storage is needed
    /// here. Any failure (missing tokens, server rejection, exception) is
    /// reported as <c>false</c>.
    /// </summary>
    private async Task<bool> TryRefreshTokensAsync()
    {
        try
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();
            var refreshToken = await _tokenService.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var response = await _authApiClient.RefreshTokenAsync(new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            });
            return response is { Success: true, Value: not null };
        }
        catch
        {
            return false;
        }
    }

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
            if (await EnsureValidTokenAsync() is { Success: false } sessionError)
            {
                return ApiResult<TResult>.From(sessionError);
            }
            if (await AddAuthorizationHeader() is { Success: false } headerError)
            {
                return ApiResult<TResult>.From(headerError);
            }

            var response = await send();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Reactive: token was accepted by IsTokenValidAsync (still
                // within its exp window) but the server rejected it anyway
                // (revoked, key rotation, etc.). Refresh + retry once.
                if (!await TryRefreshTokensAsync())
                {
                    return ApiResult<TResult>.CreateFailResult(SessionExpiredMessage);
                }
                if (await AddAuthorizationHeader() is { Success: false } retryHeaderError)
                {
                    return ApiResult<TResult>.From(retryHeaderError);
                }

                response = await send();
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return ApiResult<TResult>.CreateFailResult(SessionExpiredMessage);
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
            if (await EnsureValidTokenAsync() is { Success: false } sessionError)
            {
                return ApiResult.From(sessionError);
            }
            if (await AddAuthorizationHeader() is { Success: false } headerError)
            {
                return ApiResult.From(headerError);
            }

            var response = await send();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (!await TryRefreshTokensAsync())
                {
                    return ApiResult.CreateFailResult(SessionExpiredMessage);
                }
                if (await AddAuthorizationHeader() is { Success: false } retryHeaderError)
                {
                    return ApiResult.From(retryHeaderError);
                }

                response = await send();
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return ApiResult.CreateFailResult(SessionExpiredMessage);
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
