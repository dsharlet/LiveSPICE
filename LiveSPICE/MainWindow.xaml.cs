using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.AvalonDock.Layout;
using Microsoft.Win32;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private System.Windows.Forms.PropertyGrid Properties;

        public MainWindow()
        {
            InitializeComponent();

            Properties = new System.Windows.Forms.PropertyGrid();
            components.Init(this, toolbox_Click);

            properties.Content = new System.Windows.Forms.Integration.WindowsFormsHost() 
            { 
                Child = Properties 
            };
        }

        public LayoutContent ActiveContent { get { return schematics.SelectedContent; } }
        public SchematicViewer ActiveViewer 
        { 
            get 
            {
                if (schematics == null) return null;
                LayoutContent selected = schematics.SelectedContent;
                return selected != null ? (SchematicViewer)selected.Content : null;
            } 
        }
        public SchematicEditor ActiveEditor 
        { 
            get 
            {
                SchematicViewer active = ActiveViewer;
                return active != null ? (SchematicEditor)active.Schematic : null;
            } 
        }

        private string status;
        public string Status 
        {
            get { return status != null ? status : "Ready"; } 
            set { status = value; NotifyChanged("Status"); }
        }

        private SchematicViewer New(SchematicEditor Schematic)
        {
            Schematic.SelectionChanged += schematic_SelectionChanged;

            SchematicViewer sv = new SchematicViewer(Schematic);
            LayoutDocument doc = new LayoutDocument()
            {
                Content = sv,
                Title = Schematic.FileName,
                ToolTip = Schematic.FileName,
                IsActive = true
            };
            doc.Closing += (o, e) => e.Cancel = !Schematic.CanClose();

            Schematic.PropertyChanged += (o, e) => 
            {
                if (e.PropertyName != "FileName") return;
                
                doc.Title = Schematic.FileName;
                doc.ToolTip = Schematic.FileName;
            };

            schematics.Children.Add(doc);
            dock.UpdateLayout();
            sv.FocusCenter();
            return sv;
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e) { New(new SchematicEditor()); }
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                OpenFileDialog d = new OpenFileDialog()
                {
                    Filter = "Circuit Schematics|*" + SchematicEditor.FileExtension,
                    DefaultExt = SchematicEditor.FileExtension,
                    Multiselect = true
                };
                if (d.ShowDialog(this) ?? false)
                {
                    foreach (string i in d.FileNames)
                        New(SchematicEditor.Open(i));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (SchematicViewer i in schematics.Children.Select(i => i.Content).OfType<SchematicViewer>())
                if (!((SchematicEditor)i.Schematic).Save())
                    break;

        }

        private void Close_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = ActiveViewer != null; }
        private void Close_Executed(object sender, ExecutedRoutedEventArgs e) { dock.SaveLayout("EditConfig.xml"); ActiveContent.Close(); }
        
        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) { Close(); }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            ClosingDialog dlg = new ClosingDialog();
            dlg.Owner = this;

            foreach (SchematicViewer i in schematics.Children.Select(i => i.Content).OfType<SchematicViewer>())
                if (((SchematicEditor)i.Schematic).Edits.Dirty)
                    dlg.files.Items.Add(new TextBlock() { Text = ((SchematicEditor)i.Schematic).FileName, Tag = i.Schematic });

            if (dlg.files.Items.Count == 0)
                return;

            dlg.files.SelectAll();

            dlg.ShowDialog();
            if (!dlg.Result.HasValue)
            {
                e.Cancel = true;
                return;
            }
            else if (dlg.Result.Value)
            {
                foreach (TextBlock i in dlg.files.SelectedItems)
                {
                    if (!((SchematicEditor)i.Tag).Save())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
        }

        private void schematic_SelectionChanged(object Sender, EventArgs Args)
        {
            Properties.SelectedObjects = ((SchematicEditor)Sender).Selected.OfType<Circuit.Symbol>().Select(i => i.Component).ToArray<object>();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //dock.LoadLayout("EditConfig.xml");
        }

        private void toolbox_Click(object s, RoutedEventArgs e) 
        {
            SchematicEditor active = ActiveEditor;
            if (active == null)
                return;

            e.Handled = true;

            Type type = (Type)((Button)s).Tag;
            if (type == typeof(Circuit.Conductor))
                active.Tool = new WireTool(active);
            else
                active.Tool = new SymbolTool(active, type);

            active.Focus();
            Keyboard.Focus(active);
        }

        private AudioConfiguration ConfigAudio()
        {
            AudioConfiguration audio = new AudioConfiguration() { Owner = this };
            bool? result = audio.ShowDialog();
            return result ?? false ? audio : null;
        }

        private void Simulate_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = ActiveEditor != null; }
        private void Simulate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AudioConfiguration audio = ConfigAudio();
            if (audio != null)
            {
                TransientSimulation simulation = new TransientSimulation(ActiveEditor.Schematic, audio) { Owner = this };
                simulation.Show();
            }
        }
        
        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
