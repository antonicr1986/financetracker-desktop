using System.IO;
using System.Text;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.Tests;

public class SessionRestoreTests
{
    private class MemorySessionStore : ISessionStore
    {
        public LoginResponse? Saved { get; set; }
        public int Clears { get; private set; }

        public LoginResponse? Load() => Saved;
        public void Save(LoginResponse login) => Saved = login;

        public void Clear()
        {
            Clears++;
            Saved = null;
        }
    }

    private static readonly DateTime Now = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    private static LoginResponse Login(DateTime expiration) =>
        new("token-guardado", expiration, new UserInfo(1, "Antonio", "antonio@example.com"));

    [Fact]
    public void Start_SavesTheSession_AndClear_RemovesIt()
    {
        var store = new MemorySessionStore();
        var session = new Session(store);

        session.Start(Login(Now.AddHours(1)));
        Assert.Equal("token-guardado", store.Saved?.Token);

        session.Clear();
        Assert.Null(store.Saved);
        Assert.False(session.IsActive);
    }

    [Fact]
    public void TryRestore_WithAValidToken_SignsInWithoutAskingAgain()
    {
        var store = new MemorySessionStore { Saved = Login(Now.AddMinutes(30)) };
        var session = new Session(store);

        Assert.Equal(SessionRestore.Restored, session.TryRestore(Now));
        Assert.Equal("token-guardado", session.Token);
        Assert.Equal("Antonio", session.User?.Name);
    }

    [Fact]
    public void TryRestore_WithAnExpiredToken_ReportsItAndForgetsIt()
    {
        var store = new MemorySessionStore { Saved = Login(Now.AddMinutes(-5)) };
        var session = new Session(store);

        Assert.Equal(SessionRestore.Expired, session.TryRestore(Now));
        Assert.False(session.IsActive);
        Assert.Null(store.Saved);
    }

    [Fact]
    public void TryRestore_CountsATokenAboutToExpireAsExpired()
    {
        // Le queda un minuto: caducaria en mitad de la primera carga.
        var store = new MemorySessionStore { Saved = Login(Now.AddMinutes(1)) };

        Assert.Equal(SessionRestore.Expired, new Session(store).TryRestore(Now));
    }

    [Fact]
    public void TryRestore_TreatsAnExpirationWithoutZoneAsUtc()
    {
        var unspecified = DateTime.SpecifyKind(Now.AddMinutes(30), DateTimeKind.Unspecified);
        var store = new MemorySessionStore { Saved = Login(unspecified) };

        Assert.Equal(SessionRestore.Restored, new Session(store).TryRestore(Now));
    }

    [Fact]
    public void TryRestore_WithNothingSaved_OrNoStore_IsNone()
    {
        Assert.Equal(SessionRestore.None, new Session(new MemorySessionStore()).TryRestore(Now));
        Assert.Equal(SessionRestore.None, new Session().TryRestore(Now));
    }
}

/// <summary>DPAPI de verdad, contra un archivo temporal. Solo Windows (como el CI).</summary>
public class DpapiSessionStoreTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "ft-session-" + Guid.NewGuid());
    private string FilePath => Path.Combine(folder, "session.dat");

    public void Dispose()
    {
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
    }

    private static readonly LoginResponse Sample =
        new("eyJ.token.de-prueba", new DateTime(2026, 9, 26, 13, 0, 0, DateTimeKind.Utc),
            new UserInfo(7, "Antonio", "antonio@example.com"));

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        new DpapiSessionStore(FilePath).Save(Sample);

        var loaded = new DpapiSessionStore(FilePath).Load();

        Assert.Equal(Sample.Token, loaded?.Token);
        Assert.Equal(Sample.User, loaded?.User);
        Assert.Equal(Sample.Expiration, loaded?.Expiration.ToUniversalTime());
    }

    [Fact]
    public void TheFileOnDisk_DoesNotContainTheTokenOrTheEmailInPlainText()
    {
        new DpapiSessionStore(FilePath).Save(Sample);

        var raw = Encoding.UTF8.GetString(File.ReadAllBytes(FilePath));

        Assert.DoesNotContain("eyJ.token.de-prueba", raw);
        Assert.DoesNotContain("antonio@example.com", raw);
    }

    [Fact]
    public void ABrokenFile_CountsAsNoSession_AndIsRemoved()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath, "esto no esta cifrado con DPAPI");

        Assert.Null(new DpapiSessionStore(FilePath).Load());
        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void Clear_RemovesTheFile()
    {
        var store = new DpapiSessionStore(FilePath);
        store.Save(Sample);

        store.Clear();

        Assert.False(File.Exists(FilePath));
        Assert.Null(store.Load());
    }
}
