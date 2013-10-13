using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Microsoft.Win32;
using System.Xml.Linq;
using SyMath;

namespace LiveSPICE
{
    /// <summary>
    /// Control for interacting with a Circuit.Schematic.
    /// </summary>
    public partial class Schematic : UserControl, INotifyPropertyChanged
    {
        protected int grid = 10;
        public int Grid { get { return grid; } set { grid = value; NotifyChanged("Grid"); } }

        protected Circuit.Schematic schematic;
        
        public Schematic(Circuit.Schematic Schematic)
        {
            InitializeComponent();

            schematic = Schematic;

            Background = Brushes.LightGray;
            Cursor = Cursors.Cross;
            Width = schematic.Width;
            Height = schematic.Height;

            schematic.Elements.ItemAdded += Elements_ItemAdded;
            schematic.Elements.ItemRemoved += Elements_ItemRemoved;

            foreach (Circuit.Element i in schematic.Elements)
            {
                Element control = Element.New(i);
                components.Children.Add(control);
                
                Circuit.Coord x = i.LowerBound;
                Canvas.SetLeft(control, x.x);
                Canvas.SetTop(control, x.y);
            }
        }

        void Elements_ItemAdded(object sender, Circuit.ElementEventArgs e)
        {
            Element control = Element.New(e.Element);
            components.Children.Add(control);

            Circuit.Coord x = e.Element.LowerBound;
            Canvas.SetLeft(control, x.x);
            Canvas.SetTop(control, x.y);
        }
        void Elements_ItemRemoved(object sender, Circuit.ElementEventArgs e)
        {
            components.Children.Remove((Element)e.Element.Tag);
        }
        
        // Circuit.
        public Circuit.Circuit Build() { return schematic.Build(); }

        // Elements.
        public IEnumerable<Circuit.Element> Elements { get { return schematic.Elements; } }
        public IEnumerable<Circuit.Symbol> Symbols { get { return schematic.Elements.OfType<Circuit.Symbol>(); } }
        public IEnumerable<Circuit.Wire> Wires { get { return schematic.Elements.OfType<Circuit.Wire>(); } }

        public IEnumerable<Circuit.Element> InRect(Circuit.Coord x1, Circuit.Coord x2)
        {
            Circuit.Coord a = new Circuit.Coord(Math.Min(x1.x, x2.x), Math.Min(x1.y, x2.y));
            Circuit.Coord b = new Circuit.Coord(Math.Max(x1.x, x2.x), Math.Max(x1.y, x2.y));
            return Elements.Where(i => i.Intersects(a, b));
        }
        public IEnumerable<Circuit.Element> AtPoint(Circuit.Coord At) { return InRect(At - 1, At + 1); }
        public IEnumerable<Circuit.Element> InRect(Point x1, Point x2) { return InRect(Round(x1), Round(x2)); }
        public IEnumerable<Circuit.Element> AtPoint(Point At) { return AtPoint(Round(At)); }
        
        public static Point LowerBound(IEnumerable<Circuit.Element> Of) { return new Point(Of.Min(i => i.LowerBound.x), Of.Min(i => i.LowerBound.y)); }
        public static Point UpperBound(IEnumerable<Circuit.Element> Of) { return new Point(Of.Max(i => i.UpperBound.x), Of.Max(i => i.UpperBound.y)); }
        public Point LowerBound() { return LowerBound(Elements); }
        public Point UpperBound() { return UpperBound(Elements); }
        
        public Circuit.Point SnapToGrid(Circuit.Point x) { return new Circuit.Point(Math.Round(x.x / Grid) * Grid, Math.Round(x.y / Grid) * Grid); }
        public Point SnapToGrid(Point x) { return new Point(Math.Round(x.X / Grid) * Grid, Math.Round(x.Y / Grid) * Grid); }
        public Vector SnapToGrid(Vector x) { return new Vector(Math.Round(x.X / Grid) * Grid, Math.Round(x.Y / Grid) * Grid); }

        protected static Circuit.Coord Round(Point x) { return new Circuit.Coord((int)Math.Round(x.X), (int)Math.Round(x.Y)); }
        
        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
