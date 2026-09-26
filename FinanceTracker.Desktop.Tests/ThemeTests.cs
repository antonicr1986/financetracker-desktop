using System.IO;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.Tests;

public class ThemePreferenceTests
{
    /// <summary>Almacen en memoria, sin tocar el disco.</summary>
    private class MemoryStore : ISettingsStore
    {
        public AppSettings Settings { get; private set; } = new();
        public int Saves { get; private set; }

        public AppSettings Load() => new() { Theme = Settings.Theme };

        public void Save(AppSettings settings)
        {
            Saves++;
            Settings = settings;
        }
    }

    [Theory]
    [InlineData(false, AppTheme.Light)]
    [InlineData(true, AppTheme.Dark)]
    public void WithoutAChoice_FollowsWindows(bool systemDark, AppTheme expected)
    {
        var preference = new ThemePreference(new MemoryStore(), () => systemDark);

        Assert.Null(preference.Chosen);
        Assert.Equal(expected, preference.Effective);
    }

    [Fact]
    public void Toggle_SwitchesFromWhatIsShownAndSavesIt()
    {
        var store = new MemoryStore();
        var preference = new ThemePreference(store, () => true); // Windows en oscuro

        var next = preference.Toggle();

        Assert.Equal(AppTheme.Light, next);
        Assert.Equal("light", store.Settings.Theme);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public void OnceChosen_IgnoresWindows()
    {
        var systemDark = false;
        var preference = new ThemePreference(new MemoryStore(), () => systemDark);
        preference.Toggle(); // claro -> oscuro

        systemDark = false; // Windows sigue en claro
        Assert.Equal(AppTheme.Dark, preference.Effective);

        preference.Toggle();
        Assert.Equal(AppTheme.Light, preference.Effective);
    }

    [Fact]
    public void AnUnknownValue_CountsAsNoChoice()
    {
        var store = new MemoryStore();
        store.Save(new AppSettings { Theme = "morado" });
        var preference = new ThemePreference(store, () => true);

        Assert.Null(preference.Chosen);
        Assert.Equal(AppTheme.Dark, preference.Effective);
    }
}

public class SettingsStoreTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "ft-tests-" + Guid.NewGuid());
    private string FilePath => Path.Combine(folder, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips_CreatingTheFolder()
    {
        var store = new SettingsStore(FilePath);

        store.Save(new AppSettings { Theme = "dark" });

        Assert.Equal("dark", new SettingsStore(FilePath).Load().Theme);
    }

    [Fact]
    public void MissingOrBrokenFile_GivesDefaults()
    {
        Assert.Null(new SettingsStore(FilePath).Load().Theme);

        Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath, "{ esto no es json");

        Assert.Null(new SettingsStore(FilePath).Load().Theme);
    }
}
