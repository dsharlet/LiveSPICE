using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Util;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for ComponentLibrary.xaml
    /// </summary>
    public partial class ComponentLibrary : UserControl, INotifyPropertyChanged
    {
        private static readonly List<Type> Common = new List<Type>()
        {
            typeof(Circuit.Conductor),
            typeof(Circuit.Ground),
            typeof(Circuit.Rail),
            typeof(Circuit.Resistor),
            typeof(Circuit.Capacitor),
            typeof(Circuit.Inductor),
            typeof(Circuit.VoltageSource),
            typeof(Circuit.CurrentSource),
            typeof(Circuit.NamedWire),
            typeof(Circuit.Label)
        };

        private static readonly Dictionary<Type, KeyGesture[]> ShortcutKeys = new Dictionary<Type, KeyGesture[]>()
        {
            [typeof(Circuit.Conductor)] = new[] { new KeyGesture(Key.W, ModifierKeys.Control) },
            [typeof(Circuit.Ground)] = new[] { new KeyGesture(Key.G, ModifierKeys.Control) },
            [typeof(Circuit.Rail)] = new[] { new KeyGesture(Key.P, ModifierKeys.Control) },
            [typeof(Circuit.Resistor)] = new[] { new KeyGesture(Key.R, ModifierKeys.Control) },
            [typeof(Circuit.Capacitor)] = new[] { new KeyGesture(Key.F, ModifierKeys.Control) },
            [typeof(Circuit.Inductor)] = new[] { 
                new KeyGesture(Key.L, ModifierKeys.Control), 
                new KeyGesture(Key.H, ModifierKeys.Control) },
            [typeof(Circuit.Label)] = new[] { new KeyGesture(Key.T, ModifierKeys.Control) },
        };

        private readonly Category root = new Category();
        public Category Root { get { return root; } }

        public ComponentLibrary()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void LoadComponents()
        {
            ProgressDialog.Run(Window.GetWindow(this), "Loading component library...", () =>
            {
                Root.Clear();

                // Add types identified in Common.
                Category common = Root.FindChild("Common");
                common.IsExpanded = true;
                foreach (Type i in Common)
                {
                    ShortcutKeys.TryGetValue(i, out KeyGesture[] keys);
                    common.AddComponent(i, keys);
                }

                // Add generic types to the Generic category.
                Category generic = Root.FindChild("Generic");
                Type root = typeof(Circuit.Component);
                foreach (Assembly i in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type j in i.GetTypes().Where(j => j.IsPublic && !j.IsAbstract && root.IsAssignableFrom(j) && j.CustomAttribute<ObsoleteAttribute>() == null))
                    {
                        ShortcutKeys.TryGetValue(j, out KeyGesture[] keys);
                        generic.AddComponent(j, keys);
                    }
                }

                // Load standard libraries.
                string app = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string[] search =
                {
                    System.IO.Path.Combine(app, "Components"),
                    System.IO.Path.Combine(app, @"..\..\..\Circuit\Components"),
                };
                string path = search.FirstOrDefault(i => System.IO.Directory.Exists(i));
                if (path != null)
                    Root.LoadLibraries(path);

                // Load components from the user docs folder.
                try
                {
                    Root.LoadLibraries(System.IO.Path.Combine(App.Current.UserDocuments.FullName, "Components"));
                }
                catch (Exception Ex)
                {
                    Util.Log.Global.WriteLine(Util.MessageType.Warning, "Component library directory not found: {0}", Ex.Message);
                }

                foreach (Component i in Root.Children.SelectMany(i => i.Flatten))
                    Root.Components.Add(i);
            });
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
                        i.IsVisible = true;
                }
                else
                {
                    categories.Visibility = Visibility.Collapsed;
                    components.Visibility = Visibility.Visible;

                    foreach (Component i in root.Components)
                        i.IsVisible = i.Name.ToUpper().IndexOf(f) != -1;
                }
                NotifyChanged(nameof(Filter));
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
