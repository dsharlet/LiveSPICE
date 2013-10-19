using System;
using System.Collections.Generic;
using System.Linq;
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
        private static Tuple<Type, Key[]>[] CommonTypes = 
        {
            new Tuple<Type, Key[]> (typeof(Circuit.Conductor), new Key[] { Key.W }),
            new Tuple<Type, Key[]> (typeof(Circuit.Ground), new Key[] { Key.G }),
            new Tuple<Type, Key[]> (typeof(Circuit.Resistor), new Key[] { Key.R }),
            new Tuple<Type, Key[]> (typeof(Circuit.Capacitor), new Key[] { Key.F }),
            new Tuple<Type, Key[]> (typeof(Circuit.Inductor), new Key[] { Key.L, Key.H }),
            new Tuple<Type, Key[]> (typeof(Circuit.VoltageSource), null),
            new Tuple<Type, Key[]> (typeof(Circuit.NamedWire), null),
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

        public void Init(Window ShortcutKeys, RoutedEventHandler OnClick)
        {
            Expander common = GetCategory("Common");
            foreach (Tuple<Type, Key[]> i in CommonTypes)
            {
                Button item = AddItem(common, i.Item1, OnClick);

                if (i.Item2 != null)
                {
                    RoutedCommand command = new RoutedCommand();
                    foreach (Key j in i.Item2)
                        command.InputGestures.Add(new KeyGesture(j, ModifierKeys.Control));

                    CommandBinding binding = new CommandBinding(command, (o, e) => item.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
                    ShortcutKeys.CommandBindings.Add(binding);
                }
            }
            common.IsExpanded = true;

            foreach (Type i in GetComponentTypes())
                foreach (CategoryAttribute j in i.GetCustomAttributes(typeof(CategoryAttribute), false).Cast<CategoryAttribute>())
                    AddItem(GetCategory(j.Category), i, OnClick);
        }
        
        private Button AddItem(Expander Group, Type T, RoutedEventHandler OnClick)
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
                };
                button.Click += OnClick;
                
                ((Panel)Group.Content).Children.Add(button);

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
