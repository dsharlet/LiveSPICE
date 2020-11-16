using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SchematicControls;

namespace LiveSPICE
{
    /// <summary>
    /// Control that displays a circuit symbol layout.
    /// </summary>
    public class LayoutControl : Control, INotifyPropertyChanged
    {
        static LayoutControl() { DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutControl), new FrameworkPropertyMetadata(typeof(LayoutControl))); }

        private bool showText = true;
        public bool ShowText { get { return showText; } set { showText = value; InvalidateVisual(); NotifyChanged(nameof(ShowText)); } }

        private Circuit.SymbolLayout layout = null;
        public Circuit.SymbolLayout Layout { get { return layout; } set { layout = value; InvalidateVisual(); NotifyChanged(nameof(Layout)); } }

        protected override Size MeasureOverride(Size constraint)
        {
            if (layout == null)
                return base.MeasureOverride(constraint);
            return new Size(
                Math.Min(layout.Width, constraint.Width),
                Math.Min(layout.Height, constraint.Height));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Circuit.Coord center = (layout.LowerBound + layout.UpperBound) / 2;
            double scale = Math.Min(Math.Min(ActualWidth / layout.Width, ActualHeight / layout.Height), 1.0);

            Matrix transform = new Matrix();
            transform.Translate(-center.x, -center.y);
            transform.Scale(scale, -scale);
            transform.Translate(ActualWidth / 2, ActualHeight / 2);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            SymbolControl.DrawLayout(layout, drawingContext, transform, ShowText ? FontFamily : null, FontWeight, FontSize, pixelsPerDip);
        }

        public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(
            "Layout",
            typeof(Circuit.SymbolLayout),
            typeof(LayoutControl),
            new PropertyMetadata(default(Circuit.SymbolLayout), OnComponentPropertyChanged));

        private static void OnComponentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LayoutControl target = d as LayoutControl;
            target.Layout = (Circuit.SymbolLayout)e.NewValue;
        }

        // INotifyPropertyChanged.
        protected void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
