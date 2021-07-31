using NINA.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NINA.Core.Utility.Converters {

    public class EnumTooltipTypeConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            FieldInfo fi = value?.GetType().GetField(value.ToString());
            if (fi != null) {
                var attributes = (TooltipDescriptionAttribute[])fi.GetCustomAttributes(typeof(TooltipDescriptionAttribute), false);
                var label = ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].TooltipLabel))) ? attributes[0].TooltipLabel : value.ToString();
                var s = Locale.Loc.Instance[label];
                return s;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
