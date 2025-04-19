using System;
using System.Globalization;
using System.Windows.Data;

namespace MidiVolumeMixer.ui
{
    /// <summary>
    /// Converts a float volume (0.0-1.0) to a percentage (0-100) for progress bars
    /// </summary>
    public class VolumeToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float floatValue)
            {
                return floatValue * 100;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return (float)(doubleValue / 100.0);
            }
            return 0f;
        }
    }

    /// <summary>
    /// Converts a float volume (0.0-1.0) to a percentage string for display
    /// </summary>
    public class VolumeToPercentStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float floatValue)
            {
                return $"{Math.Round(floatValue * 100)}%";
            }
            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && stringValue.EndsWith("%"))
            {
                if (float.TryParse(stringValue.TrimEnd('%'), out float result))
                {
                    return result / 100f;
                }
            }
            return 0f;
        }
    }
}