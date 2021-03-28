using NINA.Core.Enum;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace NINA.Utility.Behaviors {

    public class DragOverBehavior : Behavior<FrameworkElement> {
        public static readonly DependencyProperty DragBelowSizeProperty = DependencyProperty.Register(nameof(DragBelowSize), typeof(double), typeof(DragOverBehavior), new PropertyMetadata(0d));
        public static readonly DependencyProperty AllowDragCenterProperty = DependencyProperty.Register(nameof(AllowDragCenter), typeof(bool), typeof(DragOverBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty DragAboveSizeProperty = DependencyProperty.Register(nameof(DragAboveSize), typeof(double), typeof(DragOverBehavior), new PropertyMetadata(0d));
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register(nameof(Enabled), typeof(bool), typeof(DragOverBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty DragOverDisplayAnchorProperty = DependencyProperty.Register(nameof(DragOverDisplayAnchor), typeof(DragOverDisplayAnchor), typeof(DragOverBehavior), new PropertyMetadata(DragOverDisplayAnchor.Right));
        public static readonly DependencyProperty DragOverTopTextProperty = DependencyProperty.Register(nameof(DragOverTopText), typeof(string), typeof(DragOverBehavior), new PropertyMetadata(Locale.Loc.Instance["LblDragOver_TopText"]));
        public static readonly DependencyProperty DragOverBottomTextProperty = DependencyProperty.Register(nameof(DragOverBottomText), typeof(string), typeof(DragOverBehavior), new PropertyMetadata(Locale.Loc.Instance["LblDragOver_BottomText"]));
        public static readonly DependencyProperty DragOverCenterTextProperty = DependencyProperty.Register(nameof(DragOverCenterText), typeof(string), typeof(DragOverBehavior), new PropertyMetadata(Locale.Loc.Instance["LblDragOver_CenterText"]));

        private readonly Grid layoutParent = (Application.Current.MainWindow.FindName("RootGrid") as Grid);
        private DragOverAdorner dragOverAdorner;
        private FrameworkElement hitElement;

        public bool Enabled {
            get {
                return (bool)GetValue(EnabledProperty);
            }
            set {
                SetValue(EnabledProperty, value);
            }
        }

        public double DragBelowSize {
            get {
                return (double)GetValue(DragBelowSizeProperty);
            }
            set {
                SetValue(DragBelowSizeProperty, value);
            }
        }

        public double DragAboveSize {
            get {
                return (double)GetValue(DragAboveSizeProperty);
            }
            set {
                SetValue(DragAboveSizeProperty, value);
            }
        }

        public DragOverDisplayAnchor DragOverDisplayAnchor {
            get {
                return (DragOverDisplayAnchor)GetValue(DragOverDisplayAnchorProperty);
            }
            set {
                SetValue(DragOverDisplayAnchorProperty, value);
            }
        }

        public string DragOverTopText {
            get {
                return (string)GetValue(DragOverTopTextProperty);
            }
            set {
                SetValue(DragOverTopTextProperty, value);
            }
        }

        public string DragOverBottomText {
            get {
                return (string)GetValue(DragOverBottomTextProperty);
            }
            set {
                SetValue(DragOverBottomTextProperty, value);
            }
        }

        public string DragOverCenterText {
            get {
                return (string)GetValue(DragOverCenterTextProperty);
            }
            set {
                SetValue(DragOverCenterTextProperty, value);
            }
        }

        public bool AllowDragCenter {
            get {
                return (bool)GetValue(AllowDragCenterProperty);
            }
            set {
                SetValue(AllowDragCenterProperty, value);
            }
        }

        public DropTargetEnum DropTarget { get; set; }

        private bool hasDragOverElement = false;
        private DropTargetEnum lastDropTarget = DropTargetEnum.None;

        private void MouseInObject(object sender, MouseEventArgs e) {
            if (!Enabled) return;
            layoutParent.RaiseEvent(e);

            var layoutHitTestBase = Application.Current.MainWindow as UIElement;

            var mousePosition = e.GetPosition(layoutHitTestBase);

            // look for attached dragdropbehavior above the item
            VisualTreeHelper.HitTest(layoutHitTestBase,
                null,
                new HitTestResultCallback(FindDragDropItemAboveMyself),
                new PointHitTestParameters(mousePosition));
            VisualTreeHelper.HitTest(layoutHitTestBase,
                null,
                new HitTestResultCallback(FindFirstItemUnderDropItem),
                new PointHitTestParameters(mousePosition));

            if (hasDragOverElement && hitElement.DataContext == AssociatedObject.DataContext) {
                // check if we can actually drop into the found element
                var behaviors = Interaction.GetBehaviors(hitElement);
                var dropIntoBehavior = behaviors.FirstOrDefault(ex => ex is DropIntoBehavior) as DropIntoBehavior;
                if (dropIntoBehavior != null && !dropIntoBehavior.CanDropInto(lastAdorner.DragDropBehavior.OriginalParentedObject.DataContext.GetType())) {
                    lastDropTarget = DropTargetEnum.None;
                    DetachAdorner();
                    return;
                }

                var mousePos = e.GetPosition(AssociatedObject);
                var dropTarget = lastDropTarget;
                // check for mouse position relative to the current object
                if (DragAboveSize > mousePos.Y) {
                    lastDropTarget = DropTargetEnum.Top;
                } else if (AssociatedObject.ActualHeight - DragBelowSize < mousePos.Y) {
                    lastDropTarget = DropTargetEnum.Bottom;
                } else {
                    if (AllowDragCenter) {
                        lastDropTarget = DropTargetEnum.Center;
                    } else {
                        lastDropTarget = DropTargetEnum.None;
                    }
                }

                if (lastDropTarget != dropTarget) {
                    DetachAdorner();
                    AttachAdorner();
                }
            } else {
                if (lastDropTarget != DropTargetEnum.None) {
                    lastDropTarget = DropTargetEnum.None;
                    DetachAdorner();
                }
            }
        }

        private void AttachAdorner() {
            if (lastDropTarget == DropTargetEnum.Top || lastDropTarget == DropTargetEnum.Bottom || lastDropTarget == DropTargetEnum.Center) {
                int index = -1;

                foreach (UIElement child in layoutParent.Children) {
                    if (child is DragDropAdorner) {
                        index = layoutParent.Children.IndexOf(child);
                    }
                }

                TranslateTransform movingTransform = new TranslateTransform();

                Point relativeLocation = AssociatedObject.TranslatePoint(new Point(0, 0), layoutParent);
                double height = GetVisibleHeight(AssociatedObject);

                var adornerBaseWidth = AssociatedObject.ActualWidth;

                bool leftOfElement = DragOverDisplayAnchor == DragOverDisplayAnchor.Left;

                if (lastDropTarget == DropTargetEnum.Top) {
                    dragOverAdorner = new DragOverAdorner(adornerBaseWidth, height, DragOverTopText, leftOfElement, lastDropTarget, AssociatedObject);
                    movingTransform.Y = relativeLocation.Y - dragOverAdorner.AdornerHeight / 2;
                    movingTransform.X = relativeLocation.X + (leftOfElement ? (-(dragOverAdorner.AdornerWidth - adornerBaseWidth)) : adornerBaseWidth);
                } else if (lastDropTarget == DropTargetEnum.Center) {
                    dragOverAdorner = new DragOverAdorner(adornerBaseWidth, height, DragOverCenterText, leftOfElement, lastDropTarget, AssociatedObject);
                    movingTransform.Y = relativeLocation.Y + height / 2 - dragOverAdorner.AdornerHeight / 2;
                    movingTransform.X = relativeLocation.X + (leftOfElement ? (-(dragOverAdorner.AdornerWidth - adornerBaseWidth)) : adornerBaseWidth);
                } else {
                    dragOverAdorner = new DragOverAdorner(adornerBaseWidth, height, DragOverBottomText, leftOfElement, lastDropTarget, AssociatedObject);
                    movingTransform.Y = relativeLocation.Y + AssociatedObject.ActualHeight - dragOverAdorner.AdornerHeight / 2;
                    movingTransform.X = relativeLocation.X + (leftOfElement ? (-(dragOverAdorner.AdornerWidth - adornerBaseWidth)) : adornerBaseWidth);
                }

                layoutParent.Children.Insert(index, dragOverAdorner);
                dragOverAdorner.RenderTransform = movingTransform;
                movingTransform.X = relativeLocation.X + (leftOfElement ? (-(dragOverAdorner.AdornerWidth - adornerBaseWidth)) : adornerBaseWidth);
            }
        }

        private double GetVisibleHeight(FrameworkElement element) {
            FrameworkElement scrollViewer = null;
            DependencyObject searchingElement = element;
            while (VisualTreeHelper.GetParent(searchingElement) != null) {
                if (searchingElement is ScrollViewer) {
                    scrollViewer = searchingElement as ScrollViewer;
                    break;
                }

                searchingElement = VisualTreeHelper.GetParent(searchingElement);
            }

            if (scrollViewer == null) {
                return element.ActualHeight;
            }

            var actualHeight = scrollViewer.ActualHeight + scrollViewer.TranslatePoint(new Point(0, 0), element).Y;
            actualHeight = actualHeight > element.ActualHeight ? element.ActualHeight : actualHeight;

            return actualHeight;
        }

        private bool DetachAdorner() {
            if (layoutParent.Children.Contains(dragOverAdorner)) {
                layoutParent.Children.Remove(dragOverAdorner);
                return true;
            }

            return false;
        }

        private void MouseLeftObject(object sender, MouseEventArgs e) {
            if (!Enabled) return;
            DropTarget = lastDropTarget;
            lastDropTarget = DropTargetEnum.None;
            DetachAdorner();
        }

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.MouseMove += MouseInObject;
            AssociatedObject.MouseEnter += MouseInObject;
            AssociatedObject.MouseLeave += MouseLeftObject;
            dragOverAdorner = null;
        }

        protected override void OnDetaching() {
            base.OnDetaching();

            if (AssociatedObject != null) {
                AssociatedObject.MouseMove -= MouseInObject;
                AssociatedObject.MouseEnter -= MouseInObject;
                AssociatedObject.MouseLeave -= MouseLeftObject;
            }
        }

        private DragDropAdorner lastAdorner = null;

        public HitTestResultBehavior FindDragDropItemAboveMyself(HitTestResult result) {
            var hit = (result.VisualHit as FrameworkElement);
            if (hit != null) {
                // check for self, if we hit self, stop
                if (hit.DataContext == AssociatedObject.DataContext) {
                    hasDragOverElement = false;
                    return HitTestResultBehavior.Stop;
                }
                if (result.VisualHit is DragDropAdorner) {
                    lastAdorner = result.VisualHit as DragDropAdorner;
                    var behaviors = Interaction.GetBehaviors(AssociatedObject);
                    var dropIntoBehavior = behaviors.FirstOrDefault(ex => ex is DropIntoBehavior) as DropIntoBehavior;
                    var parent = AssociatedObject as DependencyObject;
                    while (dropIntoBehavior == null && parent != null) {
                        parent = VisualTreeHelper.GetParent(parent);
                        behaviors = Interaction.GetBehaviors(parent);
                        dropIntoBehavior = behaviors.FirstOrDefault(ex => ex is DropIntoBehavior) as DropIntoBehavior;
                    }
                    if (dropIntoBehavior == null || string.IsNullOrEmpty(dropIntoBehavior.AllowedDragDropTypesString)) {
                        hasDragOverElement = true;
                        return HitTestResultBehavior.Stop;
                    }
                    if (dropIntoBehavior.CanDropInto(lastAdorner.DragDropBehavior.OriginalParentedObject.DataContext.GetType())) {
                        hasDragOverElement = true;
                        return HitTestResultBehavior.Stop;
                    }
                }
            }

            return HitTestResultBehavior.Continue;
        }

        public HitTestResultBehavior FindFirstItemUnderDropItem(HitTestResult result) {
            var hit = (result.VisualHit as FrameworkElement);
            if (hit != null) {
                if (result.VisualHit is DragDropAdorner || result.VisualHit is DragOverAdorner) {
                    return HitTestResultBehavior.Continue;
                }

                hitElement = hit;
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }
    }
}