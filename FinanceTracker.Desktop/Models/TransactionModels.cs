using System.Text.Json.Serialization;

namespace FinanceTracker.Desktop.Models;

// La API envia el tipo como texto ("Income"/"Expense"), no como numero.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    Income,
    Expense,
}

/// <summary>
/// Copia de TransactionDto de la API. La fecha llega como "2026-09-22T00:00:00",
/// sin zona horaria, asi que se lee como DateTimeKind.Unspecified y .NET no la
/// convierte: el dia 1 sigue siendo el dia 1 este donde este el usuario.
/// </summary>
public record TransactionDto(
    int Id,
    string Description,
    decimal Amount,
    DateTime Date,
    TransactionType Type,
    int CategoryId,
    string CategoryName);

public record PagedResult<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages);

public record CategoryDto(int Id, string Name, TransactionType Type)
{
    /// <summary>Lo que muestra un ComboBox con la categoria elegida.</summary>
    public override string ToString() => Name;
}

/// <summary>
/// Cuerpo de alta y de edicion: la API tiene CreateTransactionDto y
/// UpdateTransactionDto, pero son identicos, asi que aqui hay uno solo.
/// </summary>
public record TransactionInput(string Description, decimal Amount, DateTime Date, TransactionType Type, int CategoryId);
