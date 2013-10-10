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
using System.Xml.Linq;

namespace LiveSPICE
{
    /// <summary>
    /// Base class for schematic elements.
    /// </summary>
    public class Element : Control
    {
        static Element()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Element), new FrameworkPropertyMetadata(typeof(Element)));
        }

        protected static Pen HighlightPen = new Pen(Brushes.Gray, 0.5f) { DashStyle = DashStyles.Dash };
        protected static Pen SelectedPen = new Pen(Brushes.Blue, 0.5f) { DashStyle = DashStyles.Dash };

        protected Circuit.Element element;

        private Pen pen = null;
        public Pen Pen { get { return pen; } set { pen = value; InvalidateVisual(); } }

        protected bool showTerminals = true;
        public bool ShowTerminals { get { return showTerminals; } set { showTerminals = value; InvalidateVisual(); } }

        protected void OnLayoutChanged(object sender, EventArgs e)
        {
            Circuit.Coord lb = element.LowerBound;
            Circuit.Coord ub = element.UpperBound;

            Canvas.SetLeft(this, lb.x);
            Canvas.SetTop(this, lb.y);

            Width = ub.x - lb.x;
            Height = ub.y - lb.y;            

            InvalidateVisual();
        }

        protected Element(Circuit.Element E)
        {
            element = E;
            element.Tag = this;
            element.LayoutChanged += OnLayoutChanged;

            OnLayoutChanged(null, null);

            UseLayoutRounding = true;

            foreach (Circuit.Terminal i in E.Terminals)
                i.ConnectionChanged += (x, y) => InvalidateVisual();
        }

        public static Element New(Circuit.Element E)
        {
            if (E is Circuit.Wire)
                return new Wire((Circuit.Wire)E);
            else if (E is Circuit.Symbol)
                return new Symbol((Circuit.Symbol)E);
            else
                throw new NotImplementedException();
        }
        
        private List<EventHandler> selectedChanged = new List<EventHandler>();
        public event EventHandler SelectedChanged { add { selectedChanged.Add(value); } remove { selectedChanged.Remove(value); } }

        private bool selected = false;
        public bool Selected 
        {
            get { return selected; } 
            set 
            {
                if (selected == value) return;

                selected = value;
                InvalidateVisual();
                foreach (EventHandler i in selectedChanged)
                    i(this, new EventArgs());
            } 
        }

        private bool highlighted = false;
        public bool Highlighted 
        { 
            get { return highlighted; } 
            set 
            {
                if (highlighted == value) return;
                highlighted = value; 
                InvalidateVisual(); 
            } 
        }
    }
}
