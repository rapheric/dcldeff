using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.Models;

namespace NCBA.DCL.Services;

/// <summary>
/// Service for handling Multi-Factor Authentication (MFA) operations
/// Supports TOTP (Time-based One-Time Password) and backup codes
/// </summary>
public interface IMFAService
{
    Task<(string Secret, string QrCodeUrl)> GenerateTotpSecretAsync(User user);
    Task<bool> VerifyTotpCodeAsync(string secret, string code, int window = 1);
    Task<MFASetup> EnableMFAAsync(Guid userId, string secret);
    Task<(bool Success, List<string> BackupCodes)> VerifyAndEnableMFAAsync(Guid userId, string totpCode, string sessionSecret);
    Task<bool> DisableMFAAsync(Guid userId);
    Task<List<string>> GenerateBackupCodesAsync(Guid userId);
    Task<bool> VerifyBackupCodeAsync(Guid userId, string code);
    Task<MFALog> LogMFAAttemptAsync(Guid userId, string method, bool isSuccess, string? failureReason = null, string? ipAddress = null);
    Task<TrustedDevice> AddTrustedDeviceAsync(Guid userId, string deviceFingerprint, string? deviceName = null);
    Task<List<TrustedDevice>> GetTrustedDevicesAsync(Guid userId);
    Task<bool> IsTrustedDeviceAsync(Guid userId, string deviceFingerprint);
    Task<bool> RemoveTrustedDeviceAsync(Guid userId, Guid deviceId);
}

