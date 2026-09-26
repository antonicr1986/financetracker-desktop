using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>Llamadas HTTP a la API de FinanceTracker.</summary>
public class ApiClient : IAuthService, ITransactionService
{
    public const string BaseUrl =
        "https://financetracker-api-cpctbta0gddddge5.belgiumcentral-01.azurewebsites.net/";

    /// <summary>La API no acepta mas de 100 por pagina ([Range(1, 100)]).</summary>
    public const int PageSize = 100;

    private readonly HttpClient http;
    private readonly Session session;

    public ApiClient(Session session, HttpClient? http = null)
    {
        this.session = session;

        // El plan F1 de Azure se duerme y la base de datos serverless se pausa:
        // la primera peticion del dia puede superar los 100 s por defecto.
        this.http = http ?? new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(120),
        };
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Users/login")
        {
            Content = JsonContent.Create(new LoginRequest(email, password)),
        };

        // En el login un 401 es una contrasena incorrecta, no una sesion caducada.
        using var response = await SendAsync(request, unauthorizedCode: ApiException.InvalidCredentials);
        return await ReadAsync<LoginResponse>(response);
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
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string unauthorizedCode)
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
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new ApiException(unauthorizedCode);

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
