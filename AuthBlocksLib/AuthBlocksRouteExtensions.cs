using AuthBlocksLib.Routes;
using Microsoft.AspNetCore.Routing;

namespace AuthBlocksLib;

/// <summary>
/// Fluent endpoint-mapping surface for hosts that want to expose AuthBlocks routes.
/// </summary>
public static class AuthBlocksRouteExtensions
{
    /// <summary>
    /// Map every AuthBlocks route under <c>api/{controllerName}/...</c>, preserving the
    /// pattern that the standalone host this library replaces exposed.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthBlocks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        AuthRoutes.Map(endpoints);
        UsersRoutes.Map(endpoints);
        RolesRoutes.Map(endpoints);
        UserRolesRoutes.Map(endpoints);
        PendingRegistrationRoutes.Map(endpoints);

        return endpoints;
    }
}