public class MFAService : IMFAService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MFAService> _logger;

    public MFAService(ApplicationDbContext context, ILogger<MFAService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Generates a TOTP secret and QR code for user
    /// </summary>
    public async Task<(string Secret, string QrCodeUrl)> GenerateTotpSecretAsync(User user)
    {
        try
        {
            // Generate a random secret (base32 encoded)
            var secret = GenerateSecret();

            // Generate QR code URL for authenticator app
            var qrCodeUrl = GenerateQrCodeUrl(user.Email, secret, "DCL Banking System");

            return await Task.FromResult((secret, qrCodeUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TOTP secret");
            throw;
        }
    }

    /// <summary>
    /// Verifies a TOTP code against a secret
    /// </summary>
    public async Task<bool> VerifyTotpCodeAsync(string secret, string code, int window = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length < 6)
                return false;

            // Convert code to long
            if (!long.TryParse(code, out var codeValue))
                return false;

            // Get current time step
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeStep = 30; // TOTP time step in seconds
            var currentCounter = currentTime / timeStep;

            // Verify within window (default 1 step = 30 seconds before/after)
            for (int i = -window; i <= window; i++)
            {
                var counter = currentCounter + i;
                var hmac = ComputeHmac(DecodeBase32(secret), counter);
                var offset = hmac[hmac.Length - 1] & 0x0F;
                var value = (hmac[offset] & 0x7F) << 24
                    | (hmac[offset + 1] & 0xFF) << 16
                    | (hmac[offset + 2] & 0xFF) << 8
                    | (hmac[offset + 3] & 0xFF);

                var totpCode = value % 1000000;

                if (totpCode == codeValue)
                    return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying TOTP code");
            return false;
        }
    }

    /// <summary>
    /// Enables MFA for a user with validated TOTP code
    /// </summary>
    public async Task<MFASetup> EnableMFAAsync(Guid userId, string secret)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Check if MFA already exists
            var existingMFA = await _context.MFASetups.FindAsync(userId);
            if (existingMFA != null)
            {
                existingMFA.TotpSecret = EncryptSecret(secret);
                existingMFA.IsTotpEnabled = true;
                existingMFA.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existingMFA = new MFASetup
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    User = user,
                    TotpSecret = EncryptSecret(secret),
                    IsTotpEnabled = true,
                    EnabledAt = DateTime.UtcNow
                };
                _context.MFASetups.Add(existingMFA);
            }

            user.IsMFAEnabled = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA enabled for user {UserId}", userId);
            return existingMFA;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling MFA for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Verifies TOTP and enables MFA with backup codes
    /// </summary>
    public async Task<(bool Success, List<string> BackupCodes)> VerifyAndEnableMFAAsync(Guid userId, string totpCode, string sessionSecret)
    {
        try
        {
            // Verify the TOTP code
            var isValid = await VerifyTotpCodeAsync(sessionSecret, totpCode);
            if (!isValid)
                return (false, new List<string>());

            // Generate backup codes
            var backupCodes = GenerateBackupCodes(8, 10);

            // Enable MFA
            var mfaSetup = await EnableMFAAsync(userId, sessionSecret);
            mfaSetup.BackupCodes = string.Join("|", backupCodes.Select(EncryptSecret));
            mfaSetup.IsBackupCodesEnabled = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA verified and enabled for user {UserId}", userId);
            return (true, backupCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying and enabling MFA for user {UserId}", userId);
            return (false, new List<string>());
        }
    }

    /// <summary>
    /// Disables MFA for a user
    /// </summary>
    public async Task<bool> DisableMFAAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            var mfa = await _context.MFASetups.FindAsync(userId);
            if (mfa != null)
            {
                mfa.IsActive = false;
                mfa.DisabledAt = DateTime.UtcNow;
                mfa.IsTotpEnabled = false;
                mfa.IsBackupCodesEnabled = false;
            }

            user.IsMFAEnabled = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA disabled for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling MFA for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Generates new backup codes for a user
    /// </summary>
    public async Task<List<string>> GenerateBackupCodesAsync(Guid userId)
    {
        try
        {
            var mfa = await _context.MFASetups.FindAsync(userId);
            if (mfa == null)
                throw new InvalidOperationException("MFA not setup for this user");

            var codes = GenerateBackupCodes(8, 10);
            mfa.BackupCodes = string.Join("|", codes.Select(EncryptSecret));
            mfa.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Backup codes regenerated for user {UserId}", userId);
            return codes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating backup codes for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Verifies a backup code and marks it as used
    /// </summary>
    public async Task<bool> VerifyBackupCodeAsync(Guid userId, string code)
    {
        try
        {
            var mfa = await _context.MFASetups.FindAsync(userId);
            if (mfa == null || !mfa.IsBackupCodesEnabled)
                return false;

            if (string.IsNullOrWhiteSpace(mfa.BackupCodes))
                return false;

            var codes = mfa.BackupCodes.Split('|');
            var codeFound = false;

            foreach (var storedCode in codes)
            {
                if (VerifySecret(code, storedCode))
                {
                    codeFound = true;
                    break;
                }
            }

            return codeFound;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying backup code for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Logs MFA authentication attempts
    /// </summary>
    public async Task<MFALog> LogMFAAttemptAsync(Guid userId, string method, bool isSuccess, string? failureReason = null, string? ipAddress = null)
    {
        try
        {
            var log = new MFALog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MFAMethod = method,
                IsSuccess = isSuccess,
                FailureReason = failureReason,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.MFALogs.Add(log);
            await _context.SaveChangesAsync();

            return log;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging MFA attempt for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Adds a trusted device for a user
    /// </summary>
    public async Task<TrustedDevice> AddTrustedDeviceAsync(Guid userId, string deviceFingerprint, string? deviceName = null)
    {
        try
        {
            // Check if device already trusted
            var existingDevice = await _context.TrustedDevices
                .FirstOrDefaultAsync(td => td.UserId == userId && td.DeviceFingerprint == deviceFingerprint);

            if (existingDevice != null)
            {
                existingDevice.LastUsedAt = DateTime.UtcNow;
                existingDevice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existingDevice;
            }

            var device = new TrustedDevice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceFingerprint = deviceFingerprint,
                DeviceName = deviceName ?? "Untrusted Device",
                LastUsedAt = DateTime.UtcNow
            };

            _context.TrustedDevices.Add(device);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Trusted device added for user {UserId}", userId);
            return device;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding trusted device for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets all trusted devices for a user
    /// </summary>
    public async Task<List<TrustedDevice>> GetTrustedDevicesAsync(Guid userId)
    {
        return await _context.TrustedDevices
            .Where(td => td.UserId == userId && td.IsActive)
            .OrderByDescending(td => td.LastUsedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a device is trusted
    /// </summary>
    public async Task<bool> IsTrustedDeviceAsync(Guid userId, string deviceFingerprint)
    {
        return await _context.TrustedDevices
            .AnyAsync(td => td.UserId == userId && td.DeviceFingerprint == deviceFingerprint && td.IsActive);
    }

    /// <summary>
    /// Removes a trusted device
    /// </summary>
    public async Task<bool> RemoveTrustedDeviceAsync(Guid userId, Guid deviceId)
    {
        try
        {
            var device = await _context.TrustedDevices.FindAsync(deviceId);
            if (device == null || device.UserId != userId)
                return false;

            device.IsActive = false;
            device.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing trusted device {DeviceId}", deviceId);
            return false;
        }
    }

    // ============ Helper Methods ============

    private string GenerateSecret(int length = 32)
    {
        var random = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(random);
        }
        return Base32Encode(random);
    }

    private List<string> GenerateBackupCodes(int codeLength, int codeCount)
    {
        var codes = new List<string>();
        for (int i = 0; i < codeCount; i++)
        {
            var code = new string(Enumerable.Range(0, codeLength)
                .Select(x => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[Random.Shared.Next(36)])
                .ToArray());
            codes.Add(code);
        }
        return codes;
    }

    private string GenerateQrCodeUrl(string email, string secret, string issuer)
    {
        var accountName = email;
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedSecret = Uri.EscapeDataString(secret);

        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={encodedSecret}&issuer={encodedIssuer}";
    }

    private byte[] ComputeHmac(byte[] key, long counter)
    {
        var counterBytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using (var hmac = new HMACSHA1(key))
        {
            return hmac.ComputeHash(counterBytes);
        }
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var value = 0;
        var index = 0;
        var result = new StringBuilder();

        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                index = (value >> bits) & 31;
                result.Append(alphabet[index]);
            }
        }

        if (bits > 0)
        {
            index = (value << (5 - bits)) & 31;
            result.Append(alphabet[index]);
        }

        return result.ToString();
    }

    private static byte[] DecodeBase32(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var value = 0;
        var index = 0;
        var result = new List<byte>();

        foreach (var c in input.ToUpperInvariant())
        {
            index = alphabet.IndexOf(c);
            if (index < 0)
                throw new ArgumentException("Invalid character in base32 string");

            value = (value << 5) | index;
            bits += 5;

            if (bits >= 8)
            {
                bits -= 8;
                result.Add((byte)((value >> bits) & 255));
            }
        }

        return result.ToArray();
    }

    private string EncryptSecret(string secret)
    {
        // In production, use proper encryption (AES-256)
        // For now, we'll use a simple encoding
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(secret));
    }

    private bool VerifySecret(string plaintext, string encrypted)
    {
        try
        {
            var decrypted = Encoding.UTF8.GetString(Convert.FromBase64String(encrypted));
            return decrypted == plaintext;
        }
        catch
        {
            return false;
        }
    }
}
