using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Sesion actual, en memoria. De momento se pierde al cerrar la aplicacion;
/// guardarla en disco sera un paso posterior.
/// </summary>
public class Session
{
    public string? Token { get; private set; }
    public UserInfo? User { get; private set; }

    public bool IsActive => Token is not null;

    public void Start(LoginResponse login)
    {
        Token = login.Token;
        User = login.User;
    }

    public void Clear()
    {
        Token = null;
        User = null;
    }
}
