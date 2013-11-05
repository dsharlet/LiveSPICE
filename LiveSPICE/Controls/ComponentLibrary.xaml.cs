using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.ComponentModel;
using SyMath;

namespace LiveSPICE
{
    class ComponentButton : Button
    {
        static ComponentButton() { DefaultStyleKeyProperty.OverrideMetadata(typeof(ComponentButton), new FrameworkPropertyMetadata(typeof(ComponentButton))); }

        public Circuit.Component Component { get { return (Circuit.Component)Tag; } set { Tag = value; } }
    }

    /// <summary>
    /// Interaction logic for ComponentLibrary.xaml
    /// </summary>
    public partial class ComponentLibrary : UserControl, INotifyPropertyChanged
    {
        private static List<Type> CommonTypes = new List<Type>()
        {
            typeof(Circuit.Conductor),
            typeof(Circuit.Ground),
            typeof(Circuit.Resistor),
            typeof(Circuit.Capacitor),
            typeof(Circuit.Inductor),
            typeof(Circuit.VoltageSource),
            typeof(Circuit.NamedWire),
            typeof(Circuit.Label)
        };

        private static Dictionary<Type, KeyGesture[]> ShortcutKeys = new Dictionary<Type, KeyGesture[]>()
        {
            { typeof(Circuit.Conductor), new[] { new KeyGesture(Key.W, ModifierKeys.Control) } },
            { typeof(Circuit.Ground), new[] { new KeyGesture(Key.G, ModifierKeys.Control) } },
            { typeof(Circuit.Resistor), new[] { new KeyGesture(Key.R, ModifierKeys.Control) } },
            { typeof(Circuit.Capacitor), new[] { new KeyGesture(Key.F, ModifierKeys.Control) } },
            { typeof(Circuit.Inductor), new[] { new KeyGesture(Key.L, ModifierKeys.Control), new KeyGesture(Key.H, ModifierKeys.Control) } },
            { typeof(Circuit.Label), new[] { new KeyGesture(Key.T, ModifierKeys.Control) } },
        };
        
        private static IEnumerable<Circuit.Component> Components
        {
            get
            {
                Type root = typeof(Circuit.Component);
                IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(i => i.GetTypes().Where(j => j.IsPublic && !j.IsAbstract && root.IsAssignableFrom(j)));

                foreach (Type i in types)
                {
                    // Enumerate the component itself.
                    Circuit.Component C;
                    try { C = (Circuit.Component)Activator.CreateInstance(i); } catch (Exception) { continue; }

                    yield return C;

                    // Enumerate any part definitions for this component.
                    IEnumerable<Circuit.Component> parts;
                    try { parts = (IEnumerable<Circuit.Component>)i.GetField("Parts").GetValue(null); } catch (Exception) { continue; }

                    foreach (Circuit.Component j in parts)
                        yield return j;
                }
            }
        }

        public ComponentLibrary()
        {
            InitializeComponent();
        }

        private string filter = "";
        public string Filter
        {
            get { return filter; }
            set
            {
                filter = value;
                string f = filter.ToUpper();
                if (f == "")
                {
                    categories.Visibility = Visibility.Visible;
                    filtered.Visibility = Visibility.Hidden;
                }
                else
                {
                    categories.Visibility = Visibility.Hidden;
                    filtered.Visibility = Visibility.Visible;

                    f = f.ToUpper();

                    foreach (ComponentButton i in filtered.Children)
                        i.Visibility = i.Component.GetDisplayName().ToUpper().IndexOf(f) != -1 ? Visibility.Visible : Visibility.Collapsed;
                }
                NotifyChanged("Filter");
            }
        }
                
        public void Init(RoutedEventHandler OnClick, CommandBindingCollection CommandBindings)
        {
            List<Circuit.Component> components = Components.OrderBy(i => i.GetDisplayName()).ToList();

            Panel common = AddCategory("Common");
            foreach (Circuit.Component i in components.Where(i => CommonTypes.Contains(i.GetType())))
                AddItem(common, i, OnClick, CommandBindings);

            foreach (Circuit.Component i in components)
                AddItem(AddCategory(i.GetCategory()), i, OnClick, CommandBindings);

            foreach (Circuit.Component i in components)
                AddItem(filtered, i, OnClick, null);
        }
        
        private void AddItem(Panel Group, Circuit.Component C, RoutedEventHandler OnClick, CommandBindingCollection CommandBindings)
        {
            try
            {
                StackPanel content = new StackPanel() { Orientation = Orientation.Horizontal };

                // Add image to the button.
                ComponentControl symbol = new ComponentControl(C)
                {
                    Width = 16,
                    Height = 16,
                    ShowText = false,
                    Margin = new Thickness(2),
                };
                content.Children.Add(symbol);

                TextBlock name = new TextBlock()
                {
                    Text = C.GetDisplayName(),
                    Width = 96,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Normal,
                    FontSize = FontSize,
                };
                content.Children.Add(name);

                ComponentButton button = new ComponentButton()
                {
                    Component = C,
                    ToolTip = C.GetDescription(),
                    Content = content,
                    BorderBrush = null,
                };
                button.Click += OnClick;

                Group.Children.Add(button);

                // Bind input gestures to a command and add it to the command bindings.
                KeyGesture[] keys;
                if (CommandBindings != null && ShortcutKeys.TryGetValue(C.GetType(), out keys))
                {
                    RoutedCommand command = new RoutedCommand();
                    command.InputGestures.AddRange(keys);

                    button.ToolTip = (string)button.ToolTip + " (" + keys.Select(j => j.GetDisplayStringForCulture(CultureInfo.CurrentCulture)).UnSplit(", ") + ")";

                    CommandBinding binding = new CommandBinding(command, (o, e) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
                    CommandBindings.Add(binding);
                }
            }
            catch (System.Exception) { }
        }
        
        private Panel AddCategory(string Category)
        {
            if (Category == "")
                Category = "Uncategorized";

            IEnumerable<Expander> expanders = categories.Children.OfType<Expander>();
            Expander category = expanders.SingleOrDefault(i => (string)i.Header == Category);
            if (category == null)
            {
                category = new Expander()
                {
                    Header = Category,
                    Content = new WrapPanel(),
                    Focusable = false,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    IsExpanded = !expanders.Any(),
                };
                categories.Children.Add(category);
            }

            return (Panel)category.Content;
        }
        
        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
