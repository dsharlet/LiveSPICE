using System.ComponentModel;
using System.Windows;

namespace LiveSPICEVst
{
    /// <summary>
    /// Interaction logic for SchematicWindow.xaml
    /// </summary>
    public partial class SchematicWindow : Window
    {
        public SchematicWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;

            Visibility = Visibility.Hidden;
        }
    }
}
