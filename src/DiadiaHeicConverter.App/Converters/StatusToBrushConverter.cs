using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DiadiaHeicConverter.App.Models;
using MediaColor = System.Windows.Media.Color;

namespace DiadiaHeicConverter.App.Converters;

public sealed class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ConversionStatus.Succeeded => new SolidColorBrush(MediaColor.FromRgb(22, 163, 74)),
            ConversionStatus.Failed => new SolidColorBrush(MediaColor.FromRgb(220, 38, 38)),
            ConversionStatus.Converting => new SolidColorBrush(MediaColor.FromRgb(37, 99, 235)),
            ConversionStatus.Skipped => new SolidColorBrush(MediaColor.FromRgb(202, 138, 4)),
            ConversionStatus.Cancelled => new SolidColorBrush(MediaColor.FromRgb(107, 114, 128)),
            _ => new SolidColorBrush(MediaColor.FromRgb(75, 85, 99))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
