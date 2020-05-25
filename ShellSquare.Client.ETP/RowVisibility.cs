using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ShellSquare.Client.ETP
{
    [ValueConversion(typeof(string), typeof(bool))]
    public class RowVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter.ToString() == "1")  //Parameter is set in the xaml file.
            {
                if (value.Equals(true))
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;

                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
