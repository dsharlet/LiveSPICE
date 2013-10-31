using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public class PropertyValueChangedEventArgs : System.Windows.Forms.PropertyValueChangedEventArgs
    {
        private Dictionary<object, object> old;
        public IDictionary<object, object> OldValues { get { return old; } }

        public PropertyValueChangedEventArgs(System.Windows.Forms.GridItem ChangedItem, Dictionary<object, object> OldValues) : base(ChangedItem, null) { old = OldValues; }
    }

    /// <summary>
    /// Wraps System.Windows.Forms.PropertyGrid for use in WPF, plus some improvements.
    /// </summary>
    public partial class PropertyGrid : UserControl
    {
        private System.Windows.Forms.PropertyGrid properties = new System.Windows.Forms.PropertyGrid();
        
        public PropertyGrid()
        {
            InitializeComponent();

            properties.PropertyValueChanged += OnPropertyValueChanged;
            properties.SelectedGridItemChanged += OnSelectedGridItemChanged;
            Content = new System.Windows.Forms.Integration.WindowsFormsHost() 
            { 
                Child = properties
            };
        }

        public new void Focus()
        {
            properties.Focus();
        }

        public object[] SelectedObjects { get { return properties.SelectedObjects; } set { properties.SelectedObjects = value; } }

        private Dictionary<object, object> old;

        void OnSelectedGridItemChanged(object sender, System.Windows.Forms.SelectedGridItemChangedEventArgs e)
        {
            PropertyInfo property = properties.SelectedObjects.First().GetType().GetProperty(e.NewSelection.PropertyDescriptor.Name);

            TypeConverter converter = e.NewSelection.PropertyDescriptor.Converter;

            old = new Dictionary<object, object>();
            foreach (object i in properties.SelectedObjects)
                // Janky serialize...but if it works for the PropertyGrid, it works for us.
                old[i] = converter.ConvertFromString(converter.ConvertToString(property.GetValue(i)));
        }

        public delegate void PropertyValueChangedHandler(object sender, PropertyValueChangedEventArgs e);

        private List<PropertyValueChangedHandler> propertyValueChanged = new List<PropertyValueChangedHandler>();
        public event PropertyValueChangedHandler PropertyValueChanged
        {
            add { propertyValueChanged.Add(value); }
            remove { propertyValueChanged.Remove(value); }
        }

        void OnPropertyValueChanged(object s, System.Windows.Forms.PropertyValueChangedEventArgs e)
        {
            PropertyValueChangedEventArgs args = new PropertyValueChangedEventArgs(e.ChangedItem, old);
            foreach (PropertyValueChangedHandler i in propertyValueChanged)
                i(this, args);
        }
    }
}

