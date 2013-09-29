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
    public class WireTool : SchematicTool
    {
        protected bool wiring = false;
        protected Path path;
        protected List<Point> mouse;

        public WireTool(Schematic Target) : base(Target)
        {
            path = new Path()
            {
                Fill = null,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Hidden,
                SnapsToDevicePixels = true,
                Data = new PathGeometry()
            };
        }

        public override void Begin() { Target.overlays.Children.Add(path); Target.Cursor = Cursors.Cross; }
        public override void End() { Target.overlays.Children.Remove(path); }

        public override void MouseDown(Point At)
        {
            mouse = new List<Point>() { At };
            path.Visibility = Visibility.Visible;
        }

        public override void MouseMove(Point At)
        {
            if (mouse == null)
                return;
            mouse.Add(At);
            List<Point> points = Target.FindWirePath(mouse);
            ((PathGeometry)path.Data).Clear();
            for (int i = 0; i < points.Count - 1; ++i)
            {
                LineGeometry line = new LineGeometry()
                {
                    StartPoint = points[i] + new Vector(0.5, -0.5),
                    EndPoint = points[i + 1] + new Vector(0.5, -0.5)
                };
                ((PathGeometry)path.Data).AddGeometry(line);
            }
        }

        public override void MouseUp(Point At)
        {
            ((PathGeometry)path.Data).Clear();
            path.Visibility = Visibility.Hidden;
 	        Target.AddWire(Target.FindWirePath(mouse));
            mouse = null;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                Target.Tool = null;
        }
    }
}
