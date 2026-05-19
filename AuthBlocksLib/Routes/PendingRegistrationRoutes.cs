using System.Linq.Expressions;
using API.Common.Email.Mailtrap;
using AuthBlocksData.Services;
using AuthBlocksLib.Common;
using AuthBlocksLib.Services;
using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Entities;
using AuthBlocksModels.Models;
using AuthBlocksModels.SystemDefinitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Models.Common;
using NetBlocks.Models;

namespace AuthBlocksLib.Routes;

internal static class PendingRegistrationRoutes
{
    private static readonly Dictionary<string, Expression<Func<PendingRegistration, object>>> SortExpressions = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(PendingRegistration.Id)] = e => e.Id,
        ["CreatedAt"] = e => e.CreatedAt,
        ["UpdatedAt"] = e => e.UpdatedAt,
    };

    private static Expression<Func<PendingRegistration, bool>> BuildSearch(string? _) => e => true;

    public static void Map(IEndpointRouteBuilder root)
    {
        // Class-level [HierarchicalRoleAuthorize(UserAdmin)] on PendingRegistrationController
        // → group-level role requirement; the hierarchical handler picks up the RolesAuthorizationRequirement.
        var group = root.MapGroup("api/pendingregistration")
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));

        // Base ModelController CRUD surface (inherits the class-level auth).
        group.MapGet("", async ([AsParameters] PagedQuery query, IPendingRegistrationService service) =>
            await RouteHelpers.GetPage<PendingRegistration, PendingRegistrationModel>(service, query, null, BuildSearch, SortExpressions));

        group.MapGet("all", async (IPendingRegistrationService service) =>
            await RouteHelpers.GetAll<PendingRegistration, PendingRegistrationModel>(service));

        group.MapGet("count", async ([AsParameters] PagedQuery query, IPendingRegistrationService service) =>
            await RouteHelpers.GetCount<PendingRegistration, PendingRegistrationModel>(service, query, BuildSearch, SortExpressions));

        group.MapGet("{id:long}", async (long id, IPendingRegistrationService service) =>
            await RouteHelpers.GetById<PendingRegistration, PendingRegistrationModel>(service, id));

        group.MapPost("", async ([FromBody] PendingRegistrationModel model, IPendingRegistrationService service) =>
            await RouteHelpers.Post<PendingRegistration, PendingRegistrationModel>(service, model));

        group.MapDelete("{id:long}", async (long id, IPendingRegistrationService service) =>
            await RouteHelpers.Delete<PendingRegistration, PendingRegistrationModel>(service, id));

        // POST /api/pendingregistration/create
        group.MapPost("create", Create);
    }

    private static async Task<IResult> Create(
        [FromBody] CreatePendingRegistrationRequest model,
        IRegistrationTokenService tokenService,
        IPendingRegistrationService service,
        IUserService userService,
        IGeneralEmailSender emailSender,
        AuthBlocksOptions options,
        ILogger<PendingRegistrationLogger> logger)
    {
        var existingUser = await userService.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            var resultFailure = RegistrationCreatedResult.CreateFailResult("User with this email already exists");
            return Results.BadRequest(new RegistrationCreatedResult.RegistrationCreatedResultDto(resultFailure));
        }

        var existingRegistrationResult = await service.FindByEmail(model.Email);
        if (existingRegistrationResult is { Value: PendingRegistrationModel })
        {
            var resultFailure = RegistrationCreatedResult.CreateFailResult("User with this email already pending registration");
            return Results.BadRequest(new RegistrationCreatedResult.RegistrationCreatedResultDto(resultFailure));
        }

        var tokenResult = await tokenService.GenerateTokenAsync(model.Email);

        if (tokenResult is
            {
                Success: true,
                RegistrationEmail: string email,
                RegistrationToken: string token,
                RegistrationTokenHash: string hash,
                TokenExpiration: TimeSpan expiration,
            })
        {
            var pendingRegistration = new PendingRegistrationModel
            {
                PendingUserEmail = email,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration),
                IsConsumed = false,
                Roles = model.Roles
            };

            try
            {
                await service.Create(hash, pendingRegistration);

                var subject = $"[{options.ApplicationName}] Register New Account";
                var link = QueryHelpers.AddQueryString(model.ReturnHost, new Dictionary<string, string?>
                {
                    ["UserEmail"] = email,
                    ["RegistrationToken"] = token
                });
                var message = RegistrationEmailTemplate.Create(token, link, options.ApplicationName, options.SupportEmail);
                await emailSender.SendEmailAsync(email, null, subject, message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create pending registration or send registration email for {Email}", email);
                var failure = RegistrationCreatedResult.CreateFailResult("Failed to send registration email. Please try again or contact support.");
                return Results.Json(new RegistrationCreatedResult.RegistrationCreatedResultDto(failure), statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        var result = RegistrationCreatedResult.From(tokenResult, tokenResult.RegistrationEmail);
        var resultDto = new RegistrationCreatedResult.RegistrationCreatedResultDto(result);

        return tokenResult.Success
            ? Results.Ok(resultDto)
            : Results.Json(resultDto, statusCode: StatusCodes.Status500InternalServerError);
    }

    /// <summary>Type tag used purely so ILogger&lt;T&gt; gives a clean category name for AuthBlocks pending registration routes.</summary>
    internal sealed class PendingRegistrationLogger { }
}
