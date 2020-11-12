using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View.Sequencer {

    /// <summary>
    /// Interaction logic for SequenceContainerView.xaml
    /// </summary>
    public partial class SequenceContainerView : UserControl {

        public SequenceContainerView() {
            InitializeComponent();
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        public static readonly DependencyProperty SequenceContainerContentProperty =
            DependencyProperty.Register(nameof(SequenceContainerContent), typeof(object), typeof(SequenceBlockView));

        public object SequenceContainerContent {
            get { return (object)GetValue(SequenceContainerContentProperty); }
            set { SetValue(SequenceContainerContentProperty, value); }
        }

        public static readonly DependencyProperty ShowDetailsProperty =
            DependencyProperty.Register(nameof(ShowDetails), typeof(bool), typeof(SequenceBlockView), new PropertyMetadata(true));

        public bool ShowDetails {
            get { return (bool)GetValue(ShowDetailsProperty); }
            set { SetValue(ShowDetailsProperty, value); }
        }
    }
}