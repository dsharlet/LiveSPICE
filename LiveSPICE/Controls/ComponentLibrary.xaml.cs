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

        public static IEnumerable<Type> ComponentTypes
        {
            get
            {
                Type root = typeof(Circuit.Component);
                return AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(i => i.GetTypes().Where(j => j.IsPublic && !j.IsAbstract && root.IsAssignableFrom(j)));
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

                    foreach (Button i in filtered.Children)
                        i.Visibility = GetDisplayName((Type)i.Tag).ToUpper().IndexOf(f) != -1 ? Visibility.Visible : Visibility.Collapsed;
                }
                NotifyChanged("Filter");
            }
        }
                
        public void Init(RoutedEventHandler OnClick, CommandBindingCollection CommandBindings)
        {
            Panel common = AddCategory("Common");
            foreach (Type i in CommonTypes)
                AddItem(common, i, OnClick, CommandBindings);

            foreach (Type i in ComponentTypes)
                foreach (CategoryAttribute j in i.GetCustomAttributes(typeof(CategoryAttribute), false).Cast<CategoryAttribute>())
                    AddItem(AddCategory(j.Category), i, OnClick, CommandBindings);

            foreach (Type i in ComponentTypes)
                AddItem(filtered, i, OnClick, null);
        }
        
        private void AddItem(Panel Group, Type T, RoutedEventHandler OnClick, CommandBindingCollection CommandBindings)
        {
            try
            {
                StackPanel content = new StackPanel() { Orientation = Orientation.Vertical };

                // Add image to the button.
                ComponentControl symbol = new ComponentControl((Circuit.Component)Activator.CreateInstance(T))
                {
                    Width = 48,
                    Height = 32,
                    ShowText = false,
                    Margin = new Thickness(2),
                };
                content.Children.Add(symbol);

                TextBlock name = new TextBlock()
                {
                    Text = GetDisplayName(T),
                    MaxWidth = symbol.Width,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeights.Normal
                };
                content.Children.Add(name);

                Button button = new Button()
                {
                    Tag = T,
                    ToolTip = GetDisplayName(T),
                    Content = content,
                    BorderBrush = null,
                };
                button.Click += OnClick;

                Group.Children.Add(button);

                // Bind input gestures to a command and add it to the command bindings.
                if (CommandBindings != null && ShortcutKeys.ContainsKey(T))
                {
                    RoutedCommand command = new RoutedCommand();
                    command.InputGestures.AddRange(ShortcutKeys[T]);

                    button.ToolTip = (string)button.ToolTip + " (" + ShortcutKeys[T].Select(j => j.GetDisplayStringForCulture(CultureInfo.CurrentCulture)).UnSplit(", ") + ")";

                    CommandBinding binding = new CommandBinding(command, (o, e) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
                    CommandBindings.Add(binding);
                }
            }
            catch (System.Exception) { }
        }
        
        private Panel AddCategory(string Category)
        {
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
                    IsExpanded = !expanders.Any(),
                };
                categories.Children.Add(category);
            }

            return (Panel)category.Content;
        }
        
        private static string GetDisplayName(Type T)
        {
            DisplayNameAttribute name = T.GetCustomAttribute<DisplayNameAttribute>();
            return name != null ? name.DisplayName : T.ToString();
        }

        private static string GetDescription(Type T)
        {
            DescriptionAttribute desc = T.GetCustomAttribute<DescriptionAttribute>();
            return desc != null ? desc.Description : GetDisplayName(T);
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
