using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>
/// El LoginViewModel depende de esta interfaz, no de la clase real. Asi, en los
/// tests se le pasa una version falsa y no hace falta la API ni internet.
/// </summary>
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password);

    /// <summary>
    /// Crea la cuenta. La API devuelve el usuario creado, no un token: quien
    /// llama tiene que iniciar sesion despues con las mismas credenciales.
    /// </summary>
    Task<UserInfo> RegisterAsync(string name, string email, string password, string language);

    /// <summary>
    /// Entra con el ID token que da Google. Misma respuesta que el login; si no
    /// existe la cuenta, la API la crea con las categorias en ese idioma.
    /// </summary>
    Task<LoginResponse> GoogleLoginAsync(string idToken, string language);
}
