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
    public class ProbeSelectionTool : SimulationTool
    {
        protected Point a, b;
        protected Path path;

        public ProbeSelectionTool(SimulationSchematic Target) : base(Target) 
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

        public override void Begin() { base.Begin(); Target.overlays.Children.Add(path); Target.Cursor = Cursors.Cross; }
        public override void End() { Target.overlays.Children.Remove(path); base.End(); }
        public override void Cancel() { path.Visibility = Visibility.Hidden; }
        
        private bool Movable(Point At)
        {
            return 
                ProbesOf(Target.AtPoint(At)).Any(i => ((ElementControl)i.Tag).Selected) &&
                (Keyboard.Modifiers & ModifierKeys.Control) == 0;
        }

        public override void MouseDown(Point At)
        {
            if (Movable(At))
            {
                Target.Tool = new MoveProbeTool(Simulation, At);
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
            Target.Highlight(ProbesOf(a == b ? Target.AtPoint(a) : Target.InRect(a, b)));
        }

        public override void MouseUp(Point At)
        {
            b = At;
            if (path.Visibility == Visibility.Visible)
            {
                if (a == b)
                    Target.ToggleSelect(ProbesOf(Target.AtPoint(a)));
                else
                    Target.Select(ProbesOf(Target.InRect(a, b)));
                path.Visibility = Visibility.Hidden;
                Target.Cursor = Movable(b) ? Cursors.SizeAll : Cursors.Cross;
            }
        }

        private static IEnumerable<Circuit.Element> ProbesOf(IEnumerable<Circuit.Element> Of)
        {
            return Of.OfType<Circuit.Symbol>().Where(i => i.Component is Probe);
        }
    }
}
