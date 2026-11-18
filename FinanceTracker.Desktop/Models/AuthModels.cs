namespace FinanceTracker.Desktop.Models;

// Copias de los DTOs de la API (FinanceTracker.Application/DTOs/Users).
// System.Text.Json las rellena sin distinguir mayusculas: "token" -> Token.

/// <summary>TrustedDeviceToken: dispositivo de confianza del MFA, si lo hay.</summary>
public record LoginRequest(string Email, string Password, string? TrustedDeviceToken = null);

/// <summary>Cuerpo de POST /api/Users/register (RegisterUserDto).</summary>
public record RegisterRequest(string Name, string Email, string Password, string Language);

/// <summary>Cuerpo de POST /api/Users/google (GoogleLoginDto).</summary>
public record GoogleLoginRequest(string IdToken, string Language, string? TrustedDeviceToken = null);

/// <summary>MfaEnabled: si la cuenta tiene verificacion en dos pasos.</summary>
public record UserInfo(int Id, string Name, string Email, bool MfaEnabled = false);

/// <summary>
/// Respuesta del login. Con MFA activo no trae Token ni User: trae
/// MfaRequired y un MfaToken temporal para POST /api/users/mfa/verify.
/// </summary>
public record LoginResponse(
    string Token,
    DateTime Expiration,
    UserInfo User,
    bool MfaRequired = false,
    string? MfaToken = null,
    string? TrustedDeviceToken = null);

/// <summary>Cuerpo de POST /api/users/mfa/verify: codigo TOTP o de recuperacion.</summary>
/// <summary>RememberDevice: "no volver a pedir el codigo en este dispositivo durante 30 dias".</summary>
public record MfaVerifyRequest(string MfaToken, string Code, bool RememberDevice = false);

/// <summary>Cuerpo de confirm y disable: los 6 digitos de la app autenticadora.</summary>
public record MfaCodeRequest(string Code);

/// <summary>Respuesta de POST /api/users/mfa/setup.</summary>
public record MfaSetupResponse(string Secret, string OtpAuthUri);

/// <summary>Respuesta de POST /api/users/mfa/confirm. Se muestran una sola vez.</summary>
public record MfaRecoveryCodesResponse(List<string> RecoveryCodes);

/// <summary>Cuerpo de error de la API (ProblemDetails). Solo nos interesa el codigo.</summary>
public record ApiProblem(string? Code, string? Detail);
