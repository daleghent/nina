using System.Globalization;
using System.Net;
using System.Windows.Controls;

namespace NINA.Utility.ValidationRules {
    class IsValidIPAddressRule : ValidationRule {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            IPAddress o;
            if (IPAddress.TryParse(value.ToString(), out o)) {
                return new ValidationResult(true, null);
            } else {
                return new ValidationResult(false, $"{value} must be a valid IP address");
            }
        }
    }
}
