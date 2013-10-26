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
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden,
                SnapsToDevicePixels = true,
                Data = new PathGeometry()
            };
        }

        public override void Begin() { base.Begin(); Target.overlays.Children.Add(path); Target.Cursor = Cursors.Pen; }
        public override void End() { Target.overlays.Children.Remove(path); base.End(); }

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
                    StartPoint = Target.ToPoint(points[i]) + new Vector(0.5, -0.5),
                    EndPoint = Target.ToPoint(points[i + 1]) + new Vector(0.5, -0.5)
                };
                ((PathGeometry)path.Data).AddGeometry(line);
            }
        }

        public override void MouseUp(Circuit.Coord At)
        {
            ((PathGeometry)path.Data).Clear();
            path.Visibility = Visibility.Hidden;
            Editor.AddWire(Editor.FindWirePath(mouse));
            mouse = null;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                Target.Tool = new SelectionTool(Editor);
        }
    }
}
