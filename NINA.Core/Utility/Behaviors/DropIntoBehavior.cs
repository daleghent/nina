using NINA.Core.Enum;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace NINA.Utility.Behaviors {

    public class DropIntoBehavior : Behavior<UIElement> {
        public static readonly DependencyProperty OnDropCommandProperty = DependencyProperty.Register(nameof(OnDropCommand), typeof(ICommand), typeof(DropIntoBehavior));

        public static readonly DependencyProperty AllowedDragDropTypesProperty = DependencyProperty.Register(nameof(AllowedDragDropTypesString),
            typeof(string), typeof(DropIntoBehavior));

        public DropIntoBehavior() {
            AllowedDragDropTypesString = string.Empty;
        }

        public List<Type> AllowedDragDropTypes {
            get {
                var types = AllowedDragDropTypesString.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                List<Type> outputTypes = new List<Type>();

                foreach (var type in types) {
                    Type foundType = Type.GetType(type, false, true);
                    if (foundType != null) outputTypes.Add(foundType);
                }

                return outputTypes;
            }
        }

        public string AllowedDragDropTypesString {
            get {
                return (string)GetValue(AllowedDragDropTypesProperty);
            }
            set {
                SetValue(AllowedDragDropTypesProperty, value);
            }
        }

        public ICommand OnDropCommand {
            get {
                return (ICommand)GetValue(OnDropCommandProperty);
            }

            set {
                SetValue(OnDropCommandProperty, value);
            }
        }

        protected override void OnAttached() {
            base.OnAttached();
            //Debug.WriteLine("++ DropIntoBehavior attached to " + AssociatedObject.GetHashCode());
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            //Debug.WriteLine("-- DropIntoBehavior detached from " + AssociatedObject?.GetHashCode() ?? "");
        }

        public bool CanDropInto(Type type) {
            if (!AllowedDragDropTypes.Any()) return true;
            else return AllowedDragDropTypes.Any(t => t.IsAssignableFrom(type));
        }

        public void ExecuteDropInto(DropIntoParameters parameter) {
            if (!CanDropInto(parameter.Source.GetType())) return;
            if (AssociatedObject == null) return;
            if (parameter.Position == null) parameter.Position = DropTargetEnum.Center;
            if (parameter.Target == null) parameter.Target = (AssociatedObject as FrameworkElement).DataContext as ISequenceContainer;
            if (Keyboard.IsKeyDown(Key.LeftAlt)) {
                parameter.Duplicate = true;
            }
            OnDropCommand?.Execute(parameter);
        }
    }
}