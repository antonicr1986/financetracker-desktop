using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;

namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Inicio de sesion con Google para apps de escritorio: abre el navegador,
/// la persona elige su cuenta y Google devuelve la respuesta a un puerto
/// local (127.0.0.1). Lo hace la libreria oficial de Google.
/// </summary>
public class GoogleSignIn : IGoogleSignIn
{
    // Cliente "App de escritorio" de Google Cloud. El client ID no es secreto;
    // la API lo acepta como audiencia (Google:DesktopClientId).
    private const string ClientId = "718720619866-io9ff8nbeh69gb73hknqbr9s3cjfichh.apps.googleusercontent.com";

    // Si se cierra el navegador sin elegir cuenta, Google no avisa: se deja
    // de esperar pasado este tiempo.
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// El secreto de cliente no esta en el repositorio: MSBuild lo incrusta al
    /// compilar desde la variable de entorno GOOGLE_DESKTOP_CLIENT_SECRET (en
    /// local, la del PC; en la release, un secret de GitHub). Vacio si se
    /// compilo sin ella.
    /// </summary>
    private static string ClientSecretFromBuild() =>
        typeof(GoogleSignIn).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "GoogleDesktopClientSecret")?.Value ?? "";

    public async Task<string> GetIdTokenAsync()
    {
        var clientSecret = ClientSecretFromBuild();
        if (clientSecret.Length == 0)
        {
            throw new ApiException(ApiException.GoogleSignInFailed);
        }

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = ClientId, ClientSecret = clientSecret },
            Scopes = ["openid", "email", "profile"],
            // No se guarda ningun token de Google en disco: solo hace falta el
            // ID token una vez, para que la API abra la sesion.
            DataStore = new NullDataStore(),
        });

        using var timeout = new CancellationTokenSource(Timeout);

        try
        {
            var credential = await new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver())
                .AuthorizeAsync("user", timeout.Token);

            return credential.Token.IdToken
                ?? throw new ApiException(ApiException.GoogleSignInFailed);
        }
        catch (Exception e) when (e is TokenResponseException or OperationCanceledException)
        {
            // Denegado en la pantalla de Google, o sin respuesta a tiempo.
            throw new ApiException(ApiException.GoogleSignInFailed);
        }
    }
}
