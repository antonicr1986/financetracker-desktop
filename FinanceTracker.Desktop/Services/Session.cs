using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>Resultado de intentar recuperar la sesion al abrir la aplicacion.</summary>
public enum SessionRestore
{
    /// <summary>No habia sesion guardada: login normal.</summary>
    None,

    /// <summary>Habia una y sigue valiendo: directo al panel.</summary>
    Restored,

    /// <summary>Habia una pero el token ya caduco: login con el aviso.</summary>
    Expired,
}

/// <summary>
/// Sesion actual. Si recibe un ISessionStore, sobrevive a cerrar la
/// aplicacion: se guarda al iniciar sesion y se borra al salir.
/// </summary>
public class Session(ISessionStore? store = null)
{
    /// <summary>
    /// Margen antes de la caducidad real: un token al que le queda un minuto
    /// caducaria en mitad de la primera carga del panel.
    /// </summary>
    public static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(2);

    public string? Token { get; private set; }
    public UserInfo? User { get; private set; }

    public bool IsActive => Token is not null;

    public void Start(LoginResponse login)
    {
        Token = login.Token;
        User = login.User;
        store?.Save(login);
    }

    public void Clear()
    {
        Token = null;
        User = null;
        store?.Clear();
    }

    /// <summary>Al arrancar: recupera la sesion guardada si el token sigue valiendo.</summary>
    public SessionRestore TryRestore(DateTime utcNow)
    {
        var saved = store?.Load();
        if (saved is null) return SessionRestore.None;

        // La API la calcula con DateTime.UtcNow; si llegara sin zona, es UTC.
        var expiration = saved.Expiration.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(saved.Expiration, DateTimeKind.Utc)
            : saved.Expiration.ToUniversalTime();

        if (expiration - ExpiryMargin <= utcNow)
        {
            store!.Clear();
            return SessionRestore.Expired;
        }

        Token = saved.Token;
        User = saved.User;
        return SessionRestore.Restored;
    }
}
