using Microsoft.Win32;
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

        FileSystemWatcher loadedCircuitFileWatcher = null;

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
            string examples =
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LiveSPICE", "Examples");
            string initialDirectory =
                string.IsNullOrEmpty(Plugin.SchematicPath) ? examples : Path.GetDirectoryName(Plugin.SchematicPath);
            OpenFileDialog dialog = new OpenFileDialog
            {
                InitialDirectory = initialDirectory,
                Filter = "Circuit Schematics (*.schx)|*.schx"
            };

            if (dialog.ShowDialog() == true)
            {
                ReloadCircuit(dialog.FileName);
                AutoReloadSetup();
            }
        }

        private void ShowAboutButton_Click(object sender, RoutedEventArgs e)
        {
            About about = new About() { Owner = Window.GetWindow(this) };
            about.ShowDialog();
        }

        private void ReloadCircuitButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadCircuit(Plugin.SchematicPath);
        }

        private void ShowCircuitButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin.SimulationProcessor.Schematic != null)
            {
                if (schematicWindow == null)
                {
                    schematicWindow = new SchematicWindow()
                    {
                        DataContext = Plugin.SimulationProcessor.Schematic,
                        Title = Plugin.SimulationProcessor.SchematicName
                    };
                }

                schematicWindow.Show();
                schematicWindow.Activate();
            }
        }

        private void ReloadCircuit(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Plugin.LoadSchematic(path);
            UpdateSchematic();
        }

        private void OnCircuitFileUpdate(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath != Plugin.SchematicPath)
            {
                return;
            }

            // The OnCircuitFileUpdate function is called a from a thread separate from the UI
            // and it cannot directly access the plugin data.
            // Therefore, the main UI thread should be notified to call the reload function.
            Dispatcher.Invoke(() => ReloadCircuit(Plugin.SchematicPath));
        }

        private void AutoReloadCheckBox_Click(object sender, RoutedEventArgs e)
        {
            AutoReloadSetup();
        }

        private void AutoReloadSetup()
        {
            // Dispose of the resources used by the file watcher (if needed) when the checkbox is unchecked
            if (AutoReloadCheckBox.IsChecked == false)
            {
                loadedCircuitFileWatcher?.Dispose();
                return;
            }

            // If a schematic is not loaded, there is no file to be watched
            if (string.IsNullOrEmpty(Plugin.SchematicPath))
            {
                return;
            }

            // Create a new File System Watcher to watch for write events on the loaded schematic file
            loadedCircuitFileWatcher = new FileSystemWatcher
            {
                Filter = Path.GetFileName(Plugin.SchematicPath),
                Path = Path.GetDirectoryName(Plugin.SchematicPath),
                NotifyFilter = NotifyFilters.FileName,
                // EnableRaisingEvents has to be set last, otherwise it throws an error because the path is still empty
                EnableRaisingEvents = true,
            };

            // Link a callback function to the file watcher that executes
            // each time a rename operation occurs on the loaded schematic file.
            // A rename happens because whenever the schematic file is saved, it is firstly
            // stored as a .temp file and after writing, the file is renamed to use the correct extension.
            loadedCircuitFileWatcher.Renamed += OnCircuitFileUpdate;
        }
    }
}
