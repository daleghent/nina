using AstrophotographyBuddy.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace AstrophotographyBuddy.Resources {
    partial class MySVGDictionary : ResourceDictionary {
        public MySVGDictionary() {
            InitializeComponent();
            
            //(this.Content as FrameworkElement).DataContext = this;
        }

        public string CameraSVGColor {
            get { return (string)GetValue(TextProperty); }
            set { SetValueDp(TextProperty, value); }
        }

        private string GetValue(DependencyProperty textProperty) {
            throw new NotImplementedException();
        }
        private void SetValue(DependencyProperty property, object value) {
            throw new NotImplementedException();
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("CameraSVGColor", typeof(string), typeof(MySVGDictionary), null);

        public event PropertyChangedEventHandler PropertyChanged;
        void SetValueDp(DependencyProperty property, object value, [System.Runtime.CompilerServices.CallerMemberName] String p = null) {
            SetValue(property, value);
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
        }

       
    }


}
