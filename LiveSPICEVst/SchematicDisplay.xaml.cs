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
using Circuit;
using SchematicControls;

namespace LiveSPICEVst
{
    /// <summary>
    /// Interaction logic for SchematicDisplay.xaml
    /// </summary>
    public partial class SchematicDisplay : UserControl
    {
        public SchematicDisplay()
        {
            InitializeComponent();

            this.DataContextChanged += SchematicDisplay_DataContextChanged;
            this.SizeChanged += SchematicDisplay_SizeChanged;
        }

        void UpdateScale()
        {
            // Scale the canvas to fit the size of the SchematicDisplay control
            double scale = Math.Min(ActualWidth / SchematicCanvas.Width, ActualHeight / SchematicCanvas.Height);

            SchematicCanvas.LayoutTransform = new ScaleTransform(scale, scale);
        }

        private void SchematicDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScale();
        }

        private void SchematicDisplay_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SchematicCanvas.Children.Clear();

            Schematic schematic = (DataContext as Schematic);

            if (schematic != null)
            {
                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = 0;
                double maxY = 0;

                // Find the bounds of the schematic
                foreach (Element element in schematic.Elements)
                {
                    minX = Math.Min(element.LowerBound.x, minX);
                    maxX = Math.Max(element.UpperBound.x, maxX);
                    minY = Math.Min(element.LowerBound.y, minY);
                    maxY = Math.Max(element.UpperBound.y, maxY);
                }

                SchematicCanvas.Width = (Math.Abs(minX) + Math.Abs(maxX)) * 1.2;
                SchematicCanvas.Height = (Math.Abs(minY) + Math.Abs(maxY)) * 1.2;

                UpdateScale();

                // Offset the origin by the center of the used region to center the schematic
                Circuit.Coord origin = new Circuit.Coord((int)((SchematicCanvas.Width / 2) - ((minX + maxX) / 2)), (int)((SchematicCanvas.Height / 2) - ((minY + maxY) / 2)));

                foreach (Element element in schematic.Elements)
                {
                    ElementControl control = ElementControl.New(element);

                    SchematicCanvas.Children.Add(control);

                    Circuit.Coord lb = element.LowerBound;
                    Circuit.Coord ub = element.UpperBound;

                    Canvas.SetLeft(control, lb.x + origin.x);
                    Canvas.SetTop(control, lb.y + origin.y);

                    control.Width = ub.x - lb.x;
                    control.Height = ub.y - lb.y;
                }
            }
        }
    }
}
