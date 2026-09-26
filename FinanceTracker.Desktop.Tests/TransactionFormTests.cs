using FinanceTracker.Desktop.Domain;

namespace FinanceTracker.Desktop.Tests;

public class TransactionFormTests
{
    [Theory]
    [InlineData("12,50", 12.50)]
    [InlineData("12.50", 12.50)]
    [InlineData(" 7 ", 7)]
    [InlineData("0,99", 0.99)]
    public void ParseAmount_AcceptsCommaOrDot(string text, double expected)
    {
        Assert.Equal((decimal)expected, TransactionForm.ParseAmount(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.000,50")] // separador de miles: ambiguo, se rechaza
    [InlineData("-5")]
    [InlineData(null)]
    public void ParseAmount_RejectsWhatIsNotAPlainNumber(string? text)
    {
        Assert.Null(TransactionForm.ParseAmount(text));
    }

    [Theory]
    [InlineData(12.50, "12,5")]
    [InlineData(1000, "1000")]
    [InlineData(0.99, "0,99")]
    public void FormatAmountForInput_HasNoCurrencyNoThousandsNoTrailingZeros(double amount, string expected)
    {
        Assert.Equal(expected, TransactionForm.FormatAmountForInput((decimal)amount));
    }

    [Fact]
    public void Validate_ReturnsTheFirstProblemInFormOrder()
    {
        var today = DateTime.Today;

        Assert.Equal(TransactionFormProblem.MissingDescription, TransactionForm.Validate("  ", "x", null, false));
        Assert.Equal(TransactionFormProblem.DescriptionTooLong,
            TransactionForm.Validate(new string('a', 151), "10", today, true));
        Assert.Equal(TransactionFormProblem.InvalidAmount, TransactionForm.Validate("Cena", "diez", today, true));
        Assert.Equal(TransactionFormProblem.AmountNotPositive, TransactionForm.Validate("Cena", "0", today, true));
        Assert.Equal(TransactionFormProblem.MissingDate, TransactionForm.Validate("Cena", "10", null, true));
        Assert.Equal(TransactionFormProblem.MissingCategory, TransactionForm.Validate("Cena", "10", today, false));
        Assert.Null(TransactionForm.Validate("Cena", "10", today, true));
    }
}
