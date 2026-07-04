using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiadiaHeicConverter.App.Converters;

public sealed class RatioToStrokeDashArrayConverter : IValueConverter
{
    private const double NormalizedRingLength = 30.2;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var ratio = value is double number ? Math.Clamp(number, 0, 1) : 0;
        var filled = Math.Max(0.01, NormalizedRingLength * ratio);
        var empty = Math.Max(0.01, NormalizedRingLength - filled);
        var collection = new DoubleCollection();
        collection.Add(filled);
        collection.Add(empty);
        return collection;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
