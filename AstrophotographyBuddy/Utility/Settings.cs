using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AstrophotographyBuddy.Utility {
    static class Settings {
        public static string ImageFilePath = "";
        public static string ImageFilePattern = "";

        public static SolidColorBrush PrimaryColor = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush SecondaryColor = new SolidColorBrush(Colors.OrangeRed);
        public static SolidColorBrush BackgroundColor = new SolidColorBrush(Colors.White);
        public static SolidColorBrush BorderColor = new SolidColorBrush(Colors.Orange);
    }
}
