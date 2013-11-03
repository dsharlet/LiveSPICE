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
    public partial class ComponentLibrary : UserControl
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

                
        public ComponentLibrary()
        {
            InitializeComponent();
        }
                
        public static IEnumerable<Type> GetComponentTypes()
        {
            Type root = typeof(Circuit.Component);
            return Assembly.GetAssembly(root).GetTypes().Where(t => !t.IsAbstract && root.IsAssignableFrom(t));
        }

        public void Init(RoutedEventHandler OnClick, CommandBindingCollection CommandBindings)
        {
            Expander common = GetCategory("Common");
            foreach (Type i in CommonTypes)
                AddItem(common, i, OnClick, CommandBindings);
            common.IsExpanded = true;

            foreach (Type i in GetComponentTypes())
                foreach (CategoryAttribute j in i.GetCustomAttributes(typeof(CategoryAttribute), false).Cast<CategoryAttribute>())
                    AddItem(GetCategory(j.Category), i, OnClick, CommandBindings);
        }
        
        private Button AddItem(Expander Group, Type T, RoutedEventHandler OnClick, CommandBindingCollection CommandBindings)
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
                
                ((Panel)Group.Content).Children.Add(button);

                // Bind input gestures to a command and add it to the command bindings.
                if (ShortcutKeys.ContainsKey(T))
                {
                    RoutedCommand command = new RoutedCommand();
                    command.InputGestures.AddRange(ShortcutKeys[T]);

                    button.ToolTip = (string)button.ToolTip + " (" + ShortcutKeys[T].Select(j => j.GetDisplayStringForCulture(CultureInfo.CurrentCulture)).UnSplit(", ") + ")";

                    CommandBinding binding = new CommandBinding(command, (o, e) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
                    CommandBindings.Add(binding);
                }

                return button;
            }
            catch (System.Exception) 
            {
                return null;
            }
        }
        
        private Expander GetCategory(string Category)
        {
            IEnumerable<Expander> expanders = list.Children.OfType<Expander>();
            Expander category = expanders.SingleOrDefault(i => (string)i.Header == Category);
            if (category == null)
            {
                category = new Expander()
                {
                    Header = Category,
                    Content = new WrapPanel(),
                    Focusable = false,
                    FontWeight = FontWeights.Bold
                };
                list.Children.Add(category);
            }

            return category;
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
    }
}
