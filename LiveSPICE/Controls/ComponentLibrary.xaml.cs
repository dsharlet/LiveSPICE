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

        private string name;
        public new string Name { get { return name; } }

        public ComponentButton(Circuit.Component C, string Name, string Description)
        {
            name = Name;

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
                Text = Name,
                Width = 80,
                Margin = new Thickness(3, 0, 3, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Normal,
                FontSize = FontSize,
            });

            Tag = C;
            if (Description != "")
                ToolTip = Description;
            Content = content;
            BorderBrush = null;
        }

        public Circuit.Component Component { get { return (Circuit.Component)Tag; } }
    }

    public interface ICategory
    {
        IEnumerable<Category> SubCategories { get; }
        Category SubCategory(string Name);

        void AddItem(UIElement Item);
    }

    public class Category : Expander, ICategory
    {
        private StackPanel children = new StackPanel() { Margin = new Thickness(10, 0, 0, 0) };
        private WrapPanel items = new WrapPanel() { Margin = new Thickness(10, 0, 0, 0) };

        public Category(string Name)
        {
            Header = Name;
            Focusable = false;
            FontWeight = FontWeights.Bold;
            FontSize = 14;

            StackPanel content = new StackPanel();
            content.Children.Add(children);
            content.Children.Add(items);
            Content = content;
        }

        public new string Name { get { return (string)Header; } }

        public IEnumerable<Category> SubCategories { get { return children.Children.OfType<Category>(); } }
        public IEnumerable<UIElement> Items { get { return items.Children.OfType<UIElement>(); } }

        public Category SubCategory(string Name)
        {
            Category category = SubCategories.SingleOrDefault(i => (string)i.Header == Name);
            if (category == null)
            {
                category = new Category(Name);
                children.Children.Add(category);
            }

            return category;
        }

        public void AddItem(UIElement Item) { items.Children.Add(Item); }
    }

    /// <summary>
    /// Interaction logic for ComponentLibrary.xaml
    /// </summary>
    public partial class ComponentLibrary : UserControl, INotifyPropertyChanged, ICategory
    {
        private static List<Type> Common = new List<Type>()
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

        private void AddLibrary(ICategory Category, string Library)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(Library);

            try
            {
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
                try
                {
                    Circuit.Spice.Statements statements = new Circuit.Spice.Statements(Library);
                    Category = Category.SubCategory(name);
                    foreach (Circuit.Spice.Model i in statements.OfType<Circuit.Spice.Model>().Where(i => i.Component != null))
                        AddItem(Category, i.Component, i.Component.PartNumber, i.Description);
                }
                catch (Exception)
                {
                    //Log.WriteLine(Circuit.MessageType.Error, "Error loading component library '{0}': {1}", Library, Ex.Message);
                }
            }
            catch (Exception)
            {
                //Log.WriteLine(Circuit.MessageType.Error, "Error loading component library '{0}': {1}", Library, Ex.Message);
            }
        }
        
        private void AddLibraries(ICategory Category, string Path)
        {
            foreach (string i in System.IO.Directory.GetDirectories(Path))
                AddLibraries(Category.SubCategory(System.IO.Path.GetFileName(i)), i);

            foreach (string i in System.IO.Directory.GetFiles(Path))
                AddLibrary(Category, i);
        }

        private void LoadComponents()
        {
            categories.Children.Clear();
            filtered.Children.Clear();

            // Add types identified in Common.
            Category common = SubCategory("Common");
            foreach (Type i in Common)
                AddItem(common, (Circuit.Component)Activator.CreateInstance(i));

            // Add generic types to the Standard category.
            Category standard = SubCategory("Standard");
            Type root = typeof(Circuit.Component);
            foreach (Assembly i in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type j in i.GetTypes().Where(j => j.IsPublic && !j.IsAbstract && root.IsAssignableFrom(j)))
                {
                    try
                    {
                        Circuit.Component c = (Circuit.Component)Activator.CreateInstance(j);
                        AddItem(standard, j);
                    }
                    catch (Exception) { }
                }
            }

            // Load standard libraries.
            string app = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string[] search =
            {
                System.IO.Path.Combine(app, "Components"),
                System.IO.Path.Combine(app, @"..\..\..\Components"),
            };
            string path = search.FirstOrDefault(i => System.IO.Directory.Exists(i));
            if (path != null)
                AddLibraries(this, path);

            // Load components from the user docs folder.
            AddLibraries(this, System.IO.Path.Combine(App.Current.UserDocuments.FullName, "Components"));
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
                    filtered.Visibility = Visibility.Collapsed;
                }
                else
                {
                    categories.Visibility = Visibility.Collapsed;
                    filtered.Visibility = Visibility.Visible;

                    foreach (ComponentButton i in filtered.Children)
                        i.Visibility = i.Name.ToUpper().IndexOf(f) != -1 ? Visibility.Visible : Visibility.Collapsed;
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
        
        private void Refresh_Click(object sender, EventArgs e) { LoadComponents(); }

        private ComponentButton NewComponentButton(Circuit.Component C, string Name, string Description)
        {
            ComponentButton button = new ComponentButton(C, Name, Description);
            button.Click += OnComponentClick;

            // Append tooltip if there is a shortcut key.
            KeyGesture[] keys;
            if (ShortcutKeys.TryGetValue(C.GetType(), out keys))
                button.ToolTip = (string)button.ToolTip + " (" + keys.Select(j => j.GetDisplayStringForCulture(CultureInfo.CurrentCulture)).UnSplit(", ") + ")";

            return button;
        }

        private void AddItem(ICategory Group, Circuit.Component C, string Name, string Description)
        {
            Group.AddItem(NewComponentButton(C, Name, Description));
            filtered.Children.Add(NewComponentButton(C, Name, Description));
        }
        private void AddItem(ICategory Group, Circuit.Component C)
        {
            DescriptionAttribute desc = C.GetType().GetCustomAttribute<DescriptionAttribute>();
            AddItem(Group, C, C.TypeName, desc != null ? desc.Description : null); 
        }
        private void AddItem(ICategory Group, Type T) { AddItem(Group, (Circuit.Component)Activator.CreateInstance(T)); }

        // Setup the shortcut key command bindings on the target.
        private void InitShortcutKeys(CommandBindingCollection Target)
        {
            foreach (KeyValuePair<Type, KeyGesture[]> i in ShortcutKeys)
            {
                Circuit.Component C = (Circuit.Component)Activator.CreateInstance(i.Key);

                RoutedCommand command = new RoutedCommand(C.TypeName, GetType());
                command.InputGestures.AddRange(i.Value);

                Target.Add(new CommandBinding(command, (x, y) => RaiseComponentClick(C)));
            }
        }

        // ICategory interface.
        public IEnumerable<Category> SubCategories { get { return categories.Children.OfType<Category>(); } }
        public Category SubCategory(string Category)
        {
            if (Category == "")
                Category = "Uncategorized";

            Category category = SubCategories.SingleOrDefault(i => i.Name == Category);
            if (category == null)
            {
                category = new Category(Category) { IsExpanded = !SubCategories.Any() };
                categories.Children.Add(category);
            }

            return category;
        }
        public void AddItem(UIElement Item) { throw new NotImplementedException("Item must be a child of a category."); }
        
        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
