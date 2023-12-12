﻿using Microsoft.Win32;
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
        }

        private void OversampleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;

            Plugin.SimulationProcessor.Oversample = int.Parse((combo.SelectedItem as ComboBoxItem).Content as string);
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
                Plugin.LoadSchematic(dialog.FileName);

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
                        DataContext = Plugin.SimulationProcessor.Schematic,
                        Title = Plugin.SimulationProcessor.SchematicName
                    };
                }

                schematicWindow.Show();
                schematicWindow.Activate();
            }
        }
    }
}
