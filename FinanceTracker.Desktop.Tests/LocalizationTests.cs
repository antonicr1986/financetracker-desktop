using System.IO;
using System.Text.RegularExpressions;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class StringsTests
{
    [Fact]
    public void BothLanguages_HaveExactlyTheSameKeys()
    {
        Assert.Empty(Strings.Es.Keys.Except(Strings.En.Keys)); // falta en ingles
        Assert.Empty(Strings.En.Keys.Except(Strings.Es.Keys)); // falta en español
    }

    [Fact]
    public void NoTextIsEmpty()
    {
        Assert.DoesNotContain(Strings.Es, kv => string.IsNullOrWhiteSpace(kv.Value));
        Assert.DoesNotContain(Strings.En, kv => string.IsNullOrWhiteSpace(kv.Value));
    }

    /// <summary>
    /// Una clave de {DynamicResource} que no existe no da error en WPF: el texto
    /// sale vacio sin avisar. Esto lee los XAML y comprueba cada una.
    /// </summary>
    [Fact]
    public void EveryTextKeyUsedInXaml_Exists()
    {
        var project = FindAppProjectFolder();
        var keys = Directory.EnumerateFiles(project, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            // Se quitan los comentarios: pueden citar "{DynamicResource ...}" como ejemplo.
            .Select(f => Regex.Replace(File.ReadAllText(f), "<!--.*?-->", "", RegexOptions.Singleline))
            // Una clave empieza por letra: "dashboard.income", nunca "...".
            .SelectMany(xaml => Regex.Matches(xaml, @"\{DynamicResource ([A-Za-z][\w.]*)\}").Select(m => m.Groups[1].Value))
            // Los colores y visibilidades son del tema, no textos.
            .Where(k => !k.StartsWith("Brush.") && !k.StartsWith("Visibility."))
            .Distinct()
            .ToList();

        Assert.NotEmpty(keys);
        Assert.Empty(keys.Where(k => !Strings.Es.ContainsKey(k)));
    }

    /// <summary>Sube desde bin/ hasta encontrar la carpeta del proyecto de la aplicacion.</summary>
    private static string FindAppProjectFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "FinanceTracker.Desktop");
            if (File.Exists(Path.Combine(candidate, "FinanceTracker.Desktop.csproj"))) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("No se encontro FinanceTracker.Desktop");
    }
}

public class LocalizerTests
{
    // .NET separa el importe del simbolo con un espacio de no separacion.
    private static string Plain(string text) => text.Replace(' ', ' ').Replace(' ', ' ');

    [Fact]
    public void Amounts_AreAlwaysInEuros_InBothLanguages()
    {
        Assert.Equal("12.345,60 €", Plain(12345.6m.ToString("C", new Localizer(AppLanguage.Es).Culture)));
        Assert.Equal("€12,345.60", Plain(12345.6m.ToString("C", new Localizer(AppLanguage.En).Culture)));
    }

    [Fact]
    public void T_FillsPlaceholders_AndShowsTheKeyIfMissing()
    {
        var en = new Localizer(AppLanguage.En);

        Assert.Equal("Hi, Antonio", en.T("dashboard.greeting", "Antonio"));
        Assert.Equal("no.such.key", en.T("no.such.key"));
    }

    [Fact]
    public void ApiError_UsesTheCodesText_OrAGenericOne()
    {
        var es = Es;

        Assert.Equal(Strings.Es["apiError.invalid_credentials"], es.ApiError(ApiException.InvalidCredentials));
        Assert.Equal(Strings.Es["errors.unknown"], es.ApiError("something_new"));
    }

    [Fact]
    public void Changed_FiresOnlyWhenTheLanguageActuallyChanges()
    {
        var localizer = Es;
        var count = 0;
        localizer.Changed += (_, _) => count++;

        localizer.SetLanguage(AppLanguage.Es);
        localizer.SetLanguage(AppLanguage.En);

        Assert.Equal(1, count);
    }
}

public class LanguagePreferenceTests
{
    private class MemoryStore : ISettingsStore
    {
        private AppSettings settings = new();
        public AppSettings Load() => new() { Theme = settings.Theme, Language = settings.Language };
        public void Save(AppSettings value) => settings = value;
    }

    [Theory]
    [InlineData("es-ES", AppLanguage.Es)]
    [InlineData("es-MX", AppLanguage.Es)]
    [InlineData("en-US", AppLanguage.En)]
    [InlineData("fr-FR", AppLanguage.En)] // cualquier otro idioma: ingles, como la web
    public void WithoutAChoice_FollowsWindows(string system, AppLanguage expected)
    {
        Assert.Equal(expected, new LanguagePreference(new MemoryStore(), () => system).Effective);
    }

    [Fact]
    public void Saving_OverridesWindows_AndKeepsTheTheme()
    {
        var store = new MemoryStore();
        store.Save(new AppSettings { Theme = "dark" });
        var preference = new LanguagePreference(store, () => "es-ES");

        preference.Save(AppLanguage.En);

        Assert.Equal(AppLanguage.En, preference.Effective);
        Assert.Equal("dark", store.Load().Theme);
    }
}

/// <summary>Cambiar de idioma con las pantallas abiertas, sin volver a pedir nada a la API.</summary>
public class LanguageSwitchTests
{
    [Fact]
    public async Task Dashboard_RewritesMonthsAmountsAndGreeting_WithoutReloading()
    {
        var localizer = Es;
        var api = new FakeTransactionService
        {
            Handler = () => [Tx(1, "2026-09-10T00:00:00", 1234.5m, Income)]
        };
        var vm = new MainViewModel(LoggedInSession(), api, api, api, new FakeDialogService(), localizer);
        await vm.LoadCommand.ExecuteAsync(null);

        localizer.SetLanguage(AppLanguage.En);

        Assert.Equal("Hi, Antonio", vm.Greeting);
        Assert.Equal("September 2026", vm.SelectedMonth?.Label);
        Assert.Equal("€1,234.50", vm.IncomeText);
        Assert.Equal(1, api.LoadCount);
    }

    [Fact]
    public async Task AnErrorOnScreen_IsRewrittenInTheNewLanguage()
    {
        var localizer = Es;
        var vm = new LoginViewModel(new NeverCalledAuth(), new Session(), localizer);
        await vm.LoginCommand.ExecuteAsync(null); // campos vacios

        localizer.SetLanguage(AppLanguage.En);

        Assert.Equal(Strings.En["errors.fillCredentials"], vm.ErrorMessage);
    }

    [Fact]
    public void EditorTitle_FollowsTheLanguage_AndStopsAfterDispose()
    {
        var localizer = Es;
        var editor = new TransactionEditorViewModel(new FakeTransactionService(), new FakeDialogService(), [], localizer);
        var notified = 0;
        editor.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(editor.Title)) notified++; };

        localizer.SetLanguage(AppLanguage.En);
        Assert.Equal(Strings.En["dialog.title"], editor.Title);
        Assert.Equal(1, notified);

        editor.Dispose();
        localizer.SetLanguage(AppLanguage.Es);
        Assert.Equal(1, notified); // ya no escucha
    }

    private class NeverCalledAuth : IAuthService
    {
        public Task<LoginResponse> LoginAsync(string email, string password) =>
            throw new InvalidOperationException("No deberia llamarse");
    }
}
