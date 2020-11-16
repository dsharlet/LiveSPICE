using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SchematicControls;
using AvalonDock.Layout;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ComponentLibrary Components { get { return (ComponentLibrary)components.Content; } }
        public PropertyGrid Properties { get { return (PropertyGrid)properties.Content; } }

        public MainWindow()
        {
            InitializeComponent();

            Components.ComponentClick += component_Click;
            Properties.PropertyValueChanged += properties_PropertyValueChanged;
            schematics.ChildrenTreeChanged += (o, e) => NotifyChanged("ActivewViewerZoom");

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].EndsWith(".schx") && System.IO.File.Exists(args[i]))
                    Open(args[i]);
            }
        }

        public IEnumerable<SchematicViewer> Viewers { get { return schematics.Children.Select(i => i.Content).OfType<SchematicViewer>(); } }
        public IEnumerable<SchematicEditor> Editors { get { return Viewers.Select(i => i.Schematic).OfType<SchematicEditor>(); } }

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
            set { status = value; NotifyChanged(nameof(Status)); }
        }

        private SchematicViewer AddViewer(SchematicEditor Schematic)
        {
            Schematic.SelectionChanged += schematic_SelectionChanged;
            Schematic.EditSelection += schematic_EditSelection;

            SchematicViewer sv = new SchematicViewer(Schematic);
            LayoutDocument doc = new LayoutDocument()
            {
                Content = sv,
                Title = Schematic.Title,
                ToolTip = Schematic.FilePath,
                IsActive = true
            };
            doc.Closing += (o, e) => e.Cancel = !Schematic.CanClose();

            Schematic.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "FilePath")
                {
                    doc.Title = Schematic.Title;
                    doc.ToolTip = Schematic.FilePath;
                }
            };

            sv.Tag = doc;

            schematics.Children.Add(doc);
            dock.UpdateLayout();
            sv.FocusCenter();
            return sv;
        }
        public SchematicViewer New() { return AddViewer(new SchematicEditor()); }

        public SchematicViewer FindViewer(string FileName)
        {
            FileName = System.IO.Path.GetFullPath(FileName);
            foreach (SchematicViewer i in Viewers)
                if (System.IO.Path.GetFullPath(((SchematicEditor)i.Schematic).FilePath) == FileName)
                    return i;
            return null;
        }

        private void Open(string FileName, bool CanClose)
        {
            try
            {
                SchematicViewer open = FindViewer(FileName);
                if (open != null)
                {
                    ((LayoutDocument)open.Tag).IsSelected = true;
                    // If this schematic is already open, prompt for re-open if necessary.
                    if (CanClose || ((SchematicEditor)open.Schematic).CanClose(true))
                        open.Schematic = SchematicEditor.Open(FileName);
                }
                else
                {
                    // Just make a new one.
                    AddViewer(SchematicEditor.Open(FileName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Open(string FileName) { Open(FileName, false); }

        private void New_Executed(object sender, ExecutedRoutedEventArgs e) { New(); }
        private void OnMruClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Open((string)((MenuItem)e.Source).Tag);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(this, Ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog()
                {
                    //InitialDirectory = App.Current.UserDocuments.FullName,
                    Filter = "Circuit Schematics|*." + SchematicEditor.FileExtension + "|XML Files|*.xml|All Files|*.*",
                    DefaultExt = SchematicEditor.FileExtension,
                    Multiselect = true
                };
                dlg.CustomPlaces.Add(new FileDialogCustomPlace(App.Current.UserDocuments.FullName));
                if (dlg.ShowDialog(this) ?? false)
                {
                    foreach (string i in dlg.FileNames)
                        Open(i);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (SchematicViewer i in schematics.Children.Select(i => i.Content).OfType<SchematicViewer>())
                if (!((SchematicEditor)i.Schematic).Save())
                    break;
        }

        private bool activating = false;
        private void OnActivated(object sender, EventArgs e)
        {
            if (activating)
                return;
            activating = true;

            // Find the schematics that have been modified outside the editor.
            IEnumerable<SchematicEditor> modified = EditorListDialog.Show(
                this,
                "The following schematics were modified outside LiveSPICE, do you want to reload them?",
                MessageBoxButton.YesNo,
                Editors.Where(i => i.CheckForModifications()).ToList());

            if (modified != null)
            {
                foreach (SchematicEditor i in modified)
                    i.Touch();
                foreach (SchematicEditor i in modified)
                    Open(i.FilePath, true);
            }
            activating = false;
        }

        private void Close_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = ActiveViewer != null; }
        private void Close_Executed(object sender, ExecutedRoutedEventArgs e) { App.Current.Settings.MainWindowLayout = dock.SaveLayout(); ActiveContent.Close(); }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e) { Close(); }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Find the schematics that have pending edits.
            IEnumerable<SchematicEditor> save = EditorListDialog.Show(
                this,
                "Save the following schematics?",
                MessageBoxButton.YesNoCancel,
                Editors.Where(i => i.Edits.Dirty));

            if (save != null)
            {
                foreach (SchematicEditor i in save)
                {
                    if (!i.Save())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void schematic_SelectionChanged(object Sender, SelectionEventArgs Args)
        {
            Properties.SelectedObjects = Args.Selected.ToArray<object>();
            Properties.Tag = ((SchematicEditor)Sender).Edits;
        }

        private void schematic_EditSelection(object sender, EventArgs e)
        {
            Properties.Focus();
        }

        void properties_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            IEnumerable<object> selected = Properties.SelectedObjects;
            PropertyInfo property = selected.First().GetType().GetProperty(e.ChangedItem.PropertyDescriptor.Name);
            EditStack edits = (EditStack)Properties.Tag;
            edits.Did(EditList.New(selected.Select(i => new PropertyEdit(i, property, property.GetValue(i, null), e.OldValues[i]))));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dock.LoadLayout(App.Current.Settings.MainWindowLayout);
            }
            catch (Exception) { }
        }

        private void component_Click(Circuit.Component C)
        {
            SchematicEditor active = ActiveEditor;
            if (active == null)
                active = (SchematicEditor)New().Schematic;

            if (C is Circuit.Conductor)
                active.Tool = new WireTool(active);
            else
                active.Tool = new SymbolTool(active, C.Clone());

            active.Focus();
            Keyboard.Focus(active);
        }

        private void Simulate_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = ActiveEditor != null; }
        private void Simulate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (ActiveEditor != null)
            {
                AudioConfig config = new AudioConfig() { Owner = this };
                if (config.Inputs.Length + config.Outputs.Length == 0)
                    if (!(config.ShowDialog() ?? false))
                        return;

                LiveSimulation simulation = new LiveSimulation(ActiveEditor.Schematic, config.Device, config.Inputs, config.Outputs) { Owner = this, Title = ActiveEditor.Title + " - Live Simulation" };
                simulation.Show();
            }
        }

        private void AudioConfiguration_Click(object sender, RoutedEventArgs e)
        {
            AudioConfig config = new AudioConfig() { Owner = this };
            config.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            About about = new About() { Owner = this };
            about.ShowDialog();
        }

        private void ViewProperties_Click(object sender, RoutedEventArgs e) { ToggleVisible(properties); }
        private void ViewComponents_Click(object sender, RoutedEventArgs e) { ToggleVisible(components); }

        private static void ToggleVisible(LayoutAnchorable Anchorable)
        {
            if (Anchorable.IsVisible)
                Anchorable.Hide();
            else
                Anchorable.Show();
        }

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
