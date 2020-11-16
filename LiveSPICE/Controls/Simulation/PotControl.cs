using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
                NotifyChanged(nameof(Value));
            }
        }

        public PotControl()
        {
            MouseMove += OnMouseMove;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;

            //Style = CloneStyle(Application.Current.TryFindResource(ToolBar.ButtonStyleKey) as Style, Control.BorderBrushProperty, Control.BackgroundProperty, Control.ForegroundProperty, Control.BorderThicknessProperty);

            Background = new SolidColorBrush(new Color() { A = 64, R = 192, G = 192, B = 192 });
            BorderBrush = Brushes.Gray;
            Foreground = Brushes.Red;
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

        private static double VectorToValue(Vector dx) { return Math.Atan2(dx.X, -dx.Y) / (Math.PI) * 3 / 5 + 0.5; }
        private static Vector ValueToVector(double V)
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
            Pen pen = new Pen(BorderBrush, 1.0);

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
                        Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    DC.DrawText(label, (Point)(Center + dx * r * 1.15 - new Vector(label.Width, label.Height) * 0.5));
                }
            }
            DrawNotch(DC, new Pen(Foreground, 1.5), ValueToVector(Value), r * 0.7, r * 1.15);
        }

        //private static Style CloneStyle(Style From, params DependencyProperty[] Keys)
        //{
        //    Style clone = new Style(typeof(PotControl));
        //    while (From != null)
        //    {
        //        foreach (Setter j in From.Setters.OfType<Setter>().Where(x => Keys.Contains(x.Property)))
        //            clone.Setters.Add(j);
        //        foreach (Trigger j in From.Triggers.OfType<Trigger>())
        //        {
        //            Trigger t = new Trigger()
        //            {
        //                Property = j.Property,
        //                Value = j.Value,
        //            };
        //            foreach (Setter k in j.Setters.OfType<Setter>().Where(x => Keys.Contains(x.Property)))
        //                t.Setters.Add(k);
        //            if (t.Setters.Any())
        //                clone.Triggers.Add(t);
        //        }
        //        From = From.BasedOn;
        //    }
        //    return clone;
        //}

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
