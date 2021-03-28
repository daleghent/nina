using NINA.Core.Enum;

namespace NINA.Sequencer.DragDrop {

    public class DropIntoParameters {

        public DropIntoParameters(IDroppable source, IDroppable target, DropTargetEnum? position) {
            Source = source;
            Target = target;
            Position = position;
        }

        public DropIntoParameters(IDroppable source, IDroppable target) : this(source, target, null) {
        }

        public DropIntoParameters(IDroppable source) : this(source, null, null) {
        }

        public IDroppable Source { get; }
        public IDroppable Target { get; set; }
        public DropTargetEnum? Position { get; set; }

        public bool Duplicate { get; set; } = false;
    }
}