using System.Windows;

namespace NINA.Core.Interfaces {
    public interface IMyMessageBoxVM {

        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxResult defaultResult);
    }
}