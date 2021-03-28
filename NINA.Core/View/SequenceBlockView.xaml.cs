using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Sequencer {

    /// <summary>
    /// Interaction logic for SequenceBlockView.xaml
    /// </summary>
    public partial class SequenceBlockView : UserControl {

        public SequenceBlockView() {
            InitializeComponent();
        }

        public static readonly DependencyProperty SequenceItemContentProperty =
            DependencyProperty.Register(nameof(SequenceItemContent), typeof(object), typeof(SequenceBlockView));

        public object SequenceItemContent {
            get { return (object)GetValue(SequenceItemContentProperty); }
            set { SetValue(SequenceItemContentProperty, value); }
        }

        public static readonly DependencyProperty SequenceItemProgressContentProperty =
            DependencyProperty.Register(nameof(SequenceItemProgressContent), typeof(object), typeof(SequenceBlockView));

        public object SequenceItemProgressContent {
            get { return (object)GetValue(SequenceItemProgressContentProperty); }
            set { SetValue(SequenceItemProgressContentProperty, value); }
        }
    }
}