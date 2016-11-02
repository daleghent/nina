using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToastNotifications;

namespace AstrophotographyBuddy.Utility {
    static class Notification {

        private static NotificationsSource _notificationSource;

        public static NotificationsSource NotificationSource {
            get {
                if (_notificationSource == null) {
                    _notificationSource = new NotificationsSource {
                        MaximumNotificationCount = 1                       
                    };
                }
                return _notificationSource;
            }
            set {
                _notificationSource = value;
                if (NotificationChanged != null)
                    NotificationChanged(null, EventArgs.Empty);
            }
        }

        public static event EventHandler NotificationChanged;

        public static void ShowInformation(string message) {
            NotificationSource.Show(message, NotificationType.Information);
        }

        public static void ShowSuccess(string message) {
            NotificationSource.Show(message, NotificationType.Success);
        }

        public static void ShowWarning(string message) {
            NotificationSource.Show(message, NotificationType.Warning);
        }

        public static void ShowError(string message) {
            NotificationSource.Show(message, NotificationType.Error);
        }

    }
}
