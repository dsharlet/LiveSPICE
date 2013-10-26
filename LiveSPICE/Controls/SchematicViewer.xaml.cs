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
        protected double MaxZoom = 4;
        protected Point? mouse;

        protected ScaleTransform scale = new ScaleTransform();

        public SchematicControl Schematic 
        { 
            get { return ((SchematicControl)scroll.Content); }
            set 
            { 
                scroll.Content = value;
                if (value != null)
                {
                    value.LayoutTransform = scale;
                    value.UpdateLayout();
                    Zoom = Math.Max(Zoom, MinZoom(ViewportSize));
                    FocusCenter();
                }
            }
        }

        private double LogFloor(double x) { return Math.Pow(2, Math.Floor(Math.Log(x, 2))); }

        private Size ViewportSize { get { return new Size(scroll.ViewportWidth, scroll.ViewportHeight); } }

        private double MinZoom(Size Size) { return LogFloor(Math.Min(
            Size.Width / (Schematic.ActualWidth + 1e-6), 
            Size.Height / (Schematic.ActualHeight + 1e-6))); }

        public double Zoom
        {
            get { return scale.ScaleX; }
            set
            {
                Point focus = mouse.HasValue ? mouse.Value : new Point(scroll.ViewportWidth / 2, scroll.ViewportHeight / 2);
                Point at = TranslatePoint(focus, Schematic);

                double zoom = LogFloor(value);

                scale.ScaleX = scale.ScaleY = Math.Max(Math.Min(zoom, MaxZoom), MinZoom(ViewportSize));
                Schematic.UpdateLayout();

                at = Schematic.TranslatePoint(at, this);
                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + at.X - focus.X);
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + at.Y - focus.Y);
            }
        }

        public SchematicViewer()
        {
            InitializeComponent();
            
            CommandBindings.Add(new CommandBinding(NavigationCommands.Zoom, (o, e) => Zoom *= 2));
            CommandBindings.Add(new CommandBinding(NavigationCommands.DecreaseZoom, (o, e) => Zoom *= 0.5));

            scroll.PreviewMouseWheel += (o, e) =>
            {
                Zoom = LogFloor(Zoom * (e.Delta > 0 ? 2 : 0.5));
                e.Handled = true;
            };
            scroll.PreviewMouseMove += (o, e) => mouse = e.GetPosition(this);
            scroll.MouseLeave += (o, e) => mouse = null;

            SizeChanged += SchematicViewer_SizeChanged;

            Schematic = new SchematicControl(new Circuit.Schematic());
        }
        
        public SchematicViewer(SchematicEditor Schematic) : this()
        {
            this.Schematic = Schematic;
        }

        void SchematicViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Schematic == null)
                return;

            Zoom = Math.Max(Zoom, MinZoom(e.NewSize));
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
                a = Schematic.ToPoint(Schematic.LowerBound());
                b = Schematic.ToPoint(Schematic.UpperBound());
            }
            else
            {
                a = Schematic.ToPoint(new Circuit.Coord(-100, -100));
                b = Schematic.ToPoint(new Circuit.Coord(100, 100));
            }
            FocusRect(a, b, true);
        }
    }
}
