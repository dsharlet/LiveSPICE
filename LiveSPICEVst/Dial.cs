using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace LiveSPICEVst
{
    public class Dial : RangeBase
    {
        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register("DefaultValue", typeof(double),
              typeof(Dial), new PropertyMetadata(default(double)));

        public double DefaultValue
        {
            get { return (double)this.GetValue(DefaultValueProperty); }
            set { this.SetValue(DefaultValueProperty, value); }
        }
        private bool dragging = false;
        private Point dragStart;
        private System.Drawing.Point absDragStart;
        private double valAtStart;

        static Dial()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Dial), new FrameworkPropertyMetadata(typeof(Dial)));
        }

        public Dial()
        {
            this.MouseLeftButtonDown += new MouseButtonEventHandler(Dial_MouseLeftButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(Dial_MouseLeftButtonUp);
            this.MouseMove += new MouseEventHandler(Dial_MouseMove);
            this.MouseDoubleClick += new MouseButtonEventHandler(Dial_MouseDoubleClick);
        }

        void Dial_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
            dragStart = e.GetPosition(this);
            absDragStart = System.Windows.Forms.Cursor.Position;
            valAtStart = this.Value;

            this.CaptureMouse();
            this.Cursor = Cursors.None;

            e.Handled = true;
        }

        void Dial_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();

            e.Handled = true;
        }

        void Dial_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point currentPos = e.GetPosition(this);

                double delta = dragStart.Y - currentPos.Y;
                double pixelScale = (this.Maximum - this.Minimum) / 100;
                double newValue = valAtStart + delta * pixelScale;

                if (newValue > this.Maximum)
                {
                    this.Value = this.Maximum;
                }
                else if (newValue < this.Minimum)
                {
                    this.Value = this.Minimum;
                }
                else
                {
                    this.Value = newValue;
                }

                e.Handled = true;
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            dragging = false;
            System.Windows.Forms.Cursor.Position = absDragStart;
            this.ClearValue(FrameworkElement.CursorProperty);

            base.OnLostMouseCapture(e);
        }

        void Dial_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.Value = DefaultValue;
        }
    }

    public sealed class DialToAngleConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            Dial dial = (Dial)(value[0]);

            double val = (dial.Value - dial.Minimum) / (dial.Maximum - dial.Minimum);

            double angle = 143;

            return -angle + (val * angle * 2);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("DialToAngleConverter.ConvertBack is not supported.");
        }
    }
}
