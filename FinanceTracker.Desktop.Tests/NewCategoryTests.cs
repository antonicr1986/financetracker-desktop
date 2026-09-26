using System.Net;
using System.Net.Http;
using System.Text;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class DuplicateCategoryTests
{
    private static readonly CategoryDto[] Existing = [new(1, "Regalos", Expense)];

    [Theory]
    [InlineData("Regalos")]
    [InlineData("  regalos ")] // sin distinguir mayusculas ni espacios
    [InlineData("REGALOS")]
    public void SameNameAndType_IsADuplicate(string name)
    {
        Assert.True(TransactionForm.IsDuplicateCategoryName(Existing, name, Expense));
    }

    [Fact]
    public void SameNameOfTheOtherType_IsAllowed()
    {
        Assert.False(TransactionForm.IsDuplicateCategoryName(Existing, "Regalos", Income));
    }
}

public class NewCategoryInEditorTests
{
    private static readonly List<CategoryDto> Categories =
    [
        new(1, "Supermercado", Expense),
        new(2, "Nómina", Income),
    ];

    private static TransactionEditorViewModel Editor(FakeTransactionService api) =>
        new(api, new FakeDialogService(), Categories, Es, categoryService: api);

    [Fact]
    public async Task Create_UsesTheTransactionsType_AndSelectsTheNewCategory()
    {
        var api = new FakeTransactionService();
        var vm = Editor(api);
        vm.IsIncome = true;
        vm.StartAddingCategoryCommand.Execute(null);
        vm.NewCategoryName = "  Regalos  ";

        await vm.CreateCategoryCommand.ExecuteAsync(null);

        var input = Assert.Single(api.CreatedCategories);
        Assert.Equal("Regalos", input.Name);
        Assert.Equal(Income, input.Type);
        Assert.Equal("Regalos", vm.SelectedCategory?.Name);
        Assert.Contains(vm.Categories, c => c.Name == "Regalos");
        Assert.False(vm.IsAddingCategory);
        Assert.Single(vm.CreatedCategories);
    }

    [Fact]
    public async Task Create_WithAnEmptyName_AsksForOne()
    {
        var api = new FakeTransactionService();
        var vm = Editor(api);
        vm.StartAddingCategoryCommand.Execute(null);
        vm.NewCategoryName = "   ";

        await vm.CreateCategoryCommand.ExecuteAsync(null);

        Assert.Equal(EsText("errors.writeCategoryName"), vm.ErrorMessage);
        Assert.Empty(api.CreatedCategories);
        Assert.True(vm.IsAddingCategory);
    }

    [Fact]
    public async Task Create_ADuplicate_IsStoppedBeforeCallingTheApi()
    {
        var api = new FakeTransactionService();
        var vm = Editor(api); // empieza como gasto
        vm.StartAddingCategoryCommand.Execute(null);
        vm.NewCategoryName = "supermercado";

        await vm.CreateCategoryCommand.ExecuteAsync(null);

        Assert.Equal(EsText("errors.categoryExists"), vm.ErrorMessage);
        Assert.Empty(api.CreatedCategories);
    }

    [Fact]
    public async Task Create_WhenTheApiFails_ShowsTheErrorAndKeepsTheBoxOpen()
    {
        var api = new FakeTransactionService { CategoryError = new ApiException(ApiException.Unknown) };
        var vm = Editor(api);
        vm.StartAddingCategoryCommand.Execute(null);
        vm.NewCategoryName = "Viajes";

        await vm.CreateCategoryCommand.ExecuteAsync(null);

        Assert.Equal(EsText("errors.createCategoryFailed"), vm.ErrorMessage);
        Assert.True(vm.IsAddingCategory);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void Cancel_ClosesTheBoxAndClearsIt()
    {
        var vm = Editor(new FakeTransactionService());
        vm.StartAddingCategoryCommand.Execute(null);
        vm.NewCategoryName = "Viajes";

        vm.CancelAddingCategoryCommand.Execute(null);

        Assert.False(vm.IsAddingCategory);
        Assert.Equal("", vm.NewCategoryName);
        Assert.True(vm.ShowNewCategoryLink);
    }

    [Fact]
    public void WithoutCategoriesOfTheType_SaysSo()
    {
        var vm = new TransactionEditorViewModel(new FakeTransactionService(), new FakeDialogService(),
            [new CategoryDto(1, "Supermercado", Expense)], Es);

        Assert.False(vm.HasNoCategoriesOfType);
        vm.IsIncome = true;
        Assert.True(vm.HasNoCategoriesOfType);
    }

    [Fact]
    public async Task ACategoryCreatedInACancelledForm_IsStillOfferedNextTime()
    {
        var api = new FakeTransactionService
        {
            Handler = () => [Tx(1, "2026-09-10T00:00:00", 10, Expense)],
            Categories = [new CategoryDto(1, "Supermercado", Expense)],
        };
        var dialogs = new FakeDialogService();
        var vm = new MainViewModel(LoggedInSession(), api, api, api, dialogs, Es, new MemorySettingsStore());
        await vm.LoadCommand.ExecuteAsync(null);

        // Primera vez: crea "Viajes" y cancela el movimiento.
        dialogs.Editor = editor =>
        {
            editor.StartAddingCategoryCommand.Execute(null);
            editor.NewCategoryName = "Viajes";
            editor.CreateCategoryCommand.Execute(null);
            return false;
        };
        await vm.NewTransactionCommand.ExecuteAsync(null);

        // Segunda vez: "Viajes" ya esta en el desplegable, sin recargar.
        dialogs.Editor = _ => false;
        await vm.NewTransactionCommand.ExecuteAsync(null);

        Assert.Contains(dialogs.LastEditor!.Categories, c => c.Name == "Viajes");
        Assert.Equal(1, api.LoadCount);
    }
}

public class CreateCategoryApiTests
{
    [Fact]
    public async Task Create_PostsNameAndType_AndReadsTheCreatedCategory()
    {
        HttpRequestMessage? sent = null;
        var handler = new LambdaHandler(request =>
        {
            sent = request;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"id":42,"name":"Viajes","type":"Expense"}""", Encoding.UTF8, "application/json"),
            };
        });
        var api = new ApiClient(LoggedInSession(), new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        var created = await api.CreateCategoryAsync(new CategoryInput("Viajes", Expense));

        Assert.Equal(HttpMethod.Post, sent!.Method);
        Assert.Equal("/api/Categories", sent.RequestUri!.AbsolutePath);
        Assert.Equal("""{"name":"Viajes","type":"Expense"}""", handler.Body);
        Assert.Equal(42, created.Id);
    }

    private class LambdaHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        /// <summary>
        /// Cuerpo de la peticion. Se lee aqui, con await, porque ApiClient libera
        /// la peticion al terminar y leerlo con .Result bloquearia (xUnit1031).
        /// </summary>
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond(request);
        }
    }
}
