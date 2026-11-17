using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using QRCoder;

namespace FinanceTracker.Desktop.Controls;

/// <summary>
/// Convierte el enlace otpauth:// en la imagen del QR. Se genera aqui, en el
/// equipo: el secreto no se manda a ningun servicio externo de QR.
/// </summary>
public class QrCodeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || text.Length == 0) return null;

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(8);

        var image = new BitmapImage();
        using (var stream = new MemoryStream(png))
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
        }
        image.Freeze();
        return image;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
