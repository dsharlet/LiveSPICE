using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SchematicControls;

namespace LiveSPICE
{
    public class SelectionTool : EditorTool
    {
        protected Circuit.Coord a, b;
        protected Path path;

        public SelectionTool(SchematicEditor Target) : base(Target)
        {
            path = new Path()
            {
                Fill = null,
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 2, 1 },
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden,
                Data = new RectangleGeometry()
            };
        }

        public override ContextMenu BuildContextMenu(Circuit.Coord At)
        {
            Circuit.Element at = Target.AtPoint(At).FirstOrDefault();
            if (at != null && !((ElementControl)at.Tag).Selected)
                Target.ToggleSelect(at);

            ContextMenu menu = new ContextMenu();
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.SelectAll, CommandTarget = Target });
            menu.Items.Add(new Separator());
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.Cut, CommandTarget = Target });
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.Copy, CommandTarget = Target });
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.Paste, CommandTarget = Target });
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.Delete, CommandTarget = Target });
            menu.Items.Add(new Separator());
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.Undo, CommandTarget = Target });
            menu.Items.Add(new MenuItem() { Command = ApplicationCommands.Redo, CommandTarget = Target });
            return menu;
        }

        public override void Begin() { base.Begin(); Target.Overlays.Children.Add(path); Target.Cursor = Cursors.Cross; }
        public override void End() { Target.Overlays.Children.Remove(path); base.End(); }
        public override void Cancel() { path.Visibility = Visibility.Hidden; }

        public override void MouseDoubleClick(Circuit.Coord At)
        {
            Type type = Target.AtPoint(a).OfType<Circuit.Symbol>().Select(i => i.Component.GetType()).FirstOrDefault();
            Target.Select(Target.Symbols.Where(i => i.Component.GetType() == type), false, false);
        }

        private bool Movable(Circuit.Coord At)
        {
            return
                Target.AtPoint(At).Any(i => ((ElementControl)i.Tag).Selected) &&
                (Keyboard.Modifiers & ModifierKeys.Control) == 0;
        }

        public override void MouseDown(Circuit.Coord At)
        {
            if (Movable(At))
            {
                Target.Tool = new MoveTool(Editor, At);
            }
            else
            {
                a = b = At;
                ((RectangleGeometry)path.Data).Rect = new Rect(Target.ToPoint(a), Target.ToPoint(b));
                path.Visibility = Visibility.Visible;
            }
        }

        public override void MouseMove(Circuit.Coord At)
        {
            b = At;
            if (path.Visibility != Visibility.Visible)
            {
                a = b;
                Target.Cursor = Movable(At) ? Cursors.SizeAll : Cursors.Cross;
            }
            else
            {
                Vector dx = new Vector(-0.5, -0.5);
                ((RectangleGeometry)path.Data).Rect = new Rect(Target.ToPoint(a) + dx, Target.ToPoint(b) + dx);
            }
            Target.Highlight(a == b ? Target.AtPoint(a).Take(1) : Target.InRect(a, b));
        }

        public override void MouseUp(Circuit.Coord At)
        {
            b = At;
            if (path.Visibility == Visibility.Visible)
            {
                if (a == b)
                    Target.ToggleSelect(Target.AtPoint(a).FirstOrDefault());
                else
                    Target.Select(Target.InRect(a, b));
                path.Visibility = Visibility.Hidden;
                Target.Cursor = Movable(b) ? Cursors.SizeAll : Cursors.Cross;
            }
        }

        private Circuit.Point GetSelectionCenter()
        {
            Circuit.Coord x1 = SchematicEditor.LowerBound(Target.Selected);
            Circuit.Coord x2 = SchematicEditor.UpperBound(Target.Selected);
            return Target.SnapToGrid((x1 + x2) / 2);
        }

        protected void Rotate(int Delta) { if (Target.Selected.Any()) Editor.Edits.Do(new RotateElements(Target.Selected, Delta, GetSelectionCenter())); }
        protected void Flip() { if (Target.Selected.Any()) Editor.Edits.Do(new FlipElements(Target.Selected, GetSelectionCenter().y)); }

        public override bool KeyDown(KeyEventArgs Event)
        {
            switch (Event.Key)
            {
                case Key.Left: Rotate(1); MouseMove(b); return true;
                case Key.Right: Rotate(-1); MouseMove(b); return true;
                case Key.Down: Flip(); MouseMove(b); return true;
                case Key.Up: Flip(); MouseMove(b); return true;
                default: Target.RaiseEditSelection(Event); return false;
            }
        }

        public override bool KeyUp(KeyEventArgs Event)
        {
            Target.Cursor = Movable(b) ? Cursors.SizeAll : Cursors.Cross;
            return base.KeyUp(Event);
        }
    }
}
