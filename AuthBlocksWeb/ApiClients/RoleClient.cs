using System.Text.Json;
using AuthBlocksModels.Models;
using AuthBlocksWeb.Services;
using Microsoft.Extensions.Options;
using Models.Shared.Common;
using NetBlocks.Models;
using Web.Shared.ApiClients;

namespace AuthBlocksWeb.ApiClients;

public class RoleClient : AuthorizingModelClient<RoleModel, RolesClientConfig>, IRoleApiClient
{
    public RoleClient(RolesClientConfig config, IOptions<JsonSerializerOptions> options, ITokenService tokenService) : base(config, options, tokenService)
    {
    }
}