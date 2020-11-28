using System.Windows;

namespace NINA.MyMessageBox {

    public interface IMyMessageBoxVM {

        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxResult defaultResult);
    }

    internal class MyMessageBoxVM : IMyMessageBoxVM {

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxResult defaultResult) {
            return MyMessageBox.Show(messageBoxText, caption, button, defaultResult);
        }
    }
}