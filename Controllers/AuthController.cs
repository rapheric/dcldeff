using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCBA.DCL.Data;
using NCBA.DCL.DTOs;
using NCBA.DCL.Helpers;
using NCBA.DCL.Models;
using NCBA.DCL.Services;
using System.Security.Claims;

namespace NCBA.DCL.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IMFAService _mfaService;
    private readonly ISSOService _ssoService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        JwtTokenGenerator tokenGenerator,
        IMFAService mfaService,
        ISSOService ssoService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _mfaService = mfaService;
        _ssoService = ssoService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request)
    {
        try
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
            {
                return BadRequest(new { message = "Admin already exists" });
            }

            var admin = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email,
                Password = PasswordHasher.HashPassword(request.Password),
                Role = UserRole.Admin,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Admin registered successfully",
                user = new UserResponse
                {
                    Id = admin.Id,
                    Name = admin.Name,
                    Email = admin.Email,
                    Role = admin.Role.ToString(),
                    IsMFAEnabled = false
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering admin");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.MFASetup)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!user.Active)
            {
                return StatusCode(403, new { message = "Account deactivated" });
            }

            if (!PasswordHasher.VerifyPassword(request.Password, user.Password))
            {
                await _mfaService.LogMFAAttemptAsync(user.Id, "PASSWORD", false, "Invalid password", GetClientIp());
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Check if MFA is enabled and required
            if (user.IsMFAEnabled && user.IsMFARequired && string.IsNullOrWhiteSpace(request.MFAToken))
            {
                // Generate MFA session token
                var mfaSessionToken = GenerateMFASessionToken(user.Id);

                return Ok(new LoginResponse
                {
                    IsMFARequired = true,
                    MFASessionToken = mfaSessionToken,
                    User = null,
                    Token = null
                });
            }

            // If MFA token is provided, verify it
            if (!string.IsNullOrWhiteSpace(request.MFAToken))
            {
                var isValidTotp = await _mfaService.VerifyTotpCodeAsync(user.MFASetup?.TotpSecret ?? "", request.MFAToken);
                var isValidBackupCode = await _mfaService.VerifyBackupCodeAsync(user.Id, request.MFAToken);

                if (!isValidTotp && !isValidBackupCode)
                {
                    await _mfaService.LogMFAAttemptAsync(user.Id, "TOTP", false, "Invalid MFA token", GetClientIp());
                    return Unauthorized(new { message = "Invalid MFA token" });
                }

                await _mfaService.LogMFAAttemptAsync(user.Id, "TOTP", true, null, GetClientIp());
            }

            var token = _tokenGenerator.GenerateToken(user);

            // Log login activity
            var log = new UserLog
            {
                Id = Guid.NewGuid(),
                Action = "LOGIN",
                TargetUserId = user.Id,
                TargetEmail = user.Email,
                PerformedById = user.Id,
                PerformedByEmail = user.Email,
                Timestamp = DateTime.UtcNow
            };
            _context.UserLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                Token = token,
                IsMFARequired = false,
                User = new UserResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    IsMFAEnabled = user.IsMFAEnabled
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ==================== MFA Endpoints ====================

    [Authorize]
    [HttpPost("mfa/setup")]
    public async Task<IActionResult> SetupMFA([FromBody] SetupMFARequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Generate TOTP secret
            var (secret, qrCodeUrl) = await _mfaService.GenerateTotpSecretAsync(user);

            // Generate session token for verification
            var sessionToken = GenerateMFASessionToken(userId);

            return Ok(new SetupMFAResponse
            {
                QRCodeUrl = qrCodeUrl,
                Secret = secret,
                SessionToken = sessionToken,
                Instructions = "Use an authenticator app (Google Authenticator, Authy, Microsoft Authenticator) to scan the QR code or enter the secret manually."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up MFA");
            return StatusCode(500, new { message = "Error setting up MFA" });
        }
    }

    [Authorize]
    [HttpPost("mfa/verify-setup")]
    public async Task<IActionResult> VerifyMFASetup([FromBody] VerifyMFASetupRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify the TOTP code against the session token
            var (isValid, backupCodes) = await _mfaService.VerifyAndEnableMFAAsync(userId, request.TOTPCode, request.SessionToken);

            if (!isValid)
            {
                await _mfaService.LogMFAAttemptAsync(userId, "SETUP", false, "Invalid TOTP code", GetClientIp());
                return BadRequest(new { message = "Invalid TOTP code. Please try again." });
            }

            await _mfaService.LogMFAAttemptAsync(userId, "SETUP", true, null, GetClientIp());

            return Ok(new VerifyMFAResponse
            {
                IsVerified = true,
                BackupCodes = backupCodes,
                Message = "MFA has been successfully enabled. Save your backup codes in a secure location."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying MFA setup");
            return StatusCode(500, new { message = "Error verifying MFA setup" });
        }
    }

    [Authorize]
    [HttpPost("mfa/disable")]
    public async Task<IActionResult> DisableMFA([FromBody] DisableMFARequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Verify password before disabling MFA
            if (!PasswordHasher.VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized(new { message = "Invalid password" });
            }

            var success = await _mfaService.DisableMFAAsync(userId);

            if (!success)
                return BadRequest(new { message = "Failed to disable MFA" });

            return Ok(new { message = "MFA has been disabled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling MFA");
            return StatusCode(500, new { message = "Error disabling MFA" });
        }
    }

    [Authorize]
    [HttpPost("mfa/backup-codes")]
    public async Task<IActionResult> GenerateBackupCodes([FromBody] GenerateBackupCodesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var codes = await _mfaService.GenerateBackupCodesAsync(userId);

            return Ok(new GenerateBackupCodesResponse { BackupCodes = codes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating backup codes");
            return StatusCode(500, new { message = "Error generating backup codes" });
        }
    }

    [Authorize]
    [HttpGet("mfa/status")]
    public async Task<IActionResult> GetMFAStatus()
    {
        try
        {
            var userId = GetCurrentUserId();
            var mfaSetup = await _context.MFASetups.FindAsync(userId);
            var trustedDevices = await _mfaService.GetTrustedDevicesAsync(userId);

            return Ok(new MFAStatusResponse
            {
                IsMFAEnabled = mfaSetup?.IsActive ?? false,
                IsTotpEnabled = mfaSetup?.IsTotpEnabled ?? false,
                IsBackupCodesEnabled = mfaSetup?.IsBackupCodesEnabled ?? false,
                EnabledAt = mfaSetup?.EnabledAt,
                LastTestedAt = mfaSetup?.LastTestedAt,
                TrustedDevices = trustedDevices.Select(td => new TrustedDeviceResponse
                {
                    Id = td.Id,
                    DeviceName = td.DeviceName ?? "Unknown Device",
                    DeviceType = td.DeviceType ?? "Unknown",
                    LastUsedAt = td.LastUsedAt,
                    IsActive = td.IsActive
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MFA status");
            return StatusCode(500, new { message = "Error getting MFA status" });
        }
    }

    [Authorize]
    [HttpPost("mfa/trust-device")]
    public async Task<IActionResult> TrustDevice([FromBody] TrustedDeviceRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var device = await _mfaService.AddTrustedDeviceAsync(userId, request.DeviceFingerprint, request.DeviceName);

            return Ok(new TrustedDeviceResponse
            {
                Id = device.Id,
                DeviceName = device.DeviceName ?? "Unknown",
                DeviceType = device.DeviceType ?? "Unknown",
                LastUsedAt = device.LastUsedAt,
                IsActive = device.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trusting device");
            return StatusCode(500, new { message = "Error trusting device" });
        }
    }

    // ==================== SSO Endpoints ====================

    [HttpGet("sso/providers")]
    public async Task<IActionResult> GetSSOProviders()
    {
        try
        {
            var providers = await _ssoService.GetEnabledProvidersAsync();

            return Ok(new SSOStatusResponse
            {
                AvailableProviders = providers.Select(p => new SSOProviderSetupDto
                {
                    Id = p.Id,
                    ProviderName = p.ProviderName,
                    DisplayName = p.DisplayName,
                    ProviderType = p.ProviderType,
                    IsEnabled = p.IsEnabled,
                    IconUrl = p.IconUrl,
                    DisplayOrder = p.DisplayOrder
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SSO providers");
            return StatusCode(500, new { message = "Error retrieving SSO providers" });
        }
    }

    [HttpPost("sso/authorize")]
    public async Task<IActionResult> InitializeSSOLogin([FromBody] SSOLoginInitializeRequest request)
    {
        try
        {
            var provider = await _context.SSOProviders.FindAsync(request.SSOProviderId);
            if (provider == null || !provider.IsEnabled)
                return NotFound(new { message = "SSO provider not found or disabled" });

            var authUrl = await _ssoService.GenerateAuthorizationUrlAsync(
                request.SSOProviderId,
                request.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/auth/sso/callback",
                request.State
            );

            return Ok(new SSOLoginInitializeResponse { AuthorizationUrl = authUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SSO login");
            return StatusCode(500, new { message = "Error with SSO initialization" });
        }
    }

    [HttpPost("sso/callback")]
    public async Task<IActionResult> HandleSSOCallback([FromBody] SSOCallbackRequest request)
    {
        try
        {
            var (success, user, isNewUser, message) = await _ssoService.HandleCallbackAsync(
                request.SSOProviderId,
                request.Code,
                request.State
            );

            if (!success)
                return BadRequest(new { message });

            var token = _tokenGenerator.GenerateToken(user);

            return Ok(new SSOCallbackResponse
            {
                Token = token,
                IsNewUser = isNewUser,
                User = new UserResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    IsMFAEnabled = user.IsMFAEnabled
                },
                Message = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SSO callback");
            return StatusCode(500, new { message = "Error processing SSO login" });
        }
    }

    [Authorize]
    [HttpPost("sso/link")]
    public async Task<IActionResult> LinkSSOAccount([FromBody] LinkSSOAccountRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var provider = await _context.SSOProviders.FindAsync(request.SSOProviderId);

            if (provider == null)
                return NotFound(new { message = "SSO provider not found" });

            // Handle SSO callback to get user info, then link
            var (success, user, isNewUser, message) = await _ssoService.HandleCallbackAsync(
                request.SSOProviderId,
                request.Code
            );

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message = "SSO account linked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking SSO account");
            return StatusCode(500, new { message = "Error linking SSO account" });
        }
    }

    [Authorize]
    [HttpPost("sso/unlink")]
    public async Task<IActionResult> UnlinkSSOAccount([FromBody] UnlinkSSOAccountRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Verify password
            if (!PasswordHasher.VerifyPassword(request.Password, user.Password))
                return Unauthorized(new { message = "Invalid password" });

            var success = await _ssoService.UnlinkAccountAsync(userId, request.SSOProviderId);

            if (!success)
                return BadRequest(new { message = "Failed to unlink SSO account" });

            return Ok(new { message = "SSO account unlinked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking SSO account");
            return StatusCode(500, new { message = "Error unlinking SSO account" });
        }
    }

    [Authorize]
    [HttpGet("sso/connections")]
    public async Task<IActionResult> GetSSOConnections()
    {
        try
        {
            var userId = GetCurrentUserId();
            var connections = await _ssoService.GetUserConnectionsAsync(userId);

            return Ok(new SSOStatusResponse
            {
                LinkedAccounts = connections.Select(c => new SSOConnectionResponse
                {
                    Id = c.Id,
                    SSOProviderId = c.SSOProviderId,
                    ProviderName = c.Provider.ProviderName,
                    ProviderEmail = c.ProviderEmail ?? "",
                    ConnectedAt = c.CreatedAt
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SSO connections");
            return StatusCode(500, new { message = "Error retrieving SSO connections" });
        }
    }

    // ==================== Helper Methods ====================

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id");
        return Guid.Parse(userIdClaim?.Value ?? Guid.Empty.ToString());
    }

    private string GetClientIp()
    {
        return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string GenerateMFASessionToken(Guid userId)
    {
        // Create a temporary session token that includes user ID and expiry
        var tokenString = $"{userId}:{DateTime.UtcNow.AddMinutes(15).Ticks}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenString));
    }
}
