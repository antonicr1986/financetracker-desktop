using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.Tests;

internal static class TestData
{
    /// <summary>
    /// Localizador en español, uno nuevo en cada llamada: los tests se ejecutan
    /// en paralelo y no deben compartir un idioma que otro test pueda cambiar.
    /// </summary>
    public static Localizer Es => new(AppLanguage.Es);

    /// <summary>Texto esperado, leido del diccionario y no escrito a mano.</summary>
    public static string EsText(string key) => Strings.Es[key];

    public static TransactionDto Tx(int id, string date, decimal amount, TransactionType type,
        string description = "Movimiento", string category = "Varios") =>
        new(id, description, amount, DateTime.Parse(date), type, 1, category);

    public static Session LoggedInSession()
    {
        var session = new Session();
        session.Start(new LoginResponse("token", DateTime.UtcNow.AddHours(1),
            new UserInfo(1, "Antonio", "antonio@example.com")));
        return session;
    }
}
