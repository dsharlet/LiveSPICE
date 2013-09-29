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
using Circuit;

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
            
            toolbox.Init(this, toolbox_Click);
            New(new Schematic());
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
        public Schematic ActiveSchematic 
        { 
            get 
            {
                SchematicViewer active = ActiveViewer;
                return active != null ? active.Schematic : null;
            } 
        }

        private SchematicViewer New(Schematic Schematic)
        {
            SchematicViewer sv = new SchematicViewer(Schematic);
            sv.Schematic.SelectionChanged += schematic_SelectionChanged;

            LayoutDocument doc = new LayoutDocument();
            doc.Content = sv;
           
            doc.Closing += (o, e) => e.Cancel = !sv.Schematic.CanClose();
            sv.Schematic.PropertyChanged += (o, e) => 
            {
                if (e.PropertyName != "FileName") return;
                
                doc.Title = System.IO.Path.GetFileNameWithoutExtension(sv.Schematic.FileName);
                doc.ToolTip = sv.Schematic.FileName;
            };
            doc.Title = System.IO.Path.GetFileNameWithoutExtension(sv.Schematic.FileName);
            doc.ToolTip = sv.Schematic.FileName;

            schematics.Children.Add(doc);
            doc.IsActive = true;
            dock.UpdateLayout();
            sv.FocusCenter();
            return sv;
        }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e) { New(new Schematic()); }
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                OpenFileDialog d = new OpenFileDialog();
                d.Filter = "Circuit Schematics|*" + Schematic.FileExtension;
                d.DefaultExt = Schematic.FileExtension;
                if (d.ShowDialog(this) ?? false)
                    New(Schematic.Open(d.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Close_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = ActiveViewer != null; }
        private void Close_Executed(object sender, ExecutedRoutedEventArgs e) { SaveLayout(); ActiveContent.Close(); }
        
        private void Run_Executed(object sender, ExecutedRoutedEventArgs e) 
        {
            audio.Stop();

            Schematic active = ActiveSchematic;
            if (active == null)
                return;

            try
            {
                Circuit.Circuit circuit = active.Build(log);

                simulation.Run(new Circuit.Simulation(circuit, audio.SampleRate));
            }
            catch (Exception ex)
            {
                log.Write(LogType.Error, "Error building circuit for simulation: " + ex.GetType().ToString());
                log.Write(LogType.Error, ex.ToString());
            }
            audio.Run(ProcessSamples);
        }
        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e) { audio.Stop(); }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) { Close(); }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            ClosingDialog dlg = new ClosingDialog();
            dlg.Owner = this;

            foreach (SchematicViewer i in schematics.Children.Select(i => i.Content).OfType<SchematicViewer>())
                if (i.Schematic.Edits.Dirty)
                    dlg.files.Items.Add(new TextBlock() { Text = i.Schematic.FileName, Tag = i.Schematic });

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
                    if (!((Schematic)i.Tag).Save())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
        }

        private void schematic_SelectionChanged(object Sender, EventArgs Args)
        {
            properties.SelectedObject = ((Schematic)Sender).Selected.OfType<Symbol>().Select(i => i.Component).FirstOrDefault();
        }

        private void toolbox_Click(object s, RoutedEventArgs e) 
        {
            Schematic active = ActiveSchematic;
            if (active == null)
                return;

            Type type = (Type)((Button)s).Tag;
            if (type == typeof(Circuit.Wire))
                active.Tool = new WireTool(active);
            else
                active.Tool = new SymbolTool(active, type);
            active.Focus();
        }

        // Callback for audio.
        private void ProcessSamples(double[] Samples, int Rate)
        {
            simulation.Process(Samples, Rate, oscilloscope);
        }

        private void SaveLayout(string Config)
        {
            Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dock);
            serializer.Serialize(Config);

            // http://avalondock.codeplex.com/discussions/400644
            XmlDocument configDoc = new XmlDocument();
            configDoc.Load(Config);
            XmlNodeList projectNodes = configDoc.GetElementsByTagName("LayoutDocument");
            for (int i = projectNodes.Count - 1; i > -1; i--)
            {
                projectNodes[i].ParentNode.RemoveChild(projectNodes[i]);
            }
            configDoc.Save(Config);
        }

        private void SaveLayout()
        {
            SaveLayout("EditConfig.xml");
        }

        private void LoadLayout(string Config)
        {
            try
            {
                Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dock);
                serializer.Deserialize(Config);
            }
            catch (Exception) { }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadLayout("EditConfig.xml");
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
