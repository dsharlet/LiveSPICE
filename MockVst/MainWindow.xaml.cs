using System.Windows;
using LiveSPICEVst;

namespace MockVst
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LiveSPICEPlugin plugin = new LiveSPICEPlugin();
            plugin.HostInfo = new DummyHostInfo();
            plugin.Start();

            MainGrid.Children.Add(new EditorView(plugin));
        }
    }
}
