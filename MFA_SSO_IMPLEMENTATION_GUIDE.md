# Multi-Factor Authentication (MFA) & Single Sign-On (SSO) Implementation Guide

## Overview

This guide covers the complete end-to-end implementation of Multi-Factor Authentication (MFA) and Single Sign-On (SSO) for the DCL Banking System. Both features are production-ready with enterprise-grade security.

## Table of Contents

1. [Backend Setup](#backend-setup)
2. [Frontend Setup](#frontend-setup)
3. [Database Migrations](#database-migrations)
4. [Configuration](#configuration)
5. [API Endpoints](#api-endpoints)
6. [Frontend Components](#frontend-components)
7. [Testing](#testing)
8. [Security Best Practices](#security-best-practices)

---

## Backend Setup

### 1. Database Models

The following models have been created in `Models/`:

- **MFASetup.cs** - Stores user MFA configuration
- **MFALog.cs** - Logs MFA authentication attempts
- **TrustedDevice.cs** - Manages trusted devices for users
- **SSOProvider.cs** - Stores SSO provider configurations
- **SSOConnection.cs** - Tracks user connections to SSO providers
- **SSOLog.cs** - Logs SSO authentication attempts

### 2. Services

Two new services have been created:

#### MFAService (Services/MFAService.cs)

Handles all MFA operations:
- TOTP secret generation and verification
- Backup code generation and verification
- MFA setup and verification
- MFA disabling
- Trusted device management
- MFA logging

**Key Methods:**
- `SetupMFAAsync()` - Generates TOTP secret and QR code
- `VerifyAndEnableMFAAsync()` - Verifies TOTP and enables MFA
- `VerifyTotpCodeAsync()` - Verifies TOTP codes during login
- `DisableMFAAsync()` - Disables MFA for a user
- `AddTrustedDeviceAsync()` - Marks a device as trusted

#### SSOService (Services/SSOService.cs)

Handles all SSO operations:
- OAuth2/OpenID Connect authorization URL generation
- OAuth2 callback handling
- User creation/linking with SSO providers
- Token refresh
- SSO logging

**Key Methods:**
- `GenerateAuthorizationUrlAsync()` - Creates OAuth authorization URL
- `HandleCallbackAsync()` - Processes OAuth callback and authenticates user
- `LinkAccountAsync()` - Links SSO account to existing user
- `UnlinkAccountAsync()` - Unlinks SSO account from user

### 3. Updated AuthController (Controllers/AuthController.cs)

New endpoints added:

#### MFA Endpoints
- `POST /api/admin/auth/mfa/setup` - Start MFA setup
- `POST /api/admin/auth/mfa/verify-setup` - Verify MFA setup with TOTP
- `POST /api/admin/auth/mfa/disable` - Disable MFA
- `POST /api/admin/auth/mfa/backup-codes` - Generate backup codes
- `GET /api/admin/auth/mfa/status` - Get MFA status
- `POST /api/admin/auth/mfa/trust-device` - Mark device as trusted

#### SSO Endpoints
- `GET /api/admin/auth/sso/providers` - Get available SSO providers
- `POST /api/admin/auth/sso/authorize` - Initialize SSO login
- `POST /api/admin/auth/sso/callback` - Handle SSO callback
- `POST /api/admin/auth/sso/link` - Link SSO account
- `POST /api/admin/auth/sso/unlink` - Unlink SSO account
- `GET /api/admin/auth/sso/connections` - Get user's SSO connections

#### Updated Login Endpoint
The login endpoint now supports:
- MFA verification during login
- Returns `isMFARequired` and `mfaSessionToken` when MFA is needed

---

## Frontend Setup

### 1. API Hooks

New RTK Query API files:

#### mfaApi.js
```javascript
useSetupMFAMutation()          // POST setup
useVerifyMFASetupMutation()    // POST verify
useDisableMFAMutation()        // POST disable
useGenerateBackupCodesMutation() // POST backup codes
useGetMFAStatusQuery()         // GET status
useTrustDeviceMutation()       // POST trust device
```

#### ssoApi.js
```javascript
useGetSSOProvidersQuery()      // GET providers
useInitializeSSOLoginMutation() // POST authorize
useHandleSSOCallbackMutation() // POST callback
useLinkSSOAccountMutation()    // POST link
useUnlinkSSOAccountMutation()  // POST unlink
useGetSSOConnectionsQuery()    // GET connections
```

### 2. Frontend Components

#### MFA Components

1. **MFASetup.jsx**
   - Displays TOTP secret and QR code
   - Allows user to verify TOTP code
   - Shows and manages backup codes
   - CSS: MFASetup.css

2. **MFAVerification.jsx**
   - Login-time MFA verification form
   - Supports TOTP and backup code methods
   - Session expiry countdown
   - CSS: MFAVerification.css

3. **MFAManagement.jsx**
   - Displays MFA status
   - Shows trusted devices
   - Allows regenerating backup codes
   - Allows disabling MFA
   - CSS: MFAManagement.css

#### SSO Components

1. **SSOLogin.jsx**
   - Displays available SSO providers
   - Initiates SSO login flow
   - CSS: SSOLogin.css

2. **SSOManagement.jsx**
   - Shows linked SSO accounts
   - Allows unlinking accounts
   - Shows available providers to link
   - CSS: SSOManagement.css

---

## Database Migrations

### Create Initial Migration

```bash
cd dclcsharp
dotnet ef migrations add AddMFAAndSSO
dotnet ef database update
```

This creates tables for:
- MFASetups
- MFALogs
- TrustedDevices
- SSOProviders
- SSOConnections
- SSOLogs

And adds columns to Users table:
- IsMFAEnabled
- IsMFARequired

---

## Configuration

### appsettings.json

Add the following configuration sections:

```json
{
  "MFASettings": {
    "Enabled": true,
    "Required": false,
    "TOTPIssuer": "NCBA DCL Banking System",
    "BackupCodeCount": 10,
    "BackupCodeLength": 8,
    "TrustedDeviceDays": 30
  },
  "SSOSettings": {
    "Enabled": true,
    "Providers": [
      {
        "Name": "Google",
        "DisplayName": "Sign in with Google",
        "ProviderType": "OAuth2",
        "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
        "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
        "Authority": "https://accounts.google.com",
        "AuthorizationEndpoint": "https://accounts.google.com/o/oauth2/v2/auth",
        "TokenEndpoint": "https://oauth2.googleapis.com/token",
        "UserInfoEndpoint": "https://openidconnect.googleapis.com/v1/userinfo",
        "Scopes": "openid profile email",
        "IconUrl": "https://www.google.com/favicon.ico",
        "DisplayOrder": 1,
        "Enabled": false
      },
      {
        "Name": "Microsoft",
        "DisplayName": "Sign in with Microsoft",
        "ProviderType": "OpenIDConnect",
        "ClientId": "YOUR_MICROSOFT_CLIENT_ID",
        "ClientSecret": "YOUR_MICROSOFT_CLIENT_SECRET",
        "Authority": "https://login.microsoftonline.com/common/v2.0",
        "AuthorizationEndpoint": "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
        "TokenEndpoint": "https://login.microsoftonline.com/common/oauth2/v2.0/token",
        "UserInfoEndpoint": "https://graph.microsoft.com/v1.0/me",
        "Scopes": "openid profile email",
        "IconUrl": "https://www.microsoft.com/favicon.ico",
        "DisplayOrder": 2,
        "Enabled": false
      }
    ]
  }
}
```

### Program.cs

Services are already registered:
```csharp
builder.Services.AddScoped<IMFAService, MFAService>();
builder.Services.AddScoped<ISSOService, SSOService>();
builder.Services.AddHttpClient();
```

---

## API Endpoints

### MFA API Endpoints

#### Setup MFA
```
POST /api/admin/auth/mfa/setup
Authorization: Bearer <token>

Response:
{
  "qrCodeUrl": "otpauth://totp/...",
  "secret": "XXXXXXXXXXXXXXXX",
  "sessionToken": "base64_encoded_token",
  "instructions": "Use an authenticator app..."
}
```

#### Verify MFA Setup
```
POST /api/admin/auth/mfa/verify-setup
Authorization: Bearer <token>
Content-Type: application/json

{
  "totpCode": "123456",
  "sessionToken": "base64_encoded_token"
}

Response:
{
  "isVerified": true,
  "backupCodes": ["ABCD1234", "EFGH5678", ...],
  "message": "MFA has been successfully enabled..."
}
```

#### Login with MFA
```
POST /api/admin/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password",
  "mfaToken": "123456"  // Optional, required if MFA is enabled
}

Response (MFA Required):
{
  "isMFARequired": true,
  "mfaSessionToken": "base64_encoded_token",
  "token": null,
  "user": null
}

Response (Success):
{
  "isMFARequired": false,
  "token": "jwt_token",
  "user": {
    "id": "guid",
    "name": "User Name",
    "email": "user@example.com",
    "role": "Admin",
    "isMFAEnabled": true
  },
  "mfaSessionToken": null
}
```

### SSO API Endpoints

#### Get SSO Providers
```
GET /api/admin/auth/sso/providers

Response:
{
  "availableProviders": [
    {
      "id": "guid",
      "providerName": "Google",
      "displayName": "Sign in with Google",
      "providerType": "OAuth2",
      "isEnabled": true,
      "iconUrl": "https://...",
      "displayOrder": 1
    }
  ],
  "linkedAccounts": []
}
```

#### Initialize SSO Login
```
POST /api/admin/auth/sso/authorize
Content-Type: application/json

{
  "ssoProviderId": "guid",
  "redirectUri": "https://yourapp.com/auth/sso/callback"
}

Response:
{
  "authorizationUrl": "https://provider.com/authorize?...",
  "state": "random_state",
  "codeChallenge": "pkce_challenge"
}
```

---

## Frontend Components

### Using MFA Setup

```jsx
import MFASetup from "./components/MFASetup";

function SettingsPage() {
  const [showMFASetup, setShowMFASetup] = useState(false);

  return (
    <>
      <button onClick={() => setShowMFASetup(true)}>
        Enable MFA
      </button>

      {showMFASetup && (
        <MFASetup
          onSuccess={() => {
            setShowMFASetup(false);
            // Refresh user data
          }}
          onCancel={() => setShowMFASetup(false)}
        />
      )}
    </>
  );
}
```

### Using MFA Verification

```jsx
import MFAVerification from "./components/MFAVerification";
import { useLoginMutation } from "./api/authApi";

function LoginPage() {
  const [step, setStep] = useState("login"); // login, mfa
  const [mfaSessionToken, setMFASessionToken] = useState("");
  const [login, { isLoading }] = useLoginMutation();

  const handleLogin = async (email, password) => {
    const result = await login({ email, password }).unwrap();
    
    if (result.isMFARequired) {
      setMFASessionToken(result.mfaSessionToken);
      setStep("mfa");
    } else {
      // Login successful
      dispatch(setCredentials(result));
      navigate("/admin");
    }
  };

  const handleMFAVerify = async (mfaToken) => {
    const result = await login({
      email,
      password,
      mfaToken,
      sessionToken: mfaSessionToken
    }).unwrap();
    
    dispatch(setCredentials(result));
    navigate("/admin");
  };

  return step === "mfa" ? (
    <MFAVerification
      mfaSessionToken={mfaSessionToken}
      onVerify={handleMFAVerify}
      onBack={() => setStep("login")}
      isLoading={isLoading}
    />
  ) : (
    <LoginForm onSubmit={handleLogin} />
  );
}
```

### Using SSO Login

```jsx
import SSOLogin from "./components/SSOLogin";

function LoginPage() {
  return (
    <div className="login-form">
      {/* Email/password login */}
      <form onSubmit={handleLogin}>
        {/* ... */}
      </form>

      {/* SSO Options */}
      <SSOLogin onSuccess={() => navigate("/admin")} />
    </div>
  );
}
```

### Using MFA Management

```jsx
import MFAManagement from "./components/MFAManagement";

function SettingsPage() {
  return (
    <div className="settings">
      <MFAManagement />
    </div>
  );
}
```

---

## Testing

### Backend Testing

1. **Setup MFA**
   ```bash
   curl -X POST http://localhost:5000/api/admin/auth/mfa/setup \
     -H "Authorization: Bearer {token}"
   ```

2. **Login with MFA**
   ```bash
   curl -X POST http://localhost:5000/api/admin/auth/login \
     -H "Content-Type: application/json" \
     -d '{
       "email": "user@example.com",
       "password": "password",
       "mfaToken": "123456"
     }'
   ```

3. **Test Backup Codes**
   - Disable TOTP temporarily
   - Use backup codes to login
   - Verify they work and are consumed

### Frontend Testing

1. **Unit Tests**
   ```bash
   npm test
   ```

2. **Integration Tests**
   - Test MFA flow end-to-end
   - Test SSO flow with mock provider
   - Test device trust functionality

3. **Manual Testing**
   - Use Google Authenticator, Authy, or Microsoft Authenticator
   - Test QR code scanning
   - Test backup code entry
   - Test device trust

---

## Security Best Practices

### 1. TOTP Security
- ✅ Using HMAC-SHA1 with 30-second time step
- ✅ Verifying within ±1 time window for clock skew
- ✅ Storing encrypted TOTP secrets in database
- ✅ Generating cryptographically secure backup codes

### 2. Session Management
- ✅ MFA session tokens expire after 15 minutes
- ✅ Login requires both password AND MFA token when enabled
- ✅ Failed MFA attempts are logged

### 3. Trusted Devices
- ✅ Device fingerprints are stored securely
- ✅ Only trusted devices can skip MFA temporarily
- ✅ Admins can manage and revoke trusted devices

### 4. SSO Security
- ✅ Using OAuth2/OIDC standard authorization flow
- ✅ Implementing PKCE (Proof Key for Code Exchange)
- ✅ Storing encrypted access tokens
- ✅ Supporting token refresh

### 5. Audit Logging
- ✅ All MFA events logged
- ✅ All SSO events logged
- ✅ Failed login attempts tracked
- ✅ Device trust events recorded

### 6. Password Security
- ✅ Passwords verified before disabling MFA
- ✅ Passwords verified before unlinking SSO accounts
- ✅ Password hashing using industry-standard algorithms

### 7. Data Protection
- ✅ Encryption of sensitive fields in database
- ✅ HTTPS/TLS for all API communications
- ✅ Secure cookie handling
- ✅ CSRF protection enabled

---

## Troubleshooting

### Common Issues

#### TOTP Code Not Verifying
- Check server and client clock synchronization
- Ensure TOTP secret was saved correctly
- Verify authenticator app is configured with correct issuer

#### SSO Login Fails
- Check OAuth credentials in appsettings.json
- Verify redirect URI matches provider configuration
- Check browser console for CORS errors
- Ensure provider is enabled in configuration

#### MFA Session Token Expires
- Extend MFA session token validity period
- Implement refresh mechanism
- Show countdown timer to user

#### Backup Codes Not Working
- Verify backup codes were saved correctly
- Check database encryption/decryption
- Ensure codes haven't been used already

---

## Next Steps

1. **Deploy to Production**
   - Update appsettings.json with real OAuth credentials
   - Run database migrations
   - Enable MFA/SSO in configuration

2. **User Communication**
   - Notify users about MFA availability
   - Provide setup guides
   - Offer support resources

3. **Admin Management**
   - Create admin panel for MFA enforcement
   - Implement MFA requirement policies
   - Add device management interface

4. **Monitoring**
   - Track MFA adoption rates
   - Monitor failed login attempts
   - Alert on suspicious activity

5. **Additional Providers**
   - Add more SSO providers (GitHub, Facebook, etc.)
   - Implement SAML support
   - Add SMS/Email MFA options

---

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review API logs for errors
3. Check browser console for frontend errors
4. Verify database migrations were applied
5. Confirm configuration in appsettings.json
