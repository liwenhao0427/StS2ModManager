using System.Globalization;
using System.Windows.Data;

namespace StS2ModManager.Helpers;

public class MiddleEllipsisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (!int.TryParse(parameter?.ToString(), out var maxChars) || maxChars < 8)
        {
            maxChars = 44;
        }

        if (text.Length <= maxChars)
        {
            return text;
        }

        var keepLeft = (maxChars - 1) / 2;
        var keepRight = maxChars - keepLeft - 1;
        if (keepLeft <= 0 || keepRight <= 0)
        {
            return text;
        }

        return $"{text[..keepLeft]}…{text[^keepRight..]}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }
}
