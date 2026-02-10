using System.ComponentModel.DataAnnotations;

namespace NCBA.DCL.Models;

/// <summary>
/// Stores Multi-Factor Authentication setup for users
/// Supports TOTP (Time-based One-Time Password) and backup codes
/// </summary>
public class MFASetup
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public User User { get; set; } = null!;

    /// <summary>
    /// TOTP Secret key (encrypted in database)
    /// </summary>
    public string? TotpSecret { get; set; }

    /// <summary>
    /// Backup codes stored as encrypted JSON array
    /// </summary>
    public string? BackupCodes { get; set; }

    /// <summary>
    /// Is TOTP enabled for this user
    /// </summary>
    public bool IsTotpEnabled { get; set; } = false;

    /// <summary>
    /// Is backup codes enabled for this user
    /// </summary>
    public bool IsBackupCodesEnabled { get; set; } = false;

    /// <summary>
    /// Last time MFA was tested/verified
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// When MFA was enabled
    /// </summary>
    public DateTime EnabledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When MFA was disabled (if applicable)
    /// </summary>
    public DateTime? DisabledAt { get; set; }

    /// <summary>
    /// Is MFA currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Logs MFA authentication attempts
/// </summary>
public class MFALog
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public User User { get; set; } = null!;

    /// <summary>
    /// Type of MFA used: TOTP, BackupCode, SMS, Email, etc.
    /// </summary>
    [Required]
    public string MFAMethod { get; set; } = "TOTP";

    /// <summary>
    /// Was the MFA attempt successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Reason for failure if applicable
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// IP Address from which the attempt was made
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent / device information
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device fingerprint for tracking
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tracks trusted devices to reduce MFA prompts
/// </summary>
public class TrustedDevice
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public User User { get; set; } = null!;

    /// <summary>
    /// Device fingerprint hash
    /// </summary>
    [Required]
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name given by user
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Device type (Desktop, Mobile, Tablet)
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// IP Address of the device
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Is this device currently trusted
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
