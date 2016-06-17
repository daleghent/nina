using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AstrophotographyBuddy.Model {
    class UIColorsModel : BaseINPC {

        public UIColorsModel() {
            /*
            PrimaryColor = new SolidColorBrush(Colors.Red);
            SecondaryColor = new SolidColorBrush(Colors.OrangeRed);
            BackgroundColor = new SolidColorBrush(Colors.Black);
            BorderColor = new SolidColorBrush(Colors.Orange);
            */
            PrimaryColor = new SolidColorBrush(Colors.Black);
            SecondaryColor = new SolidColorBrush(Colors.OrangeRed);
            BackgroundColor = new SolidColorBrush(Colors.White);
            BorderColor = new SolidColorBrush(Colors.Orange);
        }

        private SolidColorBrush _primaryColor;
        public SolidColorBrush PrimaryColor {
            get {
                return _primaryColor;
            }
            set {
                _primaryColor = value;
                RaisePropertyChanged();
            }
        }

        private SolidColorBrush _secondaryColor;
        public SolidColorBrush SecondaryColor {
            get {
                return _secondaryColor;
            }
            set {
                _secondaryColor = value;
                RaisePropertyChanged();
            }
        }

        private SolidColorBrush _backgroundColor;
        public SolidColorBrush BackgroundColor {
            get {
                return _backgroundColor;
            }
            set {
                _backgroundColor = value;
                RaisePropertyChanged();
            }
        }

        private SolidColorBrush _borderColor;
        public SolidColorBrush BorderColor {
            get {
                return _borderColor;
            }
            set {
                _borderColor = value;
                RaisePropertyChanged();
            }
        }
    }
}
