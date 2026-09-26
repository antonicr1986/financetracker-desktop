using System.Net;
using System.Net.Http;
using System.Text;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class DeleteCategoryInEditorTests
{
    private static List<CategoryDto> Categories() =>
    [
        new(1, "Alquiler", Expense),
        new(2, "Viajes", Expense),
        new(3, "Nómina", Income),
    ];

    private static TransactionEditorViewModel Editor(FakeTransactionService api, FakeDialogService dialogs) =>
        new(api, dialogs, Categories(), Es, categoryService: api);

    [Fact]
    public async Task Delete_AsksFirst_ThenRemovesTheSelectedCategory()
    {
        var api = new FakeTransactionService();
        var dialogs = new FakeDialogService();
        var vm = Editor(api, dialogs);
        vm.SelectedCategory = vm.Categories.Single(c => c.Name == "Viajes");

        await vm.DeleteCategoryCommand.ExecuteAsync(null);

        Assert.Equal(1, dialogs.ConfirmCount);
        Assert.Equal(new[] { 2 }, api.DeletedCategories);
        Assert.DoesNotContain(vm.Categories, c => c.Name == "Viajes");
        Assert.Equal("Alquiler", vm.SelectedCategory?.Name); // pasa a la primera que queda
        Assert.Equal(new[] { 2 }, vm.DeletedCategoryIds);
    }

    [Fact]
    public async Task BothDeletions_AskWithTheAppsOwnTranslatedButton()
    {
        var api = new FakeTransactionService();
        var dialogs = new FakeDialogService { ConfirmAnswer = false };

        await Editor(api, dialogs).DeleteCategoryCommand.ExecuteAsync(null);
        Assert.Equal(EsText("dialog.deleteYes"), dialogs.LastConfirmText);

        var edit = new TransactionEditorViewModel(api, dialogs, Categories(), Es,
            Tx(9, "2026-09-10T00:00:00", 10, Expense));
        await edit.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(EsText("dialog.deleteYes"), dialogs.LastConfirmText);
    }

    [Fact]
    public async Task Delete_WhenTheUserSaysNo_DoesNothing()
    {
        var api = new FakeTransactionService();
        var vm = Editor(api, new FakeDialogService { ConfirmAnswer = false });

        await vm.DeleteCategoryCommand.ExecuteAsync(null);

        Assert.Empty(api.DeletedCategories);
        Assert.Equal(2, vm.Categories.Count);
    }

    [Fact]
    public async Task Delete_ACategoryWithTransactions_ShowsTheApiReasonAndKeepsIt()
    {
        var api = new FakeTransactionService
        {
            DeleteCategoryError = new ApiException(ApiException.CategoryHasTransactions),
        };
        var vm = Editor(api, new FakeDialogService());
        var before = vm.SelectedCategory;

        await vm.DeleteCategoryCommand.ExecuteAsync(null);

        Assert.Equal(EsText("apiError.category_has_transactions"), vm.ErrorMessage);
        Assert.Equal(before, vm.SelectedCategory);
        Assert.Empty(vm.DeletedCategoryIds);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Delete_OneThatNoLongerExists_IsRemovedAnyway()
    {
        var api = new FakeTransactionService { DeleteCategoryError = new ApiException(ApiException.NotFound) };
        var vm = Editor(api, new FakeDialogService());
        var id = vm.SelectedCategory!.Id;

        await vm.DeleteCategoryCommand.ExecuteAsync(null);

        Assert.DoesNotContain(vm.Categories, c => c.Id == id);
        Assert.Equal(EsText("errors.categoryGone"), vm.ErrorMessage);
    }

    [Fact]
    public void Delete_IsDisabled_WithoutACategoryOfTheType()
    {
        var vm = new TransactionEditorViewModel(new FakeTransactionService(), new FakeDialogService(),
            [new CategoryDto(1, "Alquiler", Expense)], Es, categoryService: new FakeTransactionService());

        Assert.True(vm.DeleteCategoryCommand.CanExecute(null));
        vm.IsIncome = true; // no hay categorias de ingreso
        Assert.False(vm.DeleteCategoryCommand.CanExecute(null));
    }

    [Fact]
    public async Task ACategoryCreatedAndDeletedInTheSameForm_IsNotOfferedAgain()
    {
        var api = new FakeTransactionService
        {
            Handler = () => [Tx(1, "2026-09-10T00:00:00", 10, Expense)],
            Categories = [new CategoryDto(1, "Alquiler", Expense)],
        };
        var dialogs = new FakeDialogService();
        var main = new MainViewModel(LoggedInSession(), api, api, api, dialogs, Es, new MemorySettingsStore());
        await main.LoadCommand.ExecuteAsync(null);

        dialogs.Editor = editor =>
        {
            editor.StartAddingCategoryCommand.Execute(null);
            editor.NewCategoryName = "Temporal";
            editor.CreateCategoryCommand.Execute(null);   // queda elegida
            editor.DeleteCategoryCommand.Execute(null);   // y se borra
            return false;
        };
        await main.NewTransactionCommand.ExecuteAsync(null);

        dialogs.Editor = _ => false;
        await main.NewTransactionCommand.ExecuteAsync(null);

        Assert.DoesNotContain(dialogs.LastEditor!.Categories, c => c.Name == "Temporal");
        Assert.Contains(dialogs.LastEditor.Categories, c => c.Name == "Alquiler");
    }
}

public class DeleteCategoryApiTests
{
    private class LambdaHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            return Task.FromResult(respond(request));
        }
    }

    private static ApiClient Client(HttpMessageHandler handler) =>
        new(LoggedInSession(), new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

    [Fact]
    public async Task Delete_SendsTheIdWithTheToken_AndAcceptsA204()
    {
        var handler = new LambdaHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await Client(handler).DeleteCategoryAsync(7);

        Assert.Equal(HttpMethod.Delete, handler.Last!.Method);
        Assert.Equal("/api/Categories/7", handler.Last.RequestUri!.AbsolutePath);
        // El token es lo que hace que la API solo toque las categorias de este usuario.
        Assert.Equal("Bearer", handler.Last.Headers.Authorization?.Scheme);
    }

    [Fact]
    public async Task Delete_WithTransactions_KeepsTheApiCode()
    {
        var handler = new LambdaHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"code":"category_has_transactions","detail":"..."}""",
                Encoding.UTF8, "application/json"),
        });

        var e = await Assert.ThrowsAsync<ApiException>(() => Client(handler).DeleteCategoryAsync(7));

        Assert.Equal(ApiException.CategoryHasTransactions, e.Code);
    }
}
