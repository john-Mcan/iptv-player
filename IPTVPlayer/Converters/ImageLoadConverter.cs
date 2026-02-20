using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace IPTVPlayer.Converters;

/// <summary>
/// Converts a URL string into a BitmapImage with async download.
/// Uses default BitmapCacheOption (OnDemand) which downloads asynchronously
/// for HTTP URIs without blocking the UI thread.
/// Returns null if the URL is empty or invalid.
/// </summary>
public class ImageLoadConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url, UriKind.Absolute);
            bitmap.DecodePixelWidth = 28;
            bitmap.DecodePixelHeight = 28;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
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
