using NINA.Sequencer.Container;
using System.Windows.Input;

namespace NINA.Sequencer.DragDrop {

    public interface IDroppable {
        ISequenceContainer Parent { get; }

        void AttachNewParent(ISequenceContainer newParent);

        void AfterParentChanged();

        /// <summary>
        /// Command to detach the item from the UI
        /// </summary>
        ICommand DetachCommand { get; }
        /// <summary>
        /// Removes this item from the parent
        /// </summary>
        void Detach();

        ICommand MoveUpCommand { get; }

        void MoveUp();

        ICommand MoveDownCommand { get; }

        void MoveDown();
    }
}