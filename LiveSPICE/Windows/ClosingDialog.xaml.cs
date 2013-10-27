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
        private ClosingDialog(IEnumerable<SchematicEditor> Editors)
        {
            InitializeComponent();

            foreach (SchematicEditor i in Editors)
                files.Items.Add(new TextBlock()
                {
                    Text = MruMenuItem.CompactPath(i.FilePath, 50),
                    ToolTip = i.FilePath,
                    Tag = i
                });

            files.SelectAll();
            files.Focus();
        }

        private MessageBoxResult result = MessageBoxResult.Cancel;

        /// <summary>
        /// Show a dialog asking the user to save any unsaved schematics.
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Editors"></param>
        /// <returns>false if closing should be cancelled.</returns>
        public static bool Show(Window Owner, IEnumerable<SchematicEditor> Editors)
        {
            ClosingDialog dlg = new ClosingDialog(Editors) { Owner = Owner };
            dlg.ShowDialog();
            switch (dlg.result)
            {
                case MessageBoxResult.Yes:
                    foreach (FrameworkElement i in dlg.files.SelectedItems)
                        if (!((SchematicEditor)i.Tag).Save())
                            return false;
                    return true;
                case MessageBoxResult.No:
                    return true;
                default:
                    return false;
            }
        }
        public static bool Show(Window Owner, params SchematicEditor[] Editors) { return Show(Owner, Editors.AsEnumerable()); }

        private void Yes_Click(object sender, RoutedEventArgs e) { result = MessageBoxResult.Yes; Close(); }
        private void No_Click(object sender, RoutedEventArgs e) { result = MessageBoxResult.No; Close(); }
    }
}
