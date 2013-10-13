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
    /// <summary>
    /// Interaction logic for SchematicViewer.xaml
    /// </summary>
    public partial class SchematicViewer : UserControl
    {
        protected double ZoomMax = 4;
        protected Point? mouse;

        protected ScaleTransform scale = new ScaleTransform();

        public SchematicEditor Schematic { get { return ((SchematicEditor)scroll.Content); } set { scroll.Content = value; } }

        private double LogFloor(double x, double b) { return Math.Pow(b, Math.Floor(Math.Log(x, b))); }

        public double Zoom
        {
            get { return scale.ScaleX; }
            set
            {
                Point focus = mouse.HasValue ? mouse.Value : new Point(scroll.ViewportWidth / 2, scroll.ViewportHeight / 2);
                Point at = TranslatePoint(focus, Schematic);

                double zoom = LogFloor(value, 2);

                double minZoom = LogFloor(Math.Min(scroll.ViewportWidth / (Schematic.ActualWidth + 1e-6), scroll.ViewportHeight / (Schematic.ActualHeight + 1e-6)), 2);

                scale.ScaleX = scale.ScaleY = Math.Max(Math.Min(zoom, ZoomMax), minZoom);
                Schematic.UpdateLayout();

                at = Schematic.TranslatePoint(at, this);
                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + at.X - focus.X);
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + at.Y - focus.Y);
            }
        }
        
        public SchematicViewer(SchematicEditor Schematic)
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(NavigationCommands.Zoom, (o, e) => Zoom *= 2));
            CommandBindings.Add(new CommandBinding(NavigationCommands.DecreaseZoom, (o, e) => Zoom *= 0.5));

            this.Schematic = Schematic != null ? Schematic : new SchematicEditor();

            scroll.PreviewMouseWheel += (o, e) => 
            {
                Zoom = LogFloor(Zoom * (e.Delta > 0 ? 2 : 0.5), 2);
                e.Handled = true; 
            };
            scroll.PreviewMouseMove += (o, e) => mouse = e.GetPosition(this);
            scroll.MouseLeave += (o, e) => mouse = null;

            Schematic.LayoutTransform = scale;

            SizeChanged += SchematicViewer_SizeChanged;
        }

        void SchematicViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Zoom = Zoom;
            if (e.PreviousSize.Width * e.PreviousSize.Height < 1)
            {
                FocusCenter();
            }
            else
            {
                Point a = TranslatePoint(new Point(0, 0), Schematic);
                Point b = TranslatePoint((Point)e.PreviousSize - new Vector(e.NewSize.Width - scroll.ViewportWidth, e.NewSize.Height - scroll.ViewportHeight), Schematic);
                FocusRect(a, b, false);
            }
        }
        
        public void FocusRect(Point a, Point b, bool AllowZoom)
        {
            if (AllowZoom)
                Zoom = Math.Min(scroll.ViewportWidth / (b.X - a.X + 20), scroll.ViewportHeight / (b.Y - a.Y + 20));
            scroll.ScrollToHorizontalOffset(Zoom * (a.X + b.X) / 2 - scroll.ViewportWidth / 2);
            scroll.ScrollToVerticalOffset(Zoom * (a.Y + b.Y) / 2 - scroll.ViewportHeight / 2);
        }

        public void FocusCenter()
        {
            Point a, b;
            if (Schematic.Elements.Any())
            {
                a = Schematic.LowerBound();
                b = Schematic.UpperBound();
            }
            else
            {
                a = new Point(Schematic.ActualWidth / 2 - 100, Schematic.ActualHeight / 2 - 100);
                b = new Point(Schematic.ActualWidth / 2 + 100, Schematic.ActualHeight / 2 + 100);
            }
            FocusRect(a, b, true);
        }
    }
}
