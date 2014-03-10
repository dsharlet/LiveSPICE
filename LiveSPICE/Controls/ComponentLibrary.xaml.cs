using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
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
using Util;

namespace LiveSPICE
{
    public class Component : INotifyPropertyChanged
    {
        private string name;
        public string Name { get { return name; } }

        private string desc;
        public string Description { get { return desc; } }

        private Circuit.Component component;
        public Circuit.Component Class { get { return component; } }

        private Visibility visibility = Visibility.Visible;
        public Visibility Visibility { get { return visibility; } set { visibility = value; NotifyChanged("Visibility"); } }

        public Component(Circuit.Component C, string Name, string Description)
        {
            component = C;
            name = Name;
            desc = Description;
        }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class Category : INotifyPropertyChanged
    {
        private ObservableCollection<Category> children = new ObservableCollection<Category>();
        public ObservableCollection<Category> Children { get { return children; } }

        private ObservableCollection<Component> components = new ObservableCollection<Component>();
        public ObservableCollection<Component> Components { get { return components; } }

        private string name;
        public string Name { get { return name; } set { name = value; NotifyChanged("Name"); } }

        private bool expanded = false;
        public bool IsExpanded { get { return expanded; } set { expanded = value; NotifyChanged("IsExpanded"); } }

        public void Clear() { children.Clear(); components.Clear(); }

        public Category SubCategory(string Name)
        {
            Category category = children.SingleOrDefault(i => i.Name == Name);
            if (category == null)
            {
                category = new Category() { Name = Name };
                children.Add(category);
            }

            return category;
        }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Interaction logic for ComponentLibrary.xaml
    /// </summary>
    public partial class ComponentLibrary : UserControl, INotifyPropertyChanged
    {
        private static List<Type> Common = new List<Type>()
        {
            typeof(Circuit.Conductor),
            typeof(Circuit.Ground),
            typeof(Circuit.Resistor),
            typeof(Circuit.Capacitor),
            typeof(Circuit.Inductor),
            typeof(Circuit.VoltageSource),
            typeof(Circuit.CurrentSource),
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

        private Category root = new Category();
        public Category Root { get { return root; } }

        public ComponentLibrary()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void AddLibrary(Category Category, string Library)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(Library);

            try
            {
                // Try to load the library as a LiveSPICE XML document.
                XDocument doc = XDocument.Load(Library);
                XElement library = doc.Element("Library");
                if (library != null)
                {
                    XAttribute category = library.Attribute("Category");
                    Category = Category.SubCategory(category != null ? category.Value : name);

                    foreach (XElement i in library.Elements("Component"))
                    {
                        Circuit.Component C = Circuit.Component.Deserialize(i);
                        AddItem(Category, C);
                    }
                }
                else if (doc.Element("Schematic") != null)
                {
                    Circuit.Schematic S = Circuit.Schematic.Deserialize(doc.Element("Schematic"));
                    Circuit.Circuit C = S.Build();
                    AddItem(Category, C, name, C.Description);
                }
            }
            catch (System.Xml.XmlException)
            {
                // Try to load the library as a SPICE model library.
                try
                {
                    Circuit.Spice.Statements statements = new Circuit.Spice.Statements(Library);
                    IEnumerable<Circuit.Spice.Model> models = statements.OfType<Circuit.Spice.Model>().Where(i => i.Component != null);
                    if (models.Any())
                    {
                        Category = Category.SubCategory(name);
                        foreach (Circuit.Spice.Model i in models)
                            AddItem(Category, i.Component, i.Component.PartNumber, i.Description);
                    }
                }
                catch (Exception Ex)
                {
                    Util.Log.Global.WriteLine(Util.MessageType.Warning, "Failed to load component libary '{0}': {1}", Library, Ex.Message);
                }
            }
            catch (Exception Ex)
            {
                Util.Log.Global.WriteLine(Util.MessageType.Warning, "Failed to load component libary '{0}': {1}", Library, Ex.Message);
            }
        }
        
        private void AddLibraries(Category Category, string Path)
        {
            foreach (string i in System.IO.Directory.GetDirectories(Path))
                AddLibraries(Category.SubCategory(System.IO.Path.GetFileName(i)), i);

            foreach (string i in System.IO.Directory.GetFiles(Path))
                AddLibrary(Category, i);
        }

        private void LoadComponents()
        {
            ProgressDialog.Run(Window.GetWindow(this), "Loading component library...", () =>
            {
                Root.Clear();

                // Add types identified in Common.
                Category common = Root.SubCategory("Common");
                common.IsExpanded = true;
                foreach (Type i in Common)
                    AddItem(common, i);

                // Add generic types to the Generic category.
                Category standard = Root.SubCategory("Generic");
                Type root = typeof(Circuit.Component);
                foreach (Assembly i in AppDomain.CurrentDomain.GetAssemblies())
                    foreach (Type j in i.GetTypes().Where(j => j.IsPublic && !j.IsAbstract && root.IsAssignableFrom(j)))
                        AddItem(standard, j);

                // Load standard libraries.
                string app = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string[] search =
                {
                    System.IO.Path.Combine(app, "Components"),
                    System.IO.Path.Combine(app, @"..\..\..\Circuit\Components"),
                };
                string path = search.FirstOrDefault(i => System.IO.Directory.Exists(i));
                if (path != null)
                    AddLibraries(Root, path);

                // Load components from the user docs folder.
                AddLibraries(Root, System.IO.Path.Combine(App.Current.UserDocuments.FullName, "Components"));
            });
        }

        private void AddItem(Category Group, Circuit.Component C, string Name, string Description)
        {
            // Append tooltip if there is a shortcut key.
            KeyGesture[] keys;
            if (ShortcutKeys.TryGetValue(C.GetType(), out keys))
                Description += " (" + String.Join(", ", keys.Select(j => j.GetDisplayStringForCulture(CultureInfo.CurrentCulture))) + ")";

            Component c = new Component(C, Name, Description);
            Group.Components.Add(c);
            root.Components.Add(c);
        }
        private void AddItem(Category Group, Circuit.Component C)
        {
            DescriptionAttribute desc = C.GetType().CustomAttribute<DescriptionAttribute>();
            AddItem(Group, C, C.TypeName, desc != null ? desc.Description : null);
        }
        private void AddItem(Category Group, Type T) 
        {
            try
            {
                AddItem(Group, (Circuit.Component)Activator.CreateInstance(T));
            }
            catch (Exception) { }
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            CommandBindingCollection commands = Window.GetWindow(this).CommandBindings;
            foreach (KeyValuePair<Type, KeyGesture[]> i in ShortcutKeys)
            {
                Circuit.Component C = (Circuit.Component)Activator.CreateInstance(i.Key);

                RoutedCommand command = new RoutedCommand(C.TypeName, GetType());
                command.InputGestures.AddRange(i.Value);

                commands.Add(new CommandBinding(command, (x, y) => RaiseComponentClick(C)));
            }

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
                    components.Visibility = Visibility.Collapsed;

                    foreach (Component i in root.Components)
                        i.Visibility = Visibility.Visible;
                }
                else
                {
                    categories.Visibility = Visibility.Collapsed;
                    components.Visibility = Visibility.Visible;

                    foreach (Component i in root.Components)
                        i.Visibility = i.Name.ToUpper().IndexOf(f) != -1 ? Visibility.Visible : Visibility.Collapsed;
                }
                NotifyChanged("Filter");
            }
        }
        public void ClearFilter_Click(object sender, RoutedEventArgs e) { Filter = ""; }

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
        private void Component_Click(object sender, RoutedEventArgs e) { RaiseComponentClick((Circuit.Component)((FrameworkElement)sender).Tag); }

        private void Refresh_Click(object sender, EventArgs e) { LoadComponents(); }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
