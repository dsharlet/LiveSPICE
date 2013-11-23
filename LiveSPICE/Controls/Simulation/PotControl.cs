using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
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
    class PotControl : Control, INotifyPropertyChanged
    {
        static PotControl() { DefaultStyleKeyProperty.OverrideMetadata(typeof(PotControl), new FrameworkPropertyMetadata(typeof(PotControl))); }

        private double value = 0.5;
        public double Value
        {
            get { return Math.Max(Math.Min(value, 1.0), 0.0); }
            set
            {
                this.value = value;
                InvalidateVisual();
                RaiseValueChanged(Value);
                NotifyChanged("Value");
            }
        }

        public PotControl()
        {
            MouseMove += OnMouseMove;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;

            Background = new SolidColorBrush(new Color() { A = 64, R = 0, G = 0, B = 0 });
        }

        private List<Action<double>> valueChanged = new List<Action<double>>();
        protected void RaiseValueChanged(double x) { foreach (Action<double> i in valueChanged) i(x); }
        public event Action<double> ValueChanged { add { valueChanged.Add(value); } remove { valueChanged.Remove(value); } }

        private Point Center { get { return new Point(ActualWidth / 2, ActualHeight / 2); } }

        protected void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                CaptureMouse();
                Value = VectorToValue(e.GetPosition(this) - Center);
                e.Handled = true;
            }
        }

        protected void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                Value = VectorToValue(e.GetPosition(this) - Center);
                InvalidateVisual();
                e.Handled = true;
            }
        }

        protected void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ReleaseMouseCapture();

                Vector dx = e.GetPosition(this) - Center;
                Value = VectorToValue(dx);
                e.Handled = true;
            }
        }

        private double VectorToValue(Vector dx) { return Math.Atan2(dx.X, -dx.Y) / (Math.PI) * 3 / 5 + 0.5; }
        private Vector ValueToVector(double V)
        {
            double th = (V - 0.5) * Math.PI * 5 / 3;
            return new Vector(Math.Sin(th), -Math.Cos(th));
        }

        private void DrawNotch(DrawingContext DC, Pen Pen, Vector dx, double r1, double r2)
        {
            Point c = Center;
            DC.DrawLine(Pen, (Point)(c + dx * r1), (Point)(c + dx * r2));
        }

        protected override void OnRender(DrawingContext DC)
        {
            Pen pen = new Pen(Brushes.Black, 1.0);

            int n = 10;
            double r = Math.Min(ActualWidth, ActualHeight) / 2;
            DC.DrawEllipse(Background, pen, Center, r, r);

            for (int i = 0; i <= n; ++i)
            {
                double v = (double)i / n;
                Vector dx = ValueToVector(v);

                DrawNotch(DC, pen, dx, r * 0.9, r * 1.0);
                if (i == 0 || i == n)
                {
                    FormattedText label = new FormattedText(
                        v.ToString("G3"),
                        CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal), FontSize,
                        Brushes.Black);

                    DC.DrawText(label, (Point)(Center + dx * r * 1.15 - new Vector(label.Width, label.Height) * 0.5));
                }
            }
            DrawNotch(DC, new Pen(Brushes.Red, 1.5), ValueToVector(Value), r * 0.7, r * 1.15);
        }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
