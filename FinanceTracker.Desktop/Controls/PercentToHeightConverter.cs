using System.Globalization;
using System.Windows.Data;

namespace FinanceTracker.Desktop.Controls;

/// <summary>
/// Alto de una barra del grafico: el porcentaje (0-100) que calcula el
/// ViewModel sobre el alto total que se pasa como parametro.
/// </summary>
public class PercentToHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percent = value is int p ? p : 0;
        var total = double.Parse(parameter as string ?? "0", CultureInfo.InvariantCulture);
        return total * Math.Clamp(percent, 0, 100) / 100;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
