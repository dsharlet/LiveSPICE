using System;
using System.Threading.Tasks;
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
        double[][] inputs = new double[1][];
        double[][] outputs = new double[1][];
        int numSamples = 128;

        public MainWindow()
        {
            InitializeComponent();

            plugin = new LiveSPICEPlugin();
            plugin.Host = new DummyHost();
            plugin.Initialize();
            plugin.Start();

            MainGrid.Children.Add(new EditorView(plugin));

            inputs[0] = new double[numSamples];
            outputs[0] = new double[numSamples];

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        plugin.SimulationProcessor.RunSimulation(inputs, outputs, numSamples);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error running circuit simulation.\n\n" + ex.Message, "Simulation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    await Task.Delay(100);
                }
            });
        }


    }
}
