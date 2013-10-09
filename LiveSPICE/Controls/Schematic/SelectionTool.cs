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

namespace LiveSPICE
{
    public class SelectionTool : SchematicTool
    {
        protected Point a, b;
        protected Path path;

        public SelectionTool(Schematic Target) : base(Target) 
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
                SnapsToDevicePixels = true,
                Data = new RectangleGeometry()
            };
        }

        public override void Begin() { Target.overlays.Children.Add(path); Target.Cursor = Cursors.Cross; }
        public override void End() { Target.overlays.Children.Remove(path); }
        public override void Cancel() { path.Visibility = Visibility.Hidden; }

        protected IEnumerable<Circuit.Element> InRect(Point A, Point B) { return A == B ? Target.AtPoint(A) : Target.InRect(A, B); }

        //public override void MouseDoubleClick(Point At, Symbol On)
        //{
        //    if (On != null)
        //        Target.Select(Target.Symbols.Where(i => i.Component.GetType() == On.Component.GetType()));
        //    else
        //        Target.Select();
        //}

        private bool Movable(Point At)
        {
            return 
                Target.AtPoint(At).Any(i => ((Element)i.Tag).Selected) &&
                (Keyboard.Modifiers & ModifierKeys.Control) == 0;
        }

        public override void MouseDown(Point At)
        {
            if (Movable(At))
            {
                Target.Tool = new MoveTool(Target, At);
            }
            else
            {
                a = b = At;
                ((RectangleGeometry)path.Data).Rect = new Rect(a, b);
                path.Visibility = Visibility.Visible;
            }
        }

        public override void MouseMove(Point At)
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
                ((RectangleGeometry)path.Data).Rect = new Rect(a + dx, b + dx);
            }
            Target.Highlight(InRect(a, b));
        }

        public override void MouseUp(Point At)
        {
            b = At;
            if (path.Visibility == Visibility.Visible)
            {
                if (a == b)
                    Target.ToggleSelect(InRect(a, b));
                else
                    Target.Select(InRect(a, b));
                path.Visibility = Visibility.Hidden;
                Target.Cursor = Movable(b) ? Cursors.SizeAll : Cursors.Cross;
            }
        }

        private Circuit.Point GetSelectionCenter()
        {
            Point x1 = Schematic.LowerBound(Target.Selected);
            Point x2 = Schematic.UpperBound(Target.Selected);
            Point x = Target.SnapToGrid((Point)(((Vector)x1 + (Vector)x2) / 2));
            return new Circuit.Point(x.X, x.Y);
        }

        protected void Rotate(int Delta) { if (Target.Selected.Any()) Target.Edits.Do(new RotateElements(Target.Selected, Delta, GetSelectionCenter())); }
        protected void Flip() { if (Target.Selected.Any()) Target.Edits.Do(new FlipElements(Target.Selected, GetSelectionCenter().y)); }

        public override bool KeyDown(Key Key)
        {
            switch (Key)
            {
                case System.Windows.Input.Key.Left: Rotate(1); return true;
                case System.Windows.Input.Key.Right: Rotate(-1); return true;
                case System.Windows.Input.Key.Down: Flip(); return true;
                case System.Windows.Input.Key.Up: Flip(); return true;
                default: Target.Cursor = Movable(b) ? Cursors.SizeAll : Cursors.Cross; return base.KeyDown(Key);
            }
        }

        public override bool KeyUp(Key Key)
        {
            Target.Cursor = Movable(b) ? Cursors.SizeAll : Cursors.Cross; 
            return base.KeyUp(Key);
        }
    }
}
