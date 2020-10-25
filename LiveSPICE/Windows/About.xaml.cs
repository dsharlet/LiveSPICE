using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for ClosingDialog.xaml
    /// </summary>
    public partial class About : Window
    {
        public About() { InitializeComponent(); }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
            Close();
        }
    }
}
