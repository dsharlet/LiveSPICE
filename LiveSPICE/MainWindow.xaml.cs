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
        public MainWindow()
        {
            InitializeComponent();
            
            components.Init(this, toolbox_Click);
            New(new SchematicEditor());
        }

        public LayoutContent ActiveContent { get { return schematics.SelectedContent; } }
        public SchematicViewer ActiveViewer 
        { 
            get 
            {
                LayoutContent selected = schematics.SelectedContent;
                return selected != null ? (SchematicViewer)selected.Content : null;
            } 
        }
        public SchematicEditor ActiveSchematic 
        { 
            get 
            {
                SchematicViewer active = ActiveViewer;
                return active != null ? (SchematicEditor)active.Schematic : null;
            } 
        }

        private SchematicViewer New(SchematicEditor Schematic)
        {
            Schematic.SelectionChanged += schematic_SelectionChanged;

            SchematicViewer sv = new SchematicViewer(Schematic);
            LayoutDocument doc = new LayoutDocument();
            doc.Content = sv;
           
            doc.Closing += (o, e) => e.Cancel = !Schematic.CanClose();
            Schematic.PropertyChanged += (o, e) => 
            {
                if (e.PropertyName != "FileName") return;
                
                doc.Title = Schematic.FileName;
                doc.ToolTip = Schematic.FileName;
            };
            doc.Title = Schematic.FileName;
            doc.ToolTip = Schematic.FileName;

            schematics.Children.Add(doc);
            doc.IsActive = true;
            dock.UpdateLayout();
            sv.FocusCenter();
            return sv;
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e) { New(new SchematicEditor()); }
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                OpenFileDialog d = new OpenFileDialog();
                d.Filter = "Circuit Schematics|*" + SchematicEditor.FileExtension;
                d.DefaultExt = SchematicEditor.FileExtension;
                if (d.ShowDialog(this) ?? false)
                    New(SchematicEditor.Open(d.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            if (dlg.Result.Value)
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
            properties.SelectedObject = ((SchematicEditor)Sender).Selected.OfType<Circuit.Symbol>().Select(i => i.Component).FirstOrDefault();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //dock.LoadLayout("EditConfig.xml");
        }

        private void toolbox_Click(object s, RoutedEventArgs e) 
        {
            SchematicEditor active = ActiveSchematic;
            if (active == null)
                return;

            Type type = (Type)((Button)s).Tag;
            if (type == typeof(Circuit.Conductor))
                active.Tool = new WireTool(active);
            else
                active.Tool = new SymbolTool(active, type);
            active.Focus();
        }
        
        private void Simulate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Simulation simulation = new Simulation(ActiveSchematic.Schematic);
            simulation.Owner = this;
            simulation.Show();
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
