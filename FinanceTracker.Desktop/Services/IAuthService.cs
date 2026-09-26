using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>
/// El LoginViewModel depende de esta interfaz, no de la clase real. Asi, en los
/// tests se le pasa una version falsa y no hace falta la API ni internet.
/// </summary>
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password);
}
