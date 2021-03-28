using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Behaviors {

    internal class DragDropAdorner : Adorner {
        private readonly RenderTargetBitmap thisImg;
        public DragDropBehavior DragDropBehavior;

        public DragDropAdorner(DragDropBehavior dragDropBehavior, UIElement adornedElement, RenderTargetBitmap img) : base(adornedElement) {
            thisImg = img;
            DragDropBehavior = dragDropBehavior;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            drawingContext.DrawImage(thisImg, new Rect(0, 0, thisImg.Width, thisImg.Height));
        }

        protected override Size MeasureOverride(Size constraint) {
            var result = base.MeasureOverride(constraint);
            InvalidateVisual();
            return result;
        }
    }
}