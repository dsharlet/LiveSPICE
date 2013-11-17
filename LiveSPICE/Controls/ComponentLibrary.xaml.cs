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

        public ComponentButton(Circuit.Component C)
        {
            StackPanel content = new StackPanel() { Orientation = Orientation.Horizontal };

            // Add image to the button.
            content.Children.Add(new ComponentControl(C)
            {
                Width = 16,
                Height = 16,
                ShowText = false,
                Margin = new Thickness(1),
            });

            content.Children.Add(new TextBlock()
            {
                Text = C.GetDisplayName(),
                Width = 96,
                Margin = new Thickness(3, 0, 3, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Normal,
                FontSize = FontSize,
            });

            Tag = C;
            ToolTip = C.GetDescription() != "" ? C.GetDescription() : null;
            Content = content;
            BorderBrush = null;
        }

        public Circuit.Component Component { get { return (Circuit.Component)Tag; } }
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

        private static IEnumerable<Circuit.Component> LoadLibrary(string Library, string Category)
        {
            try
            {
                XDocument doc = XDocument.Load(Library);
                List<Circuit.Component> ret = new List<Circuit.Component>();
                if (doc.Element("Components") != null)
                {
                    foreach (XElement i in doc.Element("Components").Elements("Component"))
                        ret.Add(Circuit.Component.Deserialize(i));
                }
                else if (doc.Element("Schematic") != null)
                {
                    Circuit.Schematic S = Circuit.Schematic.Deserialize(doc.Element("Schematic"));
                    if (S.Circuit.Category == "")
                        S.Circuit.Category = Category;
                    ret.Add(S.Build());
                }
                return ret;
            }
            catch (System.Xml.XmlException)
            {
                try
                {
                    Circuit.Spice.Statements directives = new Circuit.Spice.Statements(Library);

                    return directives.OfType<Circuit.Component>();
                }
                catch (Exception Ex)
                {
                    //Log.WriteLine(Circuit.MessageType.Error, "Error loading component library '{0}': {1}", Library, Ex.Message);
                    return new Circuit.Component[0];
                }
            }
            catch (Exception Ex)
            {
                //Log.WriteLine(Circuit.MessageType.Error, "Error loading component library '{0}': {1}", Library, Ex.Message);
                return new Circuit.Component[0];
            }
        }

        private static IEnumerable<Circuit.Component> LoadLibrary(string Library) { return LoadLibrary(Library, "Uncategorized"); }

        private static List<Circuit.Component> LoadStandardLibraries()
        {
            // Look next to the app, or up a bit.
            string app = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            List<Circuit.Component> lib = GetFiles(System.IO.Path.Combine(app, "Components"), "*.xml").SelectMany(i => LoadLibrary(i)).ToList();
            if (lib.Count == 0)
                lib = GetFiles(System.IO.Path.Combine(app, @"..\..\..\Components"), "*.xml").SelectMany(i => LoadLibrary(i)).ToList();

            return lib;
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

                    if (C.IsImplemented)
                        yield return C;
                }

                // Load the component libraries and enumerate them.
                foreach (Circuit.Component i in LoadStandardLibraries())
                    yield return i;

                string docs = App.Current.UserDocuments.FullName;
                foreach (Circuit.Component i in GetFiles(System.IO.Path.Combine(docs, "Components"), "*").SelectMany(i => LoadLibrary(i)))
                    yield return i;
            }
        }
        
        public ComponentLibrary()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            InitShortcutKeys(Window.GetWindow(this).CommandBindings);
            
            LoadComponents();
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

        private List<Action<Circuit.Component>> componentClick = new List<Action<Circuit.Component>>();
        public event Action<Circuit.Component> ComponentClick
        {
            add { componentClick.Add(value); }
            remove { componentClick.Remove(value); }
        }
        private void RaiseComponentClick(Circuit.Component C)
        {
            foreach (Action<Circuit.Component> i in componentClick)
                i(C);
        }
        private void OnComponentClick(object sender, RoutedEventArgs e) { RaiseComponentClick(((ComponentButton)sender).Component); }


        private void LoadComponents()
        {
            categories.Children.Clear();
            filtered.Children.Clear();

            List<Circuit.Component> components = Components.OrderBy(i => i.GetDisplayName()).ToList();

            Panel common = GetCategory("Common");
            foreach (Circuit.Component i in components.Where(i => CommonTypes.Contains(i.GetType())))
                AddItem(common, i, CommandBindings);

            foreach (Circuit.Component i in components)
                AddItem(GetCategory(i.GetCategory()), i, Window.GetWindow(this).CommandBindings);

            foreach (Circuit.Component i in components)
                AddItem(filtered, i, null);
        }

        private void Refresh_Click(object sender, EventArgs e) { LoadComponents(); }
        
        private void AddItem(Panel Group, Circuit.Component C, CommandBindingCollection CommandBindings)
        {
            try
            {
                string DisplayName = C.GetDisplayName();

                ComponentButton button = new ComponentButton(C);
                button.Click += OnComponentClick;

                Group.Children.Add(button);

                // Append tooltip if there is a shortcut key.
                KeyGesture[] keys;
                if (CommandBindings != null && ShortcutKeys.TryGetValue(C.GetType(), out keys))
                    button.ToolTip = (string)button.ToolTip + " (" + keys.Select(j => j.GetDisplayStringForCulture(CultureInfo.CurrentCulture)).UnSplit(", ") + ")";
            }
            catch (System.Exception) { }
        }

        private void InitShortcutKeys(CommandBindingCollection Target)
        {
            foreach (KeyValuePair<Type, KeyGesture[]> i in ShortcutKeys)
            {
                Circuit.Component C = (Circuit.Component)Activator.CreateInstance(i.Key);

                RoutedCommand command = new RoutedCommand(C.GetDisplayName(), GetType());
                command.InputGestures.AddRange(i.Value);

                Target.Add(new CommandBinding(command, (x, y) => RaiseComponentClick(C)));
            }
        }

        private Panel GetCategory(string Category)
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

        private static IEnumerable<string> GetFiles(string Path, string Filter)
        {
            try
            {
                return System.IO.Directory.GetFiles(Path, Filter);
            }
            catch (Exception)
            {
                return new string[0];
            }
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
