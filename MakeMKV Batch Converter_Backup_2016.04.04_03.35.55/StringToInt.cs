using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;

namespace MakeMKV_Batch_Converter
{
    class StringToInt : IValueConverter
    {
        public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        {
            int val;
            if (int.TryParse(value.ToString(), out val))
            {
                return val;
            }
            return 1800;
            // Do the conversion from bool to visibility
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            int val;
            if (int.TryParse((string)value, out val))
            {
                return val;
            }
            return 1800;
            // Do the conversion from visibility to bool
        }
    }
}
