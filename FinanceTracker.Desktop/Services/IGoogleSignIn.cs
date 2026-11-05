namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Pide a Google que la persona elija su cuenta y devuelve el ID token. Va
/// detras de una interfaz para que el LoginViewModel se pruebe sin navegador.
/// </summary>
public interface IGoogleSignIn
{
    /// <summary>
    /// Lanza ApiException(GoogleSignInFailed) si se cancela, se deniega, no
    /// termina a tiempo o el ejecutable se compilo sin el secreto de cliente.
    /// </summary>
    Task<string> GetIdTokenAsync();
}
