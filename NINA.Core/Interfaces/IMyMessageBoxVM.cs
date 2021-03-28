using System.Windows;

namespace NINA.MyMessageBox {
    public interface IMyMessageBoxVM {

        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxResult defaultResult);
    }
}