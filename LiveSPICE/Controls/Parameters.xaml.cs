using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
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
    public class ParameterChangedEventArgs : EventArgs
    {
        private Circuit.Parameter changed;
        public Circuit.Parameter Changed { get { return changed; } }

        private double value;
        public double Value { get { return value; } }

        public ParameterChangedEventArgs(Circuit.Parameter Changed, double Value)
        {
            changed = Changed;
            value = Value;
        }
    }


    /// <summary>
    /// Interaction logic for Parameters.xaml
    /// </summary>
    public partial class Parameters : UserControl, INotifyPropertyChanged
    {
        public Parameters()
        {
            InitializeComponent();
        }

        public delegate void ParameterChangedEventHandler(object sender, ParameterChangedEventArgs e);

        private List<ParameterChangedEventHandler> changed = new List<ParameterChangedEventHandler>();
        protected void OnParameterChanged(Circuit.Parameter Changed, double Value)
        {
            ParameterChangedEventArgs args = new ParameterChangedEventArgs(Changed, Value);
            foreach (ParameterChangedEventHandler i in changed)
                i(this, args);
        }
        public event ParameterChangedEventHandler ParameterChanged
        {
            add { changed.Add(value); }
            remove { changed.Remove(value); }
        }

        private UIElement CreateControl(Circuit.Parameter Parameter)
        {
            if (Parameter is Circuit.RangeParameter)
            {
                Circuit.RangeParameter P = (Circuit.RangeParameter)Parameter;

                StackPanel control = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Horizontal };
                Slider slider = new Slider() { Width = 120, Minimum = 0, Maximum = 1, Value = P.Default };
                TextBlock value = new TextBlock() { Width = 30, Text = P.Default.ToString() };
                slider.ValueChanged += (o, e) => { value.Text = e.NewValue.ToString("G2"); OnParameterChanged(Parameter, e.NewValue); };
                control.Children.Add(slider);
                control.Children.Add(value);
                return control;
            }
            else
            {
                throw new NotImplementedException("Unknown parameter type");
            }
        }

        public void UpdateControls(IEnumerable<Circuit.Parameter> Parameters)
        {
            foreach (Circuit.Parameter i in Parameters)
            {
                StackPanel row = new StackPanel() { Orientation = Orientation.Horizontal, Tag = i, Margin = new Thickness(4) };
                row.Children.Add(new TextBlock() { Text = i.Name.ToString(), Width = 60, TextAlignment = TextAlignment.Right });
                row.Children.Add(CreateControl(i));
                controls.Children.Add(row);
            }
        }

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
