using Circuit;
using Microsoft.Win32;
using SharpSoundDevice;
using System;
using System.IO;
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

        SchematicWindow schematicWindow = null;
        string schematicPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LiveSPICE"), "Examples");

        public EditorView(LiveSPICEPlugin plugin)
        {
            this.Plugin = plugin;
            this.DataContext = Plugin;

            Plugin.EditorView = this;

            InitializeComponent();

            UpdateSchematic();
        }

        public void UpdateSchematic()
        {
            OverlaySchematic.DataContext = null;  // Set to null first to force rebind in case the schematic hasn't changed, but we want to update the UI
            OverlaySchematic.DataContext = Plugin.SimulationProcessor.Schematic;

            (LoadCircuitButton.Content as TextBlock).Text = (Plugin.SimulationProcessor.Schematic != null) ? Plugin.SimulationProcessor.SchematicName : "Load Schematic";

            schematicWindow = null;

            for (int i = 0; i < OversampleComboBox.Items.Count; i++)
            {
                if (int.Parse((OversampleComboBox.Items[i] as ComboBoxItem).Content as string) == Plugin.SimulationProcessor.Oversample)
                {
                    OversampleComboBox.SelectedIndex = i;

                    break;
                }
            }

            for (int i = 0; i < IterationsComboBox.Items.Count; i++)
            {
                if (int.Parse((IterationsComboBox.Items[i] as ComboBoxItem).Content as string) == Plugin.SimulationProcessor.Iterations)
                {
                    IterationsComboBox.SelectedIndex = i;

                    break;
                }
            }
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

            dialog.InitialDirectory = schematicPath;
            dialog.Filter = "Circuit Schemas (*.schx)|*.schx";

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;

                schematicPath = Path.GetDirectoryName(path);

                Plugin.LoadSchematic(path);

                UpdateSchematic();
            }
        }

        private void ShowAboutButton_Click(object sender, RoutedEventArgs e)
        {
            About about = new About() { Owner = Window.GetWindow(this) };
            about.ShowDialog();
        }

        private void ShowCircuitButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin.SimulationProcessor.Schematic != null)
            {
                if (schematicWindow == null)
                {
                    schematicWindow = new SchematicWindow()
                    {
                        Owner = Window.GetWindow(this),
                        DataContext = Plugin.SimulationProcessor.Schematic,
                        Title = Plugin.SimulationProcessor.SchematicName
                    };
                }
                
                schematicWindow.Show();
            }
        }
    }
}
