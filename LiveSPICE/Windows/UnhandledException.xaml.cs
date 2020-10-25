using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for UnhandledException.xaml
    /// </summary>
    public partial class UnhandledException : Window
    {
        private UnhandledException(Exception Ex)
        {
            InitializeComponent();
            info.Text = "Unhandled Exception: " + Ex.Message;
            ex.Text = Ex.ToString();
            ex.Focus();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ex.Text);
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        public static bool Show(Exception Ex)
        {
            UnhandledException wnd = new UnhandledException(Ex);
            return wnd.ShowDialog() ?? false;
        }
    }
}
