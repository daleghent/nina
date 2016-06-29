using System.Windows.Media;

namespace AstrophotographyBuddy.Utility {
    static class Settings  {
        public static string CameraId {
            get {
                return Properties.Settings.Default.CameraId;
            }
            set {
                Properties.Settings.Default.CameraId = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string TelescopeId {
            get {
                return Properties.Settings.Default.TelescopeId;
            }
            set {
                Properties.Settings.Default.TelescopeId = value;
                Properties.Settings.Default.Save();
            }
        }

        public static int LogLevel {
            get {
                return Properties.Settings.Default.LogLevel;
            }
            set {
                Properties.Settings.Default.LogLevel = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string FilterWheelId {
            get {
                return Properties.Settings.Default.FilterWheelId;
            }
            set {
                Properties.Settings.Default.FilterWheelId = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string ImageFilePath {
            get {
                return Properties.Settings.Default.ImageFilePath;
            }
            set {
                Properties.Settings.Default.ImageFilePath = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string ImageFilePattern {
            get {
                return Properties.Settings.Default.ImageFilePattern;
            }
            set {
                Properties.Settings.Default.ImageFilePattern = value;
                Properties.Settings.Default.Save();
            }
        }
        public static string PHD2ServerUrl {
            get {
                return Properties.Settings.Default.PHD2ServerUrl;
            }
            set {
                Properties.Settings.Default.PHD2ServerUrl = value;
                Properties.Settings.Default.Save();
            }
        }
        public static int PHD2ServerPort {
            get {
                return Properties.Settings.Default.PHD2ServerPort;
            }
            set {
                Properties.Settings.Default.PHD2ServerPort = value;
                Properties.Settings.Default.Save();
            }
        }

        public static double DitherPixels {
            get {
                return Properties.Settings.Default.DitherPixels;
            }
            set {
                Properties.Settings.Default.DitherPixels = value;
                Properties.Settings.Default.Save();
            }
        }

        public static bool DitherRAOnly {
            get {
                return Properties.Settings.Default.DitherRAOnly;
            }
            set {
                Properties.Settings.Default.DitherRAOnly = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color PrimaryColor {
            get {
                return Properties.Settings.Default.PrimaryColor;
            }
            set {
                Properties.Settings.Default.PrimaryColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color SecondaryColor {
            get {
                return Properties.Settings.Default.SecondaryColor;
            }
            set {
                Properties.Settings.Default.SecondaryColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color BorderColor {
            get {
                return Properties.Settings.Default.BorderColor;
            }
            set {
                Properties.Settings.Default.BorderColor = value;
                Properties.Settings.Default.Save();
            }
        }

        public static Color BackgroundColor {
            get {
                return Properties.Settings.Default.BackgroundColor;
            }
            set {
                Properties.Settings.Default.BackgroundColor = value;
                Properties.Settings.Default.Save();

            }
        }

        

    }

}
