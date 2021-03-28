namespace NINA.Core.Enum {

    public enum DropTargetEnum {
        Top = 0b00000001,
        Bottom = 0b00000010,
        Center = 0b00000100,
        Left = 0b00001000,
        Right = 0b00010000,
        None = 0b00000000
    }

    public enum DragOverDisplayAnchor {
        Left,
        Right
    }
}