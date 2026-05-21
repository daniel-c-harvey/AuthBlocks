namespace AuthBlocksWeb.Services;

/// <summary>
/// Called by <see cref="AuthorizingModelClient{TModel,TConfig}"/> whenever a session
/// cannot be recovered (proactive expiry with failed refresh, or reactive 401 with
/// failed refresh). Implementing code should clear tokens and notify the Blazor auth
/// cascade so components immediately reflect the unauthenticated state.
/// </summary>
public interface ISessionExpiredAction
{
    Task HandleAsync();
}
