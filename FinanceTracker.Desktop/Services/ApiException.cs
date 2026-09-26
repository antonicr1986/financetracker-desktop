namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Error de la API con un codigo estable, igual que en la web y en Android.
/// Nunca se compara el texto del error para decidir nada: solo el codigo.
/// </summary>
public class ApiException(string code) : Exception(code)
{
    public const string InvalidCredentials = "invalid_credentials";
    public const string NetworkError = "network_error";
    public const string Unknown = "unknown";

    public string Code { get; } = code;
}
