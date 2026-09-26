namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Error de la API con un codigo estable, igual que en la web y en Android.
/// Nunca se compara el texto del error para decidir nada: solo el codigo.
/// </summary>
public class ApiException(string code) : Exception(code)
{
    public const string InvalidCredentials = "invalid_credentials";
    public const string SessionExpired = "session_expired";
    public const string NotFound = "not_found";
    public const string CategoryNotFound = "category_not_found";
    public const string CategoryTypeMismatch = "category_type_mismatch";
    public const string CategoryHasTransactions = "category_has_transactions";
    public const string NetworkError = "network_error";
    public const string Unknown = "unknown";

    public string Code { get; } = code;
}
