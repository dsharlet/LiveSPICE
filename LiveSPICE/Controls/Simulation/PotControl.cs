using System;
using System.ComponentModel;
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
using SyMath;

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
                Vector dx = e.GetPosition(this) - Center;
                Value = Math.Atan2(dx.X, -dx.Y) / (Math.PI) * 2 / 3 + 0.5;
                CaptureMouse();
                e.Handled = true;
            }
        }

        protected void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                Vector dx = e.GetPosition(this) - Center;
                Value = Math.Atan2(dx.X, -dx.Y) / (Math.PI) * 2 / 3 + 0.5;
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
                Value = Math.Atan2(dx.X, -dx.Y) / (Math.PI) * 2 / 3 + 0.5;
                e.Handled = true;
            }
        }

        private void DrawNotch(DrawingContext DC, Pen Pen, double Th, double r1, double r2)
        {
            Vector dx = new Vector(Math.Sin(Th), -Math.Cos(Th));

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
                double vi = (double)i / n;
                double th = (vi - 0.5) * Math.PI * 3 / 2;
                DrawNotch(DC, pen, th, r * (i == 0 || i == n ? 0.8 : 0.9), r * 1.0);
            }
            double v = (Value - 0.5) * Math.PI * 3 / 2;
            DrawNotch(DC, new Pen(Brushes.Red, 1.5), v, r * 0.7, r * 1.05);
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
