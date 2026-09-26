using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.Tests;

internal static class TestData
{
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
