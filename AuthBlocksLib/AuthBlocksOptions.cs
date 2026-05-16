using AuthBlocksLib.Models;
using AuthBlocksLib.Options;
using NetBlocks.Models.Environment;

namespace AuthBlocksLib;

/// <summary>
/// Configuration the host populates when calling <see cref="AuthBlocksExtensions.AddAuthBlocks"/>.
/// </summary>
public class AuthBlocksOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public JwtSettings JwtSettings { get; set; } = new();
    public EmailConnection EmailConnection { get; set; } = new();

    /// <summary>
    /// Required. Product name used in outgoing AuthBlocks correspondence (email subject, registration template).
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Optional. Support email surfaced in the registration template. When empty, the template omits the clickable mailto link.
    /// </summary>
    public string SupportEmail { get; set; } = string.Empty;

    /// <summary>
    /// Optional. When null, admin user seeding is skipped (system roles still seed).
    /// </summary>
    public AdminUserSettings? AdminUserSettings { get; set; }
}
