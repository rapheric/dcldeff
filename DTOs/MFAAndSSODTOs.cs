using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NCBA.DCL.DTOs;

// ==================== MFA DTOs ====================

public class SetupMFARequest
{
    [Required]
    public Guid UserId { get; set; }
}

public class SetupMFAResponse
{
    /// <summary>
    /// QR Code data (base64 encoded or as string)
    /// </summary>
    public string? QRCodeUrl { get; set; }

    /// <summary>
    /// TOTP secret for manual entry
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Setup session token
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// Backup codes
    /// </summary>
    public List<string>? BackupCodes { get; set; }

    /// <summary>
    /// Instructions for setting up MFA
    /// </summary>
    public string Instructions { get; set; } = "Use an authenticator app (Google Authenticator, Authy, Microsoft Authenticator) to scan the QR code or enter the secret manually.";
}

public class VerifyMFASetupRequest
{
    [Required]
    [StringLength(6)]
    public string TOTPCode { get; set; } = string.Empty;

    [Required]
    public string SessionToken { get; set; } = string.Empty;
}

public class VerifyMFAResponse
{
    public bool IsVerified { get; set; }
    public List<string>? BackupCodes { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DisableMFARequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class GenerateBackupCodesRequest
{
    [Required]
    public Guid UserId { get; set; }
}

public class GenerateBackupCodesResponse
{
    public List<string> BackupCodes { get; set; } = new();
}

public class VerifyMFATokenRequest
{
    [Required]
    [StringLength(20)]
    public string MFAToken { get; set; } = string.Empty;

    [Required]
    public string SessionToken { get; set; } = string.Empty;
}

public class VerifyMFATokenResponse
{
    public bool IsValid { get; set; }
    public string? Token { get; set; }
    public UserResponse? User { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TrustedDeviceRequest
{
    [Required]
    public string DeviceFingerprint { get; set; } = string.Empty;

    public string? DeviceName { get; set; }
    public bool TrustForDays { get; set; } = false;
    public int TrustDuration { get; set; } = 30; // days
}

public class TrustedDeviceResponse
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public DateTime LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}

public class MFAStatusResponse
{
    public bool IsMFAEnabled { get; set; }
    public bool IsTotpEnabled { get; set; }
    public bool IsBackupCodesEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public List<TrustedDeviceResponse> TrustedDevices { get; set; } = new();
}

// ==================== SSO DTOs ====================

public class SSOProviderSetupDto
{
    public Guid Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
}

public class SSOLoginInitializeRequest
{
    [Required]
    public Guid SSOProviderId { get; set; }

    public string? RedirectUri { get; set; }
    public string? State { get; set; }
}

public class SSOLoginInitializeResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
}

public class SSOCallbackRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public Guid SSOProviderId { get; set; }

    public string? State { get; set; }
    public string? CodeVerifier { get; set; }
}

public class SSOCallbackResponse
{
    public string? Token { get; set; }
    public UserResponse? User { get; set; }
    public bool IsNewUser { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class LinkSSOAccountRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public Guid SSOProviderId { get; set; }
}

public class UnlinkSSOAccountRequest
{
    [Required]
    public Guid SSOProviderId { get; set; }

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class SSOConnectionResponse
{
    public Guid Id { get; set; }
    public Guid SSOProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class SSOStatusResponse
{
    public List<SSOProviderSetupDto> AvailableProviders { get; set; } = new();
    public List<SSOConnectionResponse> LinkedAccounts { get; set; } = new();
}
