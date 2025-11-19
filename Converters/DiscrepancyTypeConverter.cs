using System;
using System.Globalization;
using System.Windows.Data;
using DocumentChecker.Models;

namespace DocumentChecker.Converters
{
    public class DiscrepancyTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DiscrepancyType type)
            {
                return type switch
                {
                    DiscrepancyType.ExactMismatch => "Точное несовпадение",
                    DiscrepancyType.PartialMatch => "Опечатка",
                    DiscrepancyType.KnownError => "Известная ошибка",
                    _ => value?.ToString() ?? string.Empty
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
