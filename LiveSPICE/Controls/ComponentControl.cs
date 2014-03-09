using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
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
using System.Xml.Linq;
using System.Reflection;

namespace LiveSPICE
{
    /// <summary>
    /// Control that displays a circuit component.
    /// </summary>
    public class ComponentControl : Control, INotifyPropertyChanged
    {
        static ComponentControl() { DefaultStyleKeyProperty.OverrideMetadata(typeof(ComponentControl), new FrameworkPropertyMetadata(typeof(ComponentControl))); }

        private bool showText = true;
        public bool ShowText { get { return showText; } set { showText = value; InvalidateVisual(); } }

        protected Circuit.SymbolLayout layout = null;
        private Circuit.Component component = null;
        public Circuit.Component Component
        {
            get { return component; }
            set
            {
                component = value;
                layout = new Circuit.SymbolLayout();
                component.LayoutSymbol(layout);
                NotifyChanged("Component");
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
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

            SymbolControl.DrawLayout(layout, drawingContext, transform, ShowText ? FontFamily : null, FontWeight, FontSize);
        }

        // INotifyPropertyChanged.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
