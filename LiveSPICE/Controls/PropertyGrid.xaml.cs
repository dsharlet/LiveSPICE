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
    public partial class PropertyGrid : UserControl, INotifyPropertyChanged
    {
        private System.Windows.Forms.PropertyGrid properties = new System.Windows.Forms.PropertyGrid();
        
        public PropertyGrid()
        {
            InitializeComponent();

            properties.PropertyValueChanged += OnPropertyValueChanged;
            properties.SelectedGridItemChanged += OnSelectedGridItemChanged;

            content.Children.Add(new System.Windows.Forms.Integration.WindowsFormsHost() { Child = properties });
        }

        public new void Focus()
        {
            properties.Focus();
        }

        private ComboBoxItem multi;
        private object[] objects;

        public object[] SelectedObjects 
        { 
            get { return objects; } 
            set
            {
                objects = value;
                NotifyChanged("SelectedObjects");

                if (objects.Length > 1)
                {
                    multi = new ComboBoxItem()
                    {
                        Content = String.Format("{0} x {1}", objects.Count(), GetCommonBaseClass(objects.Select(i => i.GetType())).Name),
                        FontWeight = FontWeights.Bold,
                        Tag = null,
                    };
                }
                else
                {
                    multi = null;
                }
                NotifyChanged("Objects");

                properties.SelectedObjects = value;
                NotifyChanged("SubSelectedObject");
            }
        }

        public object SubSelectedObject 
        { 
            get { return multi != null && properties.SelectedObjects.Length > 1 ? null : properties.SelectedObject; } 
            set 
            {
                if (value == null)
                {
                    properties.SelectedObjects = objects;
                    NotifyChanged("SubSelectedObject");
                }
                else
                {
                    properties.SelectedObject = value;
                    NotifyChanged("SubSelectedObject");
                }
            } 
        }

        public IEnumerable<object> Objects
        {
            get
            {
                if (objects == null) return new object[0];
                List<ComboBoxItem> items = objects.Select(i => new ComboBoxItem() { Content = i.GetType().Name + " " + i.ToString(), Tag = i }).ToList();
                if (multi != null)
                    return new[] { multi }.Concat(items);
                else
                    return items;
            }
        }

        private Dictionary<object, object> old;

        void OnSelectedGridItemChanged(object sender, System.Windows.Forms.SelectedGridItemChangedEventArgs e)
        {
            old = new Dictionary<object, object>();
            if (e.NewSelection.PropertyDescriptor == null)
                return;

            PropertyInfo property = properties.SelectedObjects.First().GetType().GetProperty(e.NewSelection.PropertyDescriptor.Name);

            TypeConverter converter = e.NewSelection.PropertyDescriptor.Converter;

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

        public static Type GetCommonBaseClass(IEnumerable<Type> Types)
        {
            Type ret = Types.First();

            foreach (Type i in Types.Skip(1))
            {
                if (i.IsAssignableFrom(ret))
                {
                    ret = i;
                }
                else
                {
                    while (!ret.IsAssignableFrom(i))
                        ret = ret.BaseType;
                }
            }

            return ret;
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

