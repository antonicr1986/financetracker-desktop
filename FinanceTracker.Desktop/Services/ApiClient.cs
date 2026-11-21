using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>Llamadas HTTP a la API de FinanceTracker.</summary>
public class ApiClient : IAuthService, IMfaService, ITransactionService, ICategoryService, IBudgetService
{
    public const string BaseUrl =
        "https://financetracker-api-cpctbta0gddddge5.belgiumcentral-01.azurewebsites.net/";

    /// <summary>La API no acepta mas de 100 por pagina ([Range(1, 100)]).</summary>
    public const int PageSize = 100;

    private readonly HttpClient http;
    private readonly Session session;
    private readonly ITrustedDeviceStore? trustedDevices;

    public ApiClient(Session session, HttpClient? http = null, ITrustedDeviceStore? trustedDevices = null)
    {
        this.session = session;
        this.trustedDevices = trustedDevices;

        // El plan F1 de Azure se duerme y la base de datos serverless se pausa:
        // la primera peticion del dia puede superar los 100 s por defecto.
        this.http = http ?? new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(120),
        };
    }

    /// <summary>
    /// Despierta la API y la base de datos sin esperar a usar el resultado.
    /// La API corre en el plan gratuito de Azure, que se descarga tras un rato
    /// sin uso, y Azure SQL serverless se pausa a la hora. Se llama al abrir el
    /// login (/health/ready consulta la base de datos), asi la espera ocurre
    /// mientras se mira la pantalla y no al pulsar "Entrar". Cualquier fallo
    /// se ignora: el login funciona igual, solo que con la espera de siempre.
    /// </summary>
    public async Task WarmUpAsync()
    {
        try
        {
            using var response = await http.GetAsync("health/ready");
        }
        catch (Exception)
        {
            // Sin red, timeout o la API caida: no es asunto del precalentamiento.
        }
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Users/login")
        {
            // Con un dispositivo de confianza valido, la API se salta el codigo del MFA.
            Content = JsonContent.Create(new LoginRequest(email, password, trustedDevices?.Load())),
        };

        // En el login un 401 es una contrasena incorrecta, no una sesion caducada.
        using var response = await SendAsync(request, unauthorizedCode: ApiException.InvalidCredentials);
        return await ReadAsync<LoginResponse>(response);
    }

    public async Task<LoginResponse> GoogleLoginAsync(string idToken, string language)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Users/google")
        {
            Content = JsonContent.Create(new GoogleLoginRequest(idToken, language, trustedDevices?.Load())),
        };

        // Aqui un 401 es que Google no ha verificado la cuenta.
        using var response = await SendAsync(request, unauthorizedCode: ApiException.GoogleSignInFailed);
        return await ReadAsync<LoginResponse>(response);
    }

    public async Task<LoginResponse> VerifyMfaAsync(string mfaToken, string code, bool rememberDevice = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/users/mfa/verify")
        {
            Content = JsonContent.Create(new MfaVerifyRequest(mfaToken, code, rememberDevice)),
        };

        // Aqui un 401 puede ser dos cosas (codigo malo o token caducado): se
        // lee el codigo del cuerpo en lugar de fijarlo.
        using var response = await SendAsync(request, unauthorizedCode: null);
        var login = await ReadAsync<LoginResponse>(response);

        // "No volver a pedir el codigo en este equipo": se guarda (cifrado).
        if (login.TrustedDeviceToken is not null) trustedDevices?.Save(login.TrustedDeviceToken);

        return login;
    }

    // Sin sesion todavia: un 401 aqui es el paso caducado, y se lee su codigo.
    public async Task<LoginResponse> VerifyEmailAsync(string verificationToken, string code)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/users/email/verify")
        {
            Content = JsonContent.Create(new EmailVerifyRequest(verificationToken, code)),
        };
        using var response = await SendAsync(request, unauthorizedCode: null);
        return await ReadAsync<LoginResponse>(response);
    }

    public async Task ResendEmailCodeAsync(string verificationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/users/email/resend")
        {
            Content = JsonContent.Create(new EmailResendRequest(verificationToken)),
        };
        using var response = await SendAsync(request, unauthorizedCode: null);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/users/password/forgot")
        {
            Content = JsonContent.Create(new ForgotPasswordRequest(email)),
        };
        using var response = await SendAsync(request, unauthorizedCode: null);
    }

    public async Task ResetPasswordAsync(string email, string code, string newPassword)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/users/password/reset")
        {
            Content = JsonContent.Create(new ResetPasswordRequest(email, code, newPassword)),
        };
        using var response = await SendAsync(request, unauthorizedCode: null);
    }

    public async Task<MfaSetupResponse> SetupMfaAsync()
    {
        using var request = Authorized(HttpMethod.Post, "api/users/mfa/setup");
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        return await ReadAsync<MfaSetupResponse>(response);
    }

    public async Task<MfaRecoveryCodesResponse> ConfirmMfaAsync(string code)
    {
        using var request = Authorized(HttpMethod.Post, "api/users/mfa/confirm");
        request.Content = JsonContent.Create(new MfaCodeRequest(code));
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        return await ReadAsync<MfaRecoveryCodesResponse>(response);
    }

    // 204 sin cuerpo, como PUT y DELETE.
    public async Task DisableMfaAsync(string code)
    {
        using var request = Authorized(HttpMethod.Post, "api/users/mfa/disable");
        request.Content = JsonContent.Create(new MfaCodeRequest(code));
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);

        // La API ya ha olvidado los dispositivos: el token guardado no sirve.
        trustedDevices?.Save(null);
    }

    public async Task ForgetDevicesAsync()
    {
        using var request = Authorized(HttpMethod.Post, "api/users/mfa/forget-devices");
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        trustedDevices?.Save(null);
    }

    // El dispositivo de confianza del MFA se conserva: eso es ForgetDevicesAsync.
    public async Task RevokeSessionsAsync()
    {
        using var request = Authorized(HttpMethod.Post, "api/users/sessions/revoke");
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
    }

    // Los errores del cambio (contraseña actual mal, filtrada...) llegan como
    // 400 con su codigo; un 401 solo puede ser la sesion caducada o revocada.
    public async Task<LoginResponse> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        using var request = Authorized(HttpMethod.Post, "api/users/password");
        request.Content = JsonContent.Create(new ChangePasswordRequest(currentPassword, newPassword));
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        return await ReadAsync<LoginResponse>(response);
    }

    public async Task<UserInfo> RegisterAsync(string name, string email, string password, string language)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Users/register")
        {
            Content = JsonContent.Create(new RegisterRequest(name, email, password, language)),
        };

        // Sin sesion todavia: un 401 aqui no significaria nada conocido.
        using var response = await SendAsync(request, unauthorizedCode: ApiException.Unknown);
        return await ReadAsync<UserInfo>(response);
    }

    public async Task<List<TransactionDto>> GetAllTransactionsAsync()
    {
        var all = new List<TransactionDto>();
        var pageNumber = 1;
        int totalPages;

        do
        {
            using var request = Authorized(HttpMethod.Get,
                $"api/Transactions?pageNumber={pageNumber}&pageSize={PageSize}");
            using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
            var page = await ReadAsync<PagedResult<TransactionDto>>(response);

            all.AddRange(page.Items);
            totalPages = Math.Max(page.TotalPages, 1);
            pageNumber++;
        } while (pageNumber <= totalPages);

        return all;
    }

    public async Task<TransactionDto> CreateTransactionAsync(TransactionInput input)
    {
        using var request = Authorized(HttpMethod.Post, "api/Transactions");
        request.Content = JsonContent.Create(input);
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        return await ReadAsync<TransactionDto>(response);
    }

    // PUT y DELETE responden 204 sin cuerpo: no se intenta leer JSON, porque
    // fallaria y convertiria cada exito en un error.
    public async Task UpdateTransactionAsync(int id, TransactionInput input)
    {
        using var request = Authorized(HttpMethod.Put, $"api/Transactions/{id}");
        request.Content = JsonContent.Create(input);
        using var _ = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
    }

    public async Task DeleteTransactionAsync(int id)
    {
        using var request = Authorized(HttpMethod.Delete, $"api/Transactions/{id}");
        using var _ = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
    }

    /// <summary>Este endpoint devuelve un array normal, no un PagedResult.</summary>
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        using var request = Authorized(HttpMethod.Get, "api/Categories");
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        return await ReadAsync<List<CategoryDto>>(response);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CategoryInput input)
    {
        using var request = Authorized(HttpMethod.Post, "api/Categories");
        request.Content = JsonContent.Create(input);
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        return await ReadAsync<CategoryDto>(response);
    }

    // 204 sin cuerpo, como el resto de DELETE.
    public async Task DeleteCategoryAsync(int id)
    {
        using var request = Authorized(HttpMethod.Delete, $"api/Categories/{id}");
        using var _ = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
    }

    public async Task<List<BudgetDto>> GetBudgetsAsync()
    {
        using var request = Authorized(HttpMethod.Get, "api/Budgets");
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        return await ReadAsync<List<BudgetDto>>(response);
    }

    public async Task<BudgetDto> CreateBudgetAsync(BudgetInput input)
    {
        using var request = Authorized(HttpMethod.Post, "api/Budgets");
        request.Content = JsonContent.Create(input);
        using var response = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
        return await ReadAsync<BudgetDto>(response);
    }

    public async Task UpdateBudgetAsync(int id, BudgetInput input)
    {
        using var request = Authorized(HttpMethod.Put, $"api/Budgets/{id}");
        request.Content = JsonContent.Create(input);
        using var _ = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
    }

    public async Task DeleteBudgetAsync(int id)
    {
        using var request = Authorized(HttpMethod.Delete, $"api/Budgets/{id}");
        using var _ = await SendAsync(request, unauthorizedCode: ApiException.SessionExpired);
    }

    private HttpRequestMessage Authorized(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        return request;
    }

    /// <summary>
    /// Envia la peticion y convierte cualquier fallo en una ApiException con
    /// codigo. Un 401 significa cosas distintas segun donde ocurra, asi que
    /// quien llama dice que codigo le corresponde.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string? unauthorizedCode)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException(ApiException.NetworkError);
        }

        if (response.IsSuccessStatusCode) return response;

        using (response)
        {
            // Sin codigo fijo (null), el 401 se lee del cuerpo como cualquier otro error.
            if (response.StatusCode == HttpStatusCode.Unauthorized && unauthorizedCode is not null)
                throw new ApiException(unauthorizedCode);

            // El 404 de editar o borrar llega sin cuerpo: el movimiento ya no existe.
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new ApiException(ApiException.NotFound);

            throw new ApiException(await ReadErrorCodeAsync(response));
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>() ?? throw new ApiException(ApiException.Unknown);

    private static async Task<string> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
            if (!string.IsNullOrEmpty(problem?.Code)) return problem.Code;
        }
        catch
        {
            // Cuerpo vacio o que no es JSON.
        }

        return ApiException.Unknown;
    }
}
