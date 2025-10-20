using System.Globalization;

namespace ZakYip.Singulation.MauiApp.Converters;

/// <summary>
/// 布尔值转颜色转换器
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramString)
        {
            var parts = paramString.Split(':');
            if (parts.Length == 2)
            {
                var colorName = boolValue ? parts[0] : parts[1];
                return Color.FromArgb(colorName);
            }
        }
        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
