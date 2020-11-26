using System;
using System.Windows;
using LiveSPICEVst;

namespace MockVst
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        LiveSPICEPlugin plugin;
        private System.Timers.Timer simulateTimer;
        double[][] inputs = new double[1][];
        double[][] outputs = new double[1][];
        int numSamples = 128;

        public MainWindow()
        {
            InitializeComponent();

            plugin = new LiveSPICEPlugin();
            plugin.Host = new DummyHost();
            plugin.Start();

            MainGrid.Children.Add(new EditorView(plugin));

            simulateTimer = new System.Timers.Timer(100);
            simulateTimer.Elapsed += SimulateTimer_Elapsed;
            simulateTimer.Start();

            inputs[0] = new double[numSamples];
            outputs[0] = new double[numSamples];
        }

        private void SimulateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                plugin.SimulationProcessor.RunSimulation(inputs, outputs, numSamples);
            }
            catch (Exception ex)
            {
                simulateTimer.Enabled = false;

                MessageBox.Show("Error running circuit simulation.\n\n" + ex.Message, "Simulation Error", MessageBoxButton.OK, MessageBoxImage.Error);

                simulateTimer.Enabled = true;
            }
        }
    }
}
