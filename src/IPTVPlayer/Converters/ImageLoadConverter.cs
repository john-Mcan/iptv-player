using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace IPTVPlayer.Converters;

public class ImageLoadConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            int decodeWidth = 200;
            if (parameter is string ps && int.TryParse(ps, out var pw))
                decodeWidth = pw;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url, UriKind.Absolute);
            bitmap.DecodePixelWidth = decodeWidth;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
