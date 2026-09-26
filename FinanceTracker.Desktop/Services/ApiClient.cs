using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>Llamadas HTTP a la API de FinanceTracker.</summary>
public class ApiClient : IAuthService
{
    public const string BaseUrl =
        "https://financetracker-api-cpctbta0gddddge5.belgiumcentral-01.azurewebsites.net/";

    private readonly HttpClient http;

    public ApiClient(HttpClient? http = null)
    {
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
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("api/Users/login", new LoginRequest(email, password));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException(ApiException.NetworkError);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ReadErrorCodeAsync(response));
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>()
               ?? throw new ApiException(ApiException.Unknown);
    }

    private static async Task<string> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
            if (!string.IsNullOrEmpty(problem?.Code)) return problem.Code;
        }
        catch
        {
            // Cuerpo vacio o que no es JSON: nos quedamos con el estado HTTP.
        }

        return response.StatusCode == HttpStatusCode.Unauthorized
            ? ApiException.InvalidCredentials
            : ApiException.Unknown;
    }
}
