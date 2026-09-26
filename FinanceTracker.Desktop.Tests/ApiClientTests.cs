using System.Net;
using System.Net.Http;
using System.Text;
using FinanceTracker.Desktop.Services;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

/// <summary>
/// Pruebas del cliente HTTP contra un HttpMessageHandler falso: comprueban lo
/// que sale hacia la API y como se interpreta la respuesta, sin red.
/// </summary>
public class ApiClientTests
{
    private class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static string Page(int pageNumber, int totalPages, params int[] ids)
    {
        var items = string.Join(",", ids.Select(id =>
            $$"""{"id":{{id}},"description":"t{{id}}","amount":10.5,"date":"2026-09-01T00:00:00","type":"Expense","categoryId":1,"categoryName":"Varios"}"""));
        return $$"""{"items":[{{items}}],"totalCount":0,"pageNumber":{{pageNumber}},"pageSize":100,"totalPages":{{totalPages}}}""";
    }

    private static ApiClient Client(FakeHandler handler) =>
        new(LoggedInSession(), new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

    [Fact]
    public async Task GetAll_WalksEveryPageWithTokenAndPageSize100()
    {
        var handler = new FakeHandler(request =>
        {
            var page = request.RequestUri!.Query.Contains("pageNumber=1") ? 1 : 2;
            return Json(page == 1 ? Page(1, 2, 1, 2) : Page(2, 2, 3));
        });

        var all = await Client(handler).GetAllTransactionsAsync();

        Assert.Equal(new[] { 1, 2, 3 }, all.Select(t => t.Id));
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, r =>
        {
            Assert.Equal("Bearer", r.Headers.Authorization?.Scheme);
            Assert.Equal("token", r.Headers.Authorization?.Parameter);
            Assert.Contains("pageSize=100", r.RequestUri!.Query);
        });
        Assert.Equal(Models.TransactionType.Expense, all[0].Type);
    }

    [Fact]
    public async Task GetAll_WithASinglePage_DoesNotAskForASecondOne()
    {
        var handler = new FakeHandler(_ => Json(Page(1, 1, 1)));

        await Client(handler).GetAllTransactionsAsync();

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetAll_Unauthorized_MeansSessionExpired()
    {
        var handler = new FakeHandler(_ => Json("", HttpStatusCode.Unauthorized));

        var e = await Assert.ThrowsAsync<ApiException>(() => Client(handler).GetAllTransactionsAsync());

        Assert.Equal(ApiException.SessionExpired, e.Code);
    }

    [Fact]
    public async Task Login_Unauthorized_MeansInvalidCredentials()
    {
        var handler = new FakeHandler(_ =>
            Json("""{"code":"invalid_credentials","detail":"Invalid email or password."}""", HttpStatusCode.Unauthorized));

        var e = await Assert.ThrowsAsync<ApiException>(() => Client(handler).LoginAsync("a@b.com", "bad"));

        Assert.Equal(ApiException.InvalidCredentials, e.Code);
    }
}
