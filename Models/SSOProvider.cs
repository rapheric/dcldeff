using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

/// <summary>
/// Stores SSO configuration for external identity providers
/// Supports OAuth2 and OpenID Connect providers
/// </summary>
public class SSOProvider
{
    public Guid Id { get; set; }

    /// <summary>
    /// Provider name: Google, Microsoft, OIDC, SAML
    /// </summary>
    [Required]
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Provider type: OAuth2, OpenIDConnect, SAML2
    /// </summary>
    [Required]
    public string ProviderType { get; set; } = "OAuth2";

    /// <summary>
    /// Client ID from the provider
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret (should be encrypted in database)
    /// </summary>
    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Authority/Issuer URL
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Token endpoint URL
    /// </summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// Authorization endpoint URL
    /// </summary>
    public string? AuthorizationEndpoint { get; set; }

    /// <summary>
    /// User info endpoint URL
    /// </summary>
    public string? UserInfoEndpoint { get; set; }

    /// <summary>
    /// Redirect URI (callback URL)
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// Scopes to request from provider
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// Is this provider currently enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Icon URL for login button
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Display order on login page
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<SSOConnection> UserConnections { get; set; } = new List<SSOConnection>();
    public ICollection<SSOLog> SSOLogs { get; set; } = new List<SSOLog>();
}

/// <summary>
/// Links a user to their SSO provider account
/// </summary>
public class SSOConnection
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public User User { get; set; } = null!;

    [Required]
    public Guid SSOProviderId { get; set; }

    [Required]
    public SSOProvider Provider { get; set; } = null!;

    /// <summary>
    /// Provider's unique identifier for this user
    /// </summary>
    [Required]
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email from provider
    /// </summary>
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// User's name from provider
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// Additional profile data as JSON
    /// </summary>
    public string? ProviderProfileData { get; set; }

    /// <summary>
    /// Access token (encrypted)
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token (encrypted)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// Is this connection active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Logs SSO authentication attempts
/// </summary>
public class SSOLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    [Required]
    public Guid SSOProviderId { get; set; }

    public SSOProvider Provider { get; set; } = null!;

    /// <summary>
    /// Type of SSO operation: Login, LinkAccount, UnlinkAccount
    /// </summary>
    [Required]
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Was the operation successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Reason for failure if applicable
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// IP Address from which the attempt was made
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent information
    /// </summary>
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
