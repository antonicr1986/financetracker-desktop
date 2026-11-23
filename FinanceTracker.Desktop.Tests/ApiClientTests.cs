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

        /// <summary>Cuerpo de la ultima peticion, leido antes de que ApiClient la libere.</summary>
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond(request);
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

    private static ApiClient Client(FakeHandler handler, ITrustedDeviceStore? trustedDevices = null, string? appVersion = null) =>
        new(LoggedInSession(), new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") }, trustedDevices, appVersion);

    private static HttpResponseMessage UpgradeRequired() => Json(
        """{"status":426,"code":"client_version_unsupported","minimumVersion":"1.12.0"}""",
        HttpStatusCode.UpgradeRequired);

    [Fact]
    public async Task EveryRequest_ShouldSendDesktopPlatformAndVersion()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Client(handler, appVersion: "1.11.6").ForgetDevicesAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal("desktop", Assert.Single(request.Headers.GetValues("X-Client-Platform")));
        Assert.Equal("1.11.6", Assert.Single(request.Headers.GetValues("X-Client-Version")));
    }

    [Fact]
    public async Task Response426_ShouldThrowClientVersionUnsupported()
    {
        var handler = new FakeHandler(_ => UpgradeRequired());

        var error = await Assert.ThrowsAsync<ApiException>(() => Client(handler).GetCategoriesAsync());

        Assert.Equal("client_version_unsupported", error.Code);
    }

    [Fact]
    public async Task Response426_ShouldRaiseUpgradeRequired_OnlyOnce()
    {
        var client = Client(new FakeHandler(_ => UpgradeRequired()));
        var raised = 0;
        client.UpgradeRequired += (_, _) => raised++;

        await Assert.ThrowsAsync<ApiException>(() => client.GetCategoriesAsync());
        await Assert.ThrowsAsync<ApiException>(() => client.GetBudgetsAsync());

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task VerifyMfa_RememberingTheDevice_SavesItsToken_AndLoginSendsIt()
    {
        var store = new MemoryTrustedDeviceStore();
        var handler = new FakeHandler(_ => Json(
            "{\"token\":\"jwt\",\"expiration\":\"2026-11-05T12:00:00Z\",\"user\":{\"id\":7,\"name\":\"Ana\",\"email\":\"ana@correo.com\"},\"trustedDeviceToken\":\"device-123\"}"));

        await Client(handler, store).VerifyMfaAsync("mfa-token", "123456", rememberDevice: true);

        Assert.Contains("\"rememberDevice\":true", handler.Body);
        Assert.Equal("device-123", store.Load());

        await Client(handler, store).LoginAsync("ana@correo.com", "una-clave-larga");

        Assert.Contains("\"trustedDeviceToken\":\"device-123\"", handler.Body);
    }

    [Fact]
    public async Task ForgetDevices_ClearsTheSavedToken()
    {
        var store = new MemoryTrustedDeviceStore();
        store.Save("device-123");
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Client(handler, store).ForgetDevicesAsync();

        Assert.Equal("https://api.test/api/users/mfa/forget-devices", Assert.Single(handler.Requests).RequestUri!.ToString());
        Assert.Null(store.Load());
    }

    [Fact]
    public async Task Login_ReadsHasPassword_AndLeavesItNullWhenMissing()
    {
        var withField = await Client(new FakeHandler(_ => Json(
            """{"token":"t","expiration":"2026-11-20T12:00:00Z","user":{"id":7,"name":"Ana","email":"a@correo.com","hasPassword":false}}""")))
            .LoginAsync("a@correo.com", "una-clave-larga");
        var withoutField = await Client(new FakeHandler(_ => Json(
            """{"token":"t","expiration":"2026-11-20T12:00:00Z","user":{"id":7,"name":"Ana","email":"a@correo.com"}}""")))
            .LoginAsync("a@correo.com", "una-clave-larga");

        Assert.False(withField.User.HasPassword);
        Assert.Null(withoutField.User.HasPassword);
    }

    [Fact]
    public async Task ChangePassword_PostsBothPasswords_AndReturnsTheNewToken()
    {
        var handler = new FakeHandler(_ => Json("""{"token":"new-jwt","expiration":"2026-11-20T12:00:00Z","user":{"id":7,"name":"Ana","email":"ana@correo.com"}}"""));

        var login = await Client(handler).ChangePasswordAsync("la-actual-larga", "la-nueva-larga");

        Assert.Equal("https://api.test/api/users/password", Assert.Single(handler.Requests).RequestUri!.ToString());
        Assert.Contains("\"currentPassword\":\"la-actual-larga\"", handler.Body);
        Assert.Contains("\"newPassword\":\"la-nueva-larga\"", handler.Body);
        Assert.Equal("new-jwt", login.Token);
    }

    [Fact]
    public async Task ChangePassword_ReadsTheErrorCode_OfAWrongCurrentPassword()
    {
        var handler = new FakeHandler(_ => Json("""{"code":"invalid_current_password"}""", HttpStatusCode.BadRequest));

        var error = await Assert.ThrowsAsync<ApiException>(() => Client(handler).ChangePasswordAsync("mal", "la-nueva-larga"));

        Assert.Equal("invalid_current_password", error.Code);
    }

    [Fact]
    public async Task ForgotPassword_PostsTheEmail_WithoutASession()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Client(handler).ForgotPasswordAsync("ana@correo.com");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.test/api/users/password/forgot", request.RequestUri!.ToString());
        Assert.Null(request.Headers.Authorization);
        Assert.Contains("\"email\":\"ana@correo.com\"", handler.Body);
    }

    [Fact]
    public async Task ResetPassword_PostsEmailCodeAndNewPassword()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Client(handler).ResetPasswordAsync("ana@correo.com", "123456", "la-nueva-larga");

        Assert.Equal("https://api.test/api/users/password/reset", Assert.Single(handler.Requests).RequestUri!.ToString());
        Assert.Contains("\"code\":\"123456\"", handler.Body);
        Assert.Contains("\"newPassword\":\"la-nueva-larga\"", handler.Body);
    }

    [Fact]
    public async Task ResetPassword_ReadsTheErrorCode_OfAWrongCode()
    {
        var handler = new FakeHandler(_ => Json("""{"code":"invalid_reset_code"}""", HttpStatusCode.BadRequest));

        var error = await Assert.ThrowsAsync<ApiException>(
            () => Client(handler).ResetPasswordAsync("ana@correo.com", "000000", "la-nueva-larga"));

        Assert.Equal("invalid_reset_code", error.Code);
    }

    [Fact]
    public async Task RevokeSessions_PostsToItsEndpoint_AndKeepsTheTrustedDevice()
    {
        var store = new MemoryTrustedDeviceStore();
        store.Save("device-123");
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Client(handler, store).RevokeSessionsAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.test/api/users/sessions/revoke", request.RequestUri!.ToString());
        Assert.Equal("device-123", store.Load());
    }

    [Fact]
    public async Task GoogleLogin_PostsIdTokenAndLanguage()
    {
        var handler = new FakeHandler(_ => Json("""{"token":"jwt","expiration":"2026-11-05T12:00:00Z","user":{"id":7,"name":"María","email":"maria@gmail.com"}}"""));

        var login = await Client(handler).GoogleLoginAsync("google-id-token", "en");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/Users/google", request.RequestUri!.AbsolutePath);
        Assert.Contains("\"idToken\":\"google-id-token\"", handler.Body);
        Assert.Contains("\"language\":\"en\"", handler.Body);
        Assert.Equal("jwt", login.Token);
    }

    [Fact]
    public async Task GoogleLogin_Unauthorized_IsGoogleSignInFailed()
    {
        var handler = new FakeHandler(_ => Json("""{"code":"invalid_google_token"}""", HttpStatusCode.Unauthorized));

        var error = await Assert.ThrowsAsync<ApiException>(() => Client(handler).GoogleLoginAsync("x", "es"));

        Assert.Equal(ApiException.GoogleSignInFailed, error.Code);
    }

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

    private static readonly Models.TransactionInput Input =
        new("Cena", 30.5m, new DateTime(2026, 9, 20), Models.TransactionType.Expense, 3);

    [Fact]
    public async Task Create_PostsTheBodyTheApiExpects()
    {
        var handler = new FakeHandler(request =>
        {
            return Json("""{"id":1,"description":"Cena","amount":30.5,"date":"2026-09-20T00:00:00","type":"Expense","categoryId":3,"categoryName":"Comida"}""",
                HttpStatusCode.Created);
        });

        var created = await Client(handler).CreateTransactionAsync(Input);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/Transactions", request.RequestUri!.AbsolutePath);
        Assert.Equal("""{"description":"Cena","amount":30.5,"date":"2026-09-20T00:00:00","type":"Expense","categoryId":3}""", handler.Body);
        Assert.Equal(1, created.Id);
    }

    [Fact]
    public async Task Update_AcceptsA204WithNoBody()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Client(handler).UpdateTransactionAsync(5, Input);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/api/Transactions/5", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Delete_AcceptsA204_AndA404MeansNotFound()
    {
        var ok = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        await Client(ok).DeleteTransactionAsync(5);
        Assert.Equal(HttpMethod.Delete, Assert.Single(ok.Requests).Method);

        var missing = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var e = await Assert.ThrowsAsync<ApiException>(() => Client(missing).DeleteTransactionAsync(5));
        Assert.Equal(ApiException.NotFound, e.Code);
    }

    [Fact]
    public async Task Save_CategoryTypeMismatch_KeepsTheApiCode()
    {
        var handler = new FakeHandler(_ =>
            Json("""{"code":"category_type_mismatch","detail":"..."}""", HttpStatusCode.BadRequest));

        var e = await Assert.ThrowsAsync<ApiException>(() => Client(handler).CreateTransactionAsync(Input));

        Assert.Equal(ApiException.CategoryTypeMismatch, e.Code);
    }
}
