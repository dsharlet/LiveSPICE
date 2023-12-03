using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SchematicControls;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for SchematicViewer.xaml
    /// </summary>
    public partial class SchematicViewer : UserControl
    {
        protected double MaxZoom = 8;
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

        private double MinZoom(Size Size)
        {
            return LogFloor(Math.Min(
                Size.Width / (Schematic.ActualWidth + 1e-6),
                Size.Height / (Schematic.ActualHeight + 1e-6)));
        }

        public double Zoom
        {
            get { return scale.ScaleX; }
            set
            {
                Point focus = mouse ?? new Point(scroll.ViewportWidth / 2, scroll.ViewportHeight / 2);
                Point at = TranslatePoint(focus, Schematic);

                double zoom = value;

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

            CommandBindings.Add(new CommandBinding(NavigationCommands.Zoom, (o, e) => Zoom *= 1.5));
            CommandBindings.Add(new CommandBinding(NavigationCommands.DecreaseZoom, (o, e) => Zoom *= 1 / 1.5));
            CommandBindings.Add(new CommandBinding(Commands.ZoomFit, (o, e) => FocusCenter()));

            scroll.PreviewMouseWheel += (o, e) =>
            {
                Zoom *= e.Delta > 0 ? 1.1 : (1 / 1.1);
                e.Handled = true;
            };
            scroll.PreviewMouseMove += (o, e) => mouse = e.GetPosition(this);
            scroll.MouseLeave += (o, e) => mouse = null;
            scroll.MouseMove += scroll_MouseMove;

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
            if (Schematic.Selected.Any())
            {
                a = Schematic.ToPoint(SchematicControl.LowerBound(Schematic.Selected));
                b = Schematic.ToPoint(SchematicControl.UpperBound(Schematic.Selected));
            }
            else if (Schematic.Elements.Any())
            {
                a = Schematic.ToPoint(Schematic.LowerBound() - 40);
                b = Schematic.ToPoint(Schematic.UpperBound() + 40);
            }
            else
            {
                a = Schematic.ToPoint(new Circuit.Coord(-100, -100));
                b = Schematic.ToPoint(new Circuit.Coord(100, 100));
            }
            FocusRect(a, b, true);
        }

        Point? mouse_scroll = null;
        private void scroll_MouseMove(object o, System.Windows.Input.MouseEventArgs e)
        {
            if (IsMouseCaptureWithin && e.MiddleButton == MouseButtonState.Pressed)
            {
                Point x = e.GetPosition(this);
                if (mouse_scroll.HasValue)
                {
                    Vector dx = x - mouse_scroll.Value;
                    scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset - dx.X);
                    scroll.ScrollToVerticalOffset(scroll.VerticalOffset - dx.Y);
                }
                mouse_scroll = x;
                e.Handled = true;
            } 
            else
            {
                mouse_scroll = null;
            }
        }
    }
}
