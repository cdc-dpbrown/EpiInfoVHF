﻿using System;
using System.Windows;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public sealed class TodayToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return Visibility.Visible;

            DateTime dt = (DateTime)value;

            DateTime today = DateTime.Today;

            if (dt.Year == today.Year && dt.Month == today.Month && dt.Day == today.Day)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
