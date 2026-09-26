using System.Globalization;

namespace FinanceTracker.Desktop.Domain;

/// <summary>Lo primero que falla en el formulario de movimiento.</summary>
public enum TransactionFormProblem
{
    MissingDescription,
    DescriptionTooLong,
    InvalidAmount,
    AmountNotPositive,
    MissingDate,
    MissingCategory,
}

/// <summary>
/// Reglas del formulario de movimiento, en C# puro, igual que Validation.kt en
/// Android. El ViewModel solo traduce el resultado a una frase.
/// </summary>
public static class TransactionForm
{
    /// <summary>MaxLength(150) de CreateTransactionDto en la API.</summary>
    public const int MaxDescriptionLength = 150;

    /// <summary>
    /// Lee el importe escrito. Acepta coma o punto como decimal, porque la coma
    /// es lo que teclea cualquiera en español. null si no es un numero.
    /// No acepta separador de miles: "1.000,50" es ambiguo y se rechaza.
    /// </summary>
    public static decimal? ParseAmount(string? text)
    {
        var normalised = text?.Trim().Replace(',', '.');
        if (string.IsNullOrEmpty(normalised) || normalised.Count(c => c == '.') > 1) return null;

        return decimal.TryParse(normalised, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    /// <summary>Importe para rellenar el campo al editar: 12,5 y no 12,50 €.</summary>
    public static string FormatAmountForInput(decimal amount) =>
        amount.ToString("0.##########", CultureInfo.InvariantCulture).Replace('.', ',');

    /// <summary>null si el formulario es valido.</summary>
    public static TransactionFormProblem? Validate(string description, string amountText, DateTime? date, bool hasCategory)
    {
        var trimmed = description.Trim();
        if (trimmed.Length == 0) return TransactionFormProblem.MissingDescription;
        if (trimmed.Length > MaxDescriptionLength) return TransactionFormProblem.DescriptionTooLong;

        var amount = ParseAmount(amountText);
        if (amount is null) return TransactionFormProblem.InvalidAmount;
        if (amount <= 0) return TransactionFormProblem.AmountNotPositive;

        if (date is null) return TransactionFormProblem.MissingDate;
        if (!hasCategory) return TransactionFormProblem.MissingCategory;

        return null;
    }
}
