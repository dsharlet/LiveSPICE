using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for ClosingDialog.xaml
    /// </summary>
    public partial class ClosingDialog : Window
    {
        public ClosingDialog()
        {
            InitializeComponent();

            files.Focus();
        }

        private bool? result = null;
        public bool? Result { get { return result; } }

        private void Yes_Click(object sender, RoutedEventArgs e) { result = true; Close(); }
        private void No_Click(object sender, RoutedEventArgs e) { result = false; Close(); }
    }
}
