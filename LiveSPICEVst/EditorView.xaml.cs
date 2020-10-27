using Circuit;
using Microsoft.Win32;
using SharpSoundDevice;
using System;
using System.Windows;
using System.Windows.Controls;

namespace LiveSPICEVst
{
    /// <summary>
    /// Dummy VST host class to use the the UI Test App
    /// </summary>
    class DummyHostInfo : IHostInfo
    {
        public double BPM { get { return 120; } }

        public double SamplePosition { get { return 0; } }

        public double SampleRate { get { return 44100; } }

        public int BlockSize { get { return 64; } }

        public int TimeSignatureNum => throw new NotImplementedException();

        public int TimeSignatureDen => throw new NotImplementedException();

        public string HostVendor => throw new NotImplementedException();

        public string HostName => throw new NotImplementedException();

        public uint HostVersion => throw new NotImplementedException();

        public void SendEvent(int pluginSenderId, Event ev)
        {
        }
    }


    /// <summary>
    /// Interaction logic for EditorView.xaml
    /// </summary>
    public partial class EditorView : UserControl
    {
        public LiveSPICEPlugin Plugin { get; private set; }

        public string CircuitName { get; private set; }

        Schematic schematic = null;
        SchematicWindow schematicWindow = null;

        public EditorView()
            : this(new LiveSPICEPlugin())
        {
            Plugin.HostInfo = new DummyHostInfo();
            Plugin.Start();
        }

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
