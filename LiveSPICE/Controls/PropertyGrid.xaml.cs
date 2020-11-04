using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

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
        private readonly System.Windows.Forms.PropertyGrid properties = new System.Windows.Forms.PropertyGrid();

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
                NotifyChanged(nameof(SelectedObjects));

                if (objects.Length > 1)
                {
                    multi = new ComboBoxItem()
                    {
                        Content = String.Format("({0} objects)", objects.Count()),
                        FontWeight = FontWeights.Bold,
                        Tag = null,
                    };
                }
                else
                {
                    multi = null;
                }
                NotifyChanged(nameof(Objects));

                properties.SelectedObjects = null;
                properties.SelectedObjects = value;
                NotifyChanged(nameof(SubSelectedObject));
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
                    NotifyChanged(nameof(SubSelectedObject));
                }
                else
                {
                    properties.SelectedObject = value;
                    NotifyChanged(nameof(SubSelectedObject));
                }
            }
        }

        public IEnumerable<object> Objects
        {
            get
            {
                if (objects == null) return new object[0];
                List<ComboBoxItem> items = objects.Select(i => new ComboBoxItem() { Content = i.ToString(), Tag = i }).ToList();
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
                old[i] = converter.ConvertFromString(converter.ConvertToString(property.GetValue(i, null)));
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

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

