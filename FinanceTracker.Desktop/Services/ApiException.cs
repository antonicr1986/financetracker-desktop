namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Error de la API con un codigo estable, igual que en la web y en Android.
/// Nunca se compara el texto del error para decidir nada: solo el codigo.
/// </summary>
public class ApiException(string code) : Exception(code)
{
    public const string InvalidCredentials = "invalid_credentials";
    public const string GoogleSignInFailed = "invalid_google_token";
    public const string EmailAlreadyExists = "email_already_exists";
    public const string SessionExpired = "session_expired";
    public const string NotFound = "not_found";
    public const string CategoryNotFound = "category_not_found";
    public const string CategoryTypeMismatch = "category_type_mismatch";
    public const string CategoryHasTransactions = "category_has_transactions";
    public const string NetworkError = "network_error";
    public const string TooManyRequests = "too_many_requests";
    public const string ClientVersionUnsupported = "client_version_unsupported";
    public const string PasswordBreached = "password_breached";
    public const string InvalidMfaCode = "invalid_mfa_code";
    public const string InvalidMfaToken = "invalid_mfa_token";
    public const string MfaAlreadyEnabled = "mfa_already_enabled";
    public const string MfaNotEnabled = "mfa_not_enabled";
    public const string InvalidVerificationToken = "invalid_verification_token";
    public const string ResetCodeExpired = "reset_code_expired";
    public const string Unknown = "unknown";

    public string Code { get; } = code;
}
