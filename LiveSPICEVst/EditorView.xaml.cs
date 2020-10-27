using Circuit;
using Microsoft.Win32;
using SharpSoundDevice;
using System;
using System.Windows;
using System.Windows.Controls;

namespace LiveSPICEVst
{
    /// <summary>
    /// Interaction logic for EditorView.xaml
    /// </summary>
    public partial class EditorView : UserControl
    {
        public LiveSPICEPlugin Plugin { get; private set; }

        public string CircuitName { get; private set; }

        Schematic schematic = null;
        SchematicWindow schematicWindow = null;

        public EditorView(LiveSPICEPlugin plugin)
        {
            CircuitName = "Load Circuit";

            this.Plugin = plugin;
            this.DataContext = Plugin;

            InitializeComponent();
        }

        private void OversampleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;

            Plugin.SimulationProcessor.Oversample = int.Parse((combo.SelectedItem as ComboBoxItem).Content as string);
        }

        private void IterationsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;

            Plugin.SimulationProcessor.Iterations = int.Parse((combo.SelectedItem as ComboBoxItem).Content as string);
        }

        private void LoadCircuitButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Circuit Schemas (*.schx)|*.schx";

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;

                try
                {
                    schematic = Schematic.Load(path);

                    OverlaySchematic.DataContext = schematic;

                    Plugin.SimulationProcessor.SetCircuit(schematic.Build());

                    CircuitName = System.IO.Path.GetFileNameWithoutExtension(path);
                    (LoadCircuitButton.Content as TextBlock).Text = CircuitName;

                    schematicWindow = null;
                }
                catch
                {
                }
            }
        }

        private void ShowAboutButton_Click(object sender, RoutedEventArgs e)
        {
            About about = new About() { Owner = Window.GetWindow(this) };
            about.ShowDialog();
        }

        private void ShowCircuitButton_Click(object sender, RoutedEventArgs e)
        {
            if (schematic != null)
            {
                if (schematicWindow == null)
                {
                    schematicWindow = new SchematicWindow()
                    {
                        Owner = Window.GetWindow(this),
                        DataContext = schematic,
                        Title = CircuitName
                    };
                }
                
                schematicWindow.Show();
            }
        }
    }
}
