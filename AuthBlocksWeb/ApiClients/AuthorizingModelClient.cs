using System.Text.Json;
using AuthBlocksModels.Models;
using AuthBlocksWeb.Services;
using Microsoft.Extensions.Options;
using Models.Shared.Common;
using Models.Shared.Models;
using NetBlocks.Models;
using Web.Shared.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public abstract class AuthorizingModelClient<TModel, TConfig> : ModelClient<TModel, TConfig> 
    where TModel : class, IModel, new()
    where TConfig : ModelClientConfig
{
    private readonly ITokenService _tokenService;
    
    protected AuthorizingModelClient(TConfig config, IOptions<JsonSerializerOptions> options, ITokenService tokenService) : base(config, options)
    {
        _tokenService = tokenService;
    }

    protected async Task<Result> AddAuthorizationHeader()
    {
        // Add authorization header
        var token = await _tokenService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return Result.CreateFailResult("No access token available");
        }

        http.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        return Result.CreatePassResult();
    }

    protected void ClearAuthorizationHeader()
    {
        http.DefaultRequestHeaders.Authorization = null;
    }
    
    /* Model Client Overrides */
    public override async Task<ApiResult<TModel>> GetById(long id)
    {
        if (await AddAuthorizationHeader() is {Success: false} error) return ApiResult<TModel>.From(error);
        var result = await base.GetById(id);
        ClearAuthorizationHeader();
        return result;
    }
    
    public override async Task<ApiResult<IEnumerable<TModel>>> GetAll()
    {
        if (await AddAuthorizationHeader() is {Success: false} error) return ApiResult<IEnumerable<TModel>>.From(error);
        var result = await base.GetAll();
        ClearAuthorizationHeader();
        return result;   
    }
    
    public override async Task<ApiResult<PagedResult<TModel>>> GetByPage(PagedQuery query)
    {
        if (await AddAuthorizationHeader() is {Success: false} error) return ApiResult<PagedResult<TModel>>.From(error);
        var result = await base.GetByPage(query);
        ClearAuthorizationHeader();
        return result;
    }

    public override async Task<ApiResult<ItemCount>> GetPageCount(PagedQuery query)
    {
        if (await AddAuthorizationHeader() is {Success: false} error) return ApiResult<ItemCount>.From(error);
        var result = await base.GetPageCount(query);
        ClearAuthorizationHeader();
        return result;
    }

    public override async Task<ApiResult<TModel>> Update(TModel model)
    {
        if (await AddAuthorizationHeader() is {Success: false} error) return ApiResult<TModel>.From(error);
        var result = await base.Update(model);
        ClearAuthorizationHeader();
        return result;
    }

    public override async Task<ApiResult> Delete(TModel model)
    {
        if (await AddAuthorizationHeader() is {Success: false} error) return ApiResult.From(error);
        var result = await base.Delete(model);
        ClearAuthorizationHeader();
        return result;
    }
    
}