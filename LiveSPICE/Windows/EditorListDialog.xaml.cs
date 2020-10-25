using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for ClosingDialog.xaml
    /// </summary>
    public partial class EditorListDialog : Window
    {
        private EditorListDialog(string Message, MessageBoxButton Buttons, IEnumerable<SchematicEditor> Editors)
        {
            InitializeComponent();

            message.Text = Message;
            switch (Buttons)
            {
                case MessageBoxButton.OK:
                    yes.Content = "OK";
                    no.Visibility = Visibility.Collapsed;
                    cancel.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.OKCancel:
                    yes.Content = "OK";
                    no.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNo:
                    cancel.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNoCancel:
                    break;
                default: throw new ArgumentException("Button configuration not supported.", "Buttons");
            }

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

        private IEnumerable<SchematicEditor> Selected { get { return files.SelectedItems.OfType<FrameworkElement>().Select(i => (SchematicEditor)i.Tag); } }

        /// <summary>
        /// Show a dialog prompting the user regarding a list of schematics.
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Editors"></param>
        /// <returns></returns>
        public static IEnumerable<SchematicEditor> Show(Window Owner, string Message, MessageBoxButton Buttons, IEnumerable<SchematicEditor> Editors)
        {
            if (!Editors.Any())
                return new SchematicEditor[] { };

            EditorListDialog dlg = new EditorListDialog(Message, Buttons, Editors) { Owner = Owner };
            dlg.ShowDialog();

            switch (dlg.result)
            {
                case MessageBoxResult.Yes:
                case MessageBoxResult.OK:
                    return dlg.Selected.ToList();
                case MessageBoxResult.No:
                    return new SchematicEditor[] { };
                default:
                    return null;
            }
        }
        public static IEnumerable<SchematicEditor> Show(Window Owner, string Message, MessageBoxButton Buttons, params SchematicEditor[] Editors) { return Show(Owner, Message, Buttons, Editors.AsEnumerable()); }

        private void Yes_Click(object sender, RoutedEventArgs e) { result = ((string)yes.Content == "Yes" ? MessageBoxResult.Yes : MessageBoxResult.OK); Close(); }
        private void No_Click(object sender, RoutedEventArgs e) { result = MessageBoxResult.No; Close(); }
    }
}
