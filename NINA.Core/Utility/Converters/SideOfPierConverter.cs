using NINA.Core.Enum;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Core.Utility.Converters {

    public class SideOfPierConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            try {
                PierSide pierSide = (PierSide)value;
                switch (pierSide) {
                    case PierSide.pierEast:
                        return Locale.Loc.Instance["LblEast"];

                    case PierSide.pierWest:
                        return Locale.Loc.Instance["LblWest"];

                    default:
                        return string.Empty;
                }
            } catch (Exception ex) {
                Logger.Error(ex, $"Failed to convert {value} to PierSide");
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}