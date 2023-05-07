using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SchematicControls;

namespace LiveSPICE
{
    public class WireTool : EditorTool
    {
        protected bool wiring = false;
        protected Path path;
        protected List<Circuit.Coord> mouse;

        public WireTool(SchematicEditor Target) : base(Target)
        {
            path = new Path()
            {
                Fill = null,
                Stroke = ElementControl.HighlightPen.Brush,
                StrokeThickness = 1,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden,
                Data = new PathGeometry()
            };
        }

        public override void Begin() { base.Begin(); Target.Overlays.Children.Add(path); Target.Cursor = Cursors.Pen; }
        public override void End() { Target.Overlays.Children.Remove(path); base.End(); }

        public override void MouseDown(Circuit.Coord At)
        {
            mouse = new List<Circuit.Coord>() { At };
            path.Visibility = Visibility.Visible;
        }


        public override void MouseMove(Circuit.Coord At)
        {
            if (mouse == null)
                return;
            mouse.Add(At);
            List<Circuit.Coord> points = Editor.FindWirePath(mouse);
            ((PathGeometry)path.Data).Clear();
            for (int i = 0; i < points.Count - 1; ++i)
            {
                LineGeometry line = new LineGeometry()
                {
                    StartPoint = Target.ToPoint(points[i]),
                    EndPoint = Target.ToPoint(points[i + 1])
                };
                ((PathGeometry)path.Data).AddGeometry(line);
            }
        }

        public override void MouseUp(Circuit.Coord At)
        {
            ((PathGeometry)path.Data).Clear();
            path.Visibility = Visibility.Hidden;
            if (mouse != null)
            {
                Editor.AddWire(Editor.FindWirePath(mouse));
                mouse = null;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                Target.Tool = new SelectionTool(Editor);
        }
    }
}
