using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;
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
using System.Xml.Linq;
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

        private static IEnumerable<Circuit.Component> LoadLibrary(string Library)
        {
            List<Circuit.Component> components = new List<Circuit.Component>();
            try
            {
                XDocument doc = XDocument.Load(Library);
                foreach (XElement i in doc.Element("Components").Elements("Component"))
                    components.Add(Circuit.Component.Deserialize(i));
                return components;
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error loading component library '" + Library + "': " + Ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new Circuit.Component[0];
            }
        }

        private static IEnumerable<Circuit.Component> LoadLibraries(string Library)
        {
            foreach (string i in System.IO.Directory.GetFiles(Library, "*.xml"))
                foreach (Circuit.Component j in LoadLibrary(i))
                    yield return j;
        }

        private static List<Circuit.Component> LoadStandardLibraries()
        {
            // If the app is installed, the components should be in CommonDocuments. Otherwise, look for them in the source paths.
            try
            {
                return LoadLibraries(System.IO.Path.Combine(App.Current.CommonDocuments.FullName, "Components")).ToList();
            }
            catch (Exception)
            {
                return LoadLibraries(@"..\..\..\Components").ToList();
            }
        }

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
                    try { C = (Circuit.Component)Activator.CreateInstance(i); }
                    catch (Exception) { continue; }

                    yield return C;
                }

                // Load the component libraries and enumerate them.
                foreach (Circuit.Component i in LoadStandardLibraries())
                    yield return i;
                foreach (Circuit.Component i in LoadLibraries(System.IO.Path.Combine(App.Current.UserDocuments.FullName, "Components")))
                    yield return i;
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

        private List<RoutedEventHandler> componentClick = new List<RoutedEventHandler>();
        public event RoutedEventHandler ComponentClick
        {
            add { componentClick.Add(value); LoadComponents(); }
            remove { componentClick.Remove(value); }
        }

        private void OnComponentClick(object sender, RoutedEventArgs e)
        {
            foreach (RoutedEventHandler i in componentClick)
                i(sender, e);
        }

        private void LoadComponents()
        {
            categories.Children.Clear();
            filtered.Children.Clear();

            List<Circuit.Component> components = Components.OrderBy(i => i.GetDisplayName()).ToList();

            Panel common = AddCategory("Common");
            foreach (Circuit.Component i in components.Where(i => CommonTypes.Contains(i.GetType())))
                AddItem(common, i, CommandBindings);

            foreach (Circuit.Component i in components)
                AddItem(AddCategory(i.GetCategory()), i, Window.GetWindow(this).CommandBindings);

            foreach (Circuit.Component i in components)
                AddItem(filtered, i, null);
        }

        private void Refresh_Click(object sender, EventArgs e) { LoadComponents(); }
        
        private void AddItem(Panel Group, Circuit.Component C, CommandBindingCollection CommandBindings)
        {
            try
            {
                string DisplayName = C.GetDisplayName();

                StackPanel content = new StackPanel() { Orientation = Orientation.Horizontal };

                // Add image to the button.
                ComponentControl symbol = new ComponentControl(C)
                {
                    Width = 16,
                    Height = 16,
                    ShowText = false,
                    Margin = new Thickness(1),
                };
                content.Children.Add(symbol);

                TextBlock name = new TextBlock()
                {
                    Text = DisplayName,
                    Width = 96,
                    Margin = new Thickness(3, 0, 3, 0),
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
                button.Click += OnComponentClick;

                Group.Children.Add(button);

                // Bind input gestures to a command and add it to the command bindings.
                KeyGesture[] keys;
                if (CommandBindings != null && ShortcutKeys.TryGetValue(C.GetType(), out keys))
                {
                    button.ToolTip = (string)button.ToolTip + " (" + keys.Select(j => j.GetDisplayStringForCulture(CultureInfo.CurrentCulture)).UnSplit(", ") + ")";

                    if (!CommandBindings.OfType<RoutedCommand>().Any(i => i.OwnerType == GetType() && i.Name == DisplayName))
                    {
                        RoutedCommand command = new RoutedCommand(DisplayName, GetType());
                        command.InputGestures.AddRange(keys);

                        CommandBinding binding = new CommandBinding(command, (o, e) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
                        CommandBindings.Add(binding);
                    }
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
