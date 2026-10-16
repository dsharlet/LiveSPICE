using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Circuit;
using Util;
using CircuitComponent = Circuit.Component;

namespace LiveSPICE.Avalonia;

public sealed class MainWindow : Window
{
    private static readonly Type[] CommonComponents =
    {
        typeof(Conductor),
        typeof(Ground),
        typeof(Rail),
        typeof(Resistor),
        typeof(Capacitor),
        typeof(Inductor),
        typeof(VoltageSource),
        typeof(CurrentSource),
        typeof(NamedWire),
        typeof(Circuit.Label)
    };

    private readonly TabControl documents = new TabControl();
    private readonly TreeView componentList = new TreeView();
    private readonly TextBox componentFilter = new TextBox { Watermark = "Filter components" };
    private List<ComponentListItem> allComponents = new List<ComponentListItem>();
    private readonly PropertyInspector propertyInspector = new PropertyInspector();
    private readonly TextBlock status = new TextBlock { Text = "Ready", VerticalAlignment = VerticalAlignment.Center };
    private readonly AppSettings settings = AppSettings.Load();
    private readonly MenuItem recentFilesMenu = new MenuItem { Header = "Recent Files" };
    private bool suppressSelectionEvent;
    private bool closeConfirmed;

    public MainWindow()
    {
        Title = "LiveSPICE Avalonia";
        Width = Math.Max(settings.WindowWidth, 800);
        Height = Math.Max(settings.WindowHeight, 500);
        MinWidth = 800;
        MinHeight = 500;
        KeyDown += OnKeyDown;
        Activated += async (_, _) => await CheckExternalModificationsAsync();

        DockPanel root = new DockPanel();

        Menu menu = BuildMenu();
        DockPanel.SetDock(menu, Dock.Top);
        root.Children.Add(menu);

        Control toolbar = BuildToolbar();
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);

        Border statusBar = new Border
        {
            Background = Brushes.WhiteSmoke,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new global::Avalonia.Thickness(1, 0, 0, 0),
            Padding = new global::Avalonia.Thickness(8, 4),
            Child = status
        };
        DockPanel.SetDock(statusBar, Dock.Bottom);
        root.Children.Add(statusBar);

        Grid content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("240,* ,300")
        };

        componentList.Margin = new global::Avalonia.Thickness(6);
        componentList.SelectionChanged += (_, _) => ActivateSelectedComponent();
        componentFilter.Margin = new global::Avalonia.Thickness(6, 0, 6, 4);
        componentFilter.TextChanged += (_, _) => ApplyComponentFilter();
        PopulateComponents();

        // The library adds ~90 parts to what was a short list, so it needs a filter to stay usable.
        DockPanel componentsContent = new DockPanel();
        DockPanel.SetDock(componentFilter, Dock.Top);
        componentsContent.Children.Add(componentFilter);
        componentsContent.Children.Add(componentList);

        Border componentsPanel = Panel("Components", componentsContent);
        Grid.SetColumn(componentsPanel, 0);
        content.Children.Add(componentsPanel);

        documents.SelectionChanged += (_, _) => OnActiveDocumentChanged();
        Grid.SetColumn(documents, 1);
        content.Children.Add(documents);

        propertyInspector.PropertyChangedByUser += action =>
        {
            ActiveDocument?.Do(action);
            ActiveCanvas?.InvalidateVisual();
            RefreshDocumentHeaders();
        };
        Border propertiesPanel = Panel("Properties", propertyInspector);
        Grid.SetColumn(propertiesPanel, 2);
        content.Children.Add(propertiesPanel);

        root.Children.Add(content);
        Content = root;
    }

    private SchematicDocument? ActiveDocument => ActiveTab?.Tag as SchematicDocument;

    private SchematicCanvas? ActiveCanvas => ActiveTab?.Content as SchematicCanvas;

    private TabItem? ActiveTab => documents.SelectedItem as TabItem;

    private Menu BuildMenu()
    {
        Menu menu = new Menu
        {
            ItemsSource = new[]
            {
                new MenuItem
                {
                    Header = "_File",
                    ItemsSource = new Control[]
                    {
                        MenuItem("_New", (_, _) => NewDocument()),
                        MenuItem("_Open", async (_, _) => await OpenSchematicAsync()),
                        MenuItem("_Save", async (_, _) => await SaveActiveAsync()),
                        MenuItem("Save _As", async (_, _) => await SaveActiveAsAsync()),
                        MenuItem("Save A_ll", async (_, _) => await SaveAllAsync()),
                        new Separator(),
                        recentFilesMenu,
                        new Separator(),
                        MenuItem("_Close", async (_, _) => await CloseActiveAsync()),
                        MenuItem("E_xit", (_, _) => Close())
                    }
                },
                new MenuItem
                {
                    Header = "_Edit",
                    ItemsSource = new Control[]
                    {
                        MenuItem("_Delete", (_, _) => ActiveCanvas?.DeleteSelection()),
                        MenuItem("_Undo", (_, _) => UndoActive()),
                        MenuItem("_Redo", (_, _) => RedoActive()),
                        new Separator(),
                        MenuItem("Cu_t", async (_, _) => await CutSelectionAsync()),
                        MenuItem("_Copy", async (_, _) => await CopySelectionAsync()),
                        MenuItem("_Paste", async (_, _) => await PasteSelectionAsync()),
                        MenuItem("Select _All", (_, _) => ActiveCanvas?.SelectAll()),
                        new Separator(),
                        MenuItem("Rotate Left", (_, _) => ActiveCanvas?.RotateSelection(1)),
                        MenuItem("Rotate Right", (_, _) => ActiveCanvas?.RotateSelection(-1)),
                        MenuItem("Flip", (_, _) => ActiveCanvas?.FlipSelection())
                    }
                },
                new MenuItem
                {
                    Header = "_View",
                    ItemsSource = new Control[]
                    {
                        MenuItem("Zoom _In", (_, _) => ZoomActive(1.2)),
                        MenuItem("Zoom _Out", (_, _) => ZoomActive(1 / 1.2)),
                        MenuItem("Zoom _Fit", (_, _) => ActiveCanvas?.FitToView())
                    }
                },
                new MenuItem
                {
                    Header = "_Simulate",
                    ItemsSource = new Control[]
                    {
                        MenuItem("Audio Settings", (_, _) => new AudioConfigWindow(settings).Show()),
                        MenuItem("Validate Build", (_, _) => ValidateActiveCircuit()),
                        MenuItem("Run Simulation", (_, _) => RunSimulation())
                    }
                },
                new MenuItem
                {
                    Header = "_About",
                    ItemsSource = new Control[]
                    {
                        MenuItem("About", (_, _) => status.Text = "LiveSPICE Avalonia GUI port")
                    }
                }
            }
        };
        RefreshRecentFilesMenu();
        return menu;
    }

    private static MenuItem MenuItem(string header, EventHandler<global::Avalonia.Interactivity.RoutedEventArgs> click)
    {
        MenuItem item = new MenuItem { Header = header };
        item.Click += click;
        return item;
    }

    private Control BuildToolbar()
    {
        StackPanel toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new global::Avalonia.Thickness(8),
            Children =
            {
                Button("New", (_, _) => NewDocument()),
                Button("Open", async (_, _) => await OpenSchematicAsync()),
                Button("Save", async (_, _) => await SaveActiveAsync()),
                Button("Undo", (_, _) => UndoActive()),
                Button("Redo", (_, _) => RedoActive()),
                Button("Copy", async (_, _) => await CopySelectionAsync()),
                Button("Paste", async (_, _) => await PasteSelectionAsync()),
                Button("Delete", (_, _) => ActiveCanvas?.DeleteSelection()),
                Button("Wire", (_, _) => BeginWireTool()),
                Button("Run", (_, _) => RunSimulation()),
                Button("+", (_, _) => ZoomActive(1.2), 36),
                Button("-", (_, _) => ZoomActive(1 / 1.2), 36),
                Button("Fit", (_, _) => ActiveCanvas?.FitToView())
            }
        };
        return toolbar;
    }

    private static Button Button(string text, EventHandler<global::Avalonia.Interactivity.RoutedEventArgs> click, double minWidth = 64)
    {
        Button button = new Button { Content = text, MinWidth = minWidth };
        button.Click += click;
        return button;
    }

    private static Border Panel(string title, Control content)
    {
        DockPanel dock = new DockPanel();
        TextBlock header = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.Bold,
            Margin = new global::Avalonia.Thickness(8, 8, 8, 4)
        };
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        dock.Children.Add(content);

        return new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new global::Avalonia.Thickness(1),
            Child = dock
        };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        string[] paths = Environment.GetCommandLineArgs()
            .Skip(1)
            .Where(i => i.EndsWith(".schx", StringComparison.OrdinalIgnoreCase) && File.Exists(i))
            .ToArray();

        if (paths.Length > 0)
            foreach (string path in paths)
                LoadSchematic(path);
        else
            NewDocument();

        string? screenshotPath = Environment.GetEnvironmentVariable("LIVESPICE_SCREENSHOT");
        if (!string.IsNullOrWhiteSpace(screenshotPath))
            Dispatcher.UIThread.Post(() => SaveScreenshotAndExit(screenshotPath), DispatcherPriority.ApplicationIdle);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!closeConfirmed && documents.Items.OfType<TabItem>().Any(i => (i.Tag as SchematicDocument)?.Dirty == true))
        {
            e.Cancel = true;
            Dispatcher.UIThread.Post(async () =>
            {
                if (await CloseAllDocumentsAsync())
                {
                    closeConfirmed = true;
                    Close();
                }
            });
            return;
        }

        settings.WindowWidth = Width;
        settings.WindowHeight = Height;
        settings.Save();
        base.OnClosing(e);
    }

    private async System.Threading.Tasks.Task OpenSchematicAsync()
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open LiveSPICE schematic",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Circuit Schematics") { Patterns = new[] { "*.schx" } },
                FilePickerFileTypes.All
            }
        });

        foreach (string path in files.Select(OpenPath).Where(i => i != null).Cast<string>())
            LoadSchematic(path);
    }

    internal static string? OpenPath(IStorageFile file)
    {
        return OpenPath(file.TryGetLocalPath(), file.Path);
    }

    internal static string? OpenPath(string? localPath, Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(localPath))
            return localPath;

        if (uri.IsFile)
            return uri.LocalPath;

        return null;
    }

    private void LoadSchematic(string path)
    {
        try
        {
                string fullPath = Path.GetFullPath(path);
                TabItem? existing = documents.Items.OfType<TabItem>()
                    .FirstOrDefault(i => string.Equals(Path.GetFullPath(((SchematicDocument)i.Tag!).FilePath ?? string.Empty), fullPath, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    documents.SelectedItem = existing;
                    status.Text = $"Selected {Path.GetFileName(path)}";
                    return;
                }

                ReplaceUntouchedStartupDocument();
                AddDocument(SchematicDocument.Open(path));
                settings.MarkUsed(path);
                RefreshRecentFilesMenu();
                status.Text = $"Loaded {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            status.Text = ex.Message;
        }
    }

    private void ReplaceUntouchedStartupDocument()
    {
        if (documents.Items.Count != 1)
            return;

        if (documents.Items[0] is TabItem { Tag: SchematicDocument document } tab && IsUntouchedStartupDocument(document))
            documents.Items.Remove(tab);
    }

    internal static bool IsUntouchedStartupDocument(SchematicDocument document)
    {
        return document.FilePath == null && !document.Dirty && !document.Schematic.Elements.Any();
    }

        private void NewDocument()
        {
            AddDocument(SchematicDocument.New());
            status.Text = "Created new schematic";
        }

        private void AddDocument(SchematicDocument document)
        {
            SchematicCanvas canvas = new SchematicCanvas { Document = document };
            canvas.SelectionChanged += OnCanvasSelectionChanged;
            canvas.DocumentChanged += () =>
            {
                RefreshDocumentHeaders();
                status.Text = document.Title;
            };
            canvas.ContextMenu = BuildCanvasContextMenu();

            TabItem tab = new TabItem
            {
                Header = document.Title,
                Content = canvas,
                Tag = document
            };
            documents.Items.Add(tab);
            documents.SelectedItem = tab;
            canvas.FitToView();
        }

        private ContextMenu BuildCanvasContextMenu()
        {
            return new ContextMenu
            {
                ItemsSource = new Control[]
                {
                    MenuItem("Undo", (_, _) => UndoActive()),
                    MenuItem("Redo", (_, _) => RedoActive()),
                    new Separator(),
                    MenuItem("Cut", async (_, _) => await CutSelectionAsync()),
                    MenuItem("Copy", async (_, _) => await CopySelectionAsync()),
                    MenuItem("Paste", async (_, _) => await PasteSelectionAsync()),
                    MenuItem("Delete", (_, _) => ActiveCanvas?.DeleteSelection()),
                    new Separator(),
                    MenuItem("Rotate Left", (_, _) => ActiveCanvas?.RotateSelection(1)),
                    MenuItem("Rotate Right", (_, _) => ActiveCanvas?.RotateSelection(-1)),
                    MenuItem("Flip", (_, _) => ActiveCanvas?.FlipSelection()),
                    MenuItem("Select All", (_, _) => ActiveCanvas?.SelectAll())
                }
            };
        }

        private void RefreshDocumentHeaders()
        {
            foreach (TabItem item in documents.Items.OfType<TabItem>())
                if (item.Tag is SchematicDocument document)
                    item.Header = document.Title;
        }

        private void OnActiveDocumentChanged()
        {
            if (suppressSelectionEvent)
                return;

            propertyInspector.SetSelectedObjects(ActiveCanvas?.SelectedObjects ?? Array.Empty<object>());
            if (ActiveDocument != null)
                Title = $"LiveSPICE Avalonia - {ActiveDocument.Title}";
        }

        private void OnCanvasSelectionChanged()
        {
            propertyInspector.SetSelectedObjects(ActiveCanvas?.SelectedObjects ?? Array.Empty<object>());
        }

        private void ZoomActive(double multiplier)
        {
            if (ActiveCanvas != null)
                ActiveCanvas.Zoom *= multiplier;
        }

        private async System.Threading.Tasks.Task CloseActiveAsync()
        {
            if (ActiveTab != null && await TryCloseTabAsync(ActiveTab))
            {
                documents.Items.Remove(ActiveTab);
                if (documents.Items.Count == 0)
                    NewDocument();
            }
        }

        private async System.Threading.Tasks.Task<bool> CloseAllDocumentsAsync()
        {
            foreach (TabItem tab in documents.Items.OfType<TabItem>().ToList())
            {
                documents.SelectedItem = tab;
                if (!await TryCloseTabAsync(tab))
                    return false;
                documents.Items.Remove(tab);
            }

            return true;
        }

        private async System.Threading.Tasks.Task<bool> TryCloseTabAsync(TabItem tab)
        {
            if (tab.Tag is not SchematicDocument document || !document.Dirty)
                return true;

            SaveChoice choice = await SavePromptWindow.ShowAsync(this, document.Title);
            if (choice == SaveChoice.Cancel)
                return false;
            if (choice == SaveChoice.Discard)
                return true;

            documents.SelectedItem = tab;
            await SaveActiveAsync();
            return !document.Dirty;
        }

        private async System.Threading.Tasks.Task CheckExternalModificationsAsync()
        {
            List<TabItem> modified = documents.Items.OfType<TabItem>()
                .Where(i => (i.Tag as SchematicDocument)?.WasModifiedExternally() == true)
                .ToList();
            if (modified.Count == 0)
                return;

            string names = string.Join("\n", modified.Select(i => ((SchematicDocument)i.Tag!).FilePath));
            if (!await ConfirmWindow.ShowAsync(this, "Reload Modified Schematics", "Reload schematics modified outside LiveSPICE?\n\n" + names))
                return;

            foreach (TabItem tab in modified)
            {
                if (tab.Tag is not SchematicDocument document || document.FilePath == null)
                    continue;

                int index = documents.Items.IndexOf(tab);
                documents.Items.Remove(tab);
                SchematicDocument reloaded = SchematicDocument.Open(document.FilePath);
                SchematicCanvas canvas = new SchematicCanvas { Document = reloaded };
                canvas.SelectionChanged += OnCanvasSelectionChanged;
                canvas.DocumentChanged += () =>
                {
                    RefreshDocumentHeaders();
                    status.Text = reloaded.Title;
                };
                canvas.ContextMenu = BuildCanvasContextMenu();
                TabItem replacement = new TabItem { Header = reloaded.Title, Content = canvas, Tag = reloaded };
                documents.Items.Insert(index, replacement);
                documents.SelectedItem = replacement;
                canvas.FitToView();
            }

            RefreshDocumentHeaders();
            status.Text = "Reloaded externally modified schematics";
        }

        private async System.Threading.Tasks.Task SaveActiveAsync()
        {
            if (ActiveDocument == null)
                return;

            if (ActiveDocument.FilePath == null)
                await SaveActiveAsAsync();
            else
                SaveDocument(ActiveDocument, ActiveDocument.FilePath);
        }

        private async System.Threading.Tasks.Task SaveActiveAsAsync()
        {
            if (ActiveDocument == null)
                return;

            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save LiveSPICE schematic",
                SuggestedFileName = ActiveDocument.FilePath == null ? "Untitled.schx" : Path.GetFileName(ActiveDocument.FilePath),
                FileTypeChoices = new[] { new FilePickerFileType("Circuit Schematics") { Patterns = new[] { "*.schx" } } },
                DefaultExtension = "schx"
            });

            string? path = file?.TryGetLocalPath();
            if (path != null)
                SaveDocument(ActiveDocument, path);
        }

        private async System.Threading.Tasks.Task SaveAllAsync()
        {
            foreach (TabItem tab in documents.Items.OfType<TabItem>().ToList())
            {
                suppressSelectionEvent = true;
                documents.SelectedItem = tab;
                suppressSelectionEvent = false;
                await SaveActiveAsync();
            }
            OnActiveDocumentChanged();
        }

        private void SaveDocument(SchematicDocument document, string path)
        {
            try
            {
                document.Save(path);
                settings.MarkUsed(path);
                settings.Save();
                RefreshRecentFilesMenu();
                RefreshDocumentHeaders();
                status.Text = $"Saved {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        private void RefreshRecentFilesMenu()
        {
            List<Control> items = settings.ExistingRecentFiles()
                .Select(path => MenuItem(CompactPath(path, 48), (_, _) => LoadSchematic(path)))
                .Cast<Control>()
                .ToList();

            if (items.Count == 0)
                items.Add(new MenuItem { Header = "(empty)", IsEnabled = false });

            recentFilesMenu.ItemsSource = items;
        }

        private static string CompactPath(string path, int maxLength)
        {
            if (path.Length <= maxLength)
                return path;

            string file = Path.GetFileName(path);
            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            int keep = Math.Max(0, maxLength - file.Length - 5);
            if (directory.Length > keep)
                directory = "..." + directory[^keep..];
            return Path.Combine(directory, file);
        }

        private void PopulateComponents()
        {
            List<ComponentListItem> items = CommonComponents.Select(i => new ComponentListItem(i)).ToList();
            Type root = typeof(CircuitComponent);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(i => !i.IsDynamic))
            {
                try
                {
                    foreach (Type type in assembly.GetTypes().Where(i => i.IsPublic && !i.IsAbstract && root.IsAssignableFrom(i) && i.CustomAttribute<ObsoleteAttribute>() == null))
                        if (items.All(i => i.ComponentType != type))
                            items.Add(new ComponentListItem(type));
                }
                catch
                {
                }
            }

            // Three tiers, so a generic Diode does not sit beside the specific ones: the handful
            // of everyday components, then the remaining generic types, then the part libraries.
            allComponents = items
                .Select(i => i.InCategory(CommonComponents.Contains(i.ComponentType!) ? BasicCategory : TypesCategory))
                .Concat(ComponentLibrary.Load().Select(i => new ComponentListItem(i)))
                .ToList();
            ApplyComponentFilter();
        }

        private void ApplyComponentFilter()
        {
            string filter = componentFilter.Text?.Trim() ?? string.Empty;
            IEnumerable<ComponentListItem> matches = filter.Length == 0
                ? allComponents
                : allComponents.Where(i =>
                    i.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    i.Category.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    i.Description.Contains(filter, StringComparison.OrdinalIgnoreCase));

            // Group into collapsible categories so a generic Diode does not sit at the same level
            // as the forty-odd specific diodes. While filtering, expand everything that matched -
            // hiding results behind a collapsed header would defeat the search.
            List<TreeViewItem> categories = new List<TreeViewItem>();
            foreach (IGrouping<string, ComponentListItem> group in matches.GroupBy(i => i.Category).OrderBy(i => CategoryOrder(i.Key)).ThenBy(i => i.Key))
            {
                TreeViewItem category = new TreeViewItem
                {
                    Header = $"{group.Key}  ({group.Count()})",
                    FontWeight = FontWeight.Bold,
                    IsExpanded = filter.Length > 0 || group.Key == BasicCategory,
                    ItemsSource = group
                        .OrderBy(i => i.Name)
                        .Select(i => new TreeViewItem { Header = i.Name, Tag = i, FontWeight = FontWeight.Normal })
                        .ToList()
                };
                categories.Add(category);
            }
            componentList.ItemsSource = categories;
        }

        // Named to match the WPF app's palette (ComponentLibrary.xaml.cs), so the layout is
        // familiar: "Generic" is the unconfigured component you fill in yourself, as opposed to a
        // pre-configured part from a library.
        private const string BasicCategory = "Common";
        private const string TypesCategory = "Generic";

        /// <summary>Basic first, then the generic types, then the part libraries alphabetically.</summary>
        private static int CategoryOrder(string Category)
        {
            if (Category == BasicCategory) return 0;
            if (Category == TypesCategory) return 1;
            return 2;
        }

        private void ActivateSelectedComponent()
        {
            if (componentList.SelectedItem is not TreeViewItem node || node.Tag is not ComponentListItem item || ActiveCanvas == null)
                return;

            if (item.ComponentType == typeof(Conductor))
            {
                ActiveCanvas.BeginWireTool();
                status.Text = "Wire tool: click two grid points";
                return;
            }

            ActiveCanvas.PendingComponent = item.Create();
            status.Text = $"Place {item.Name}: click schematic";
        }

        private void BeginWireTool()
        {
            componentList.SelectedItem = null;
            ActiveCanvas?.BeginWireTool();
            status.Text = "Wire tool: click two grid points";
        }

        private void ValidateActiveCircuit()
        {
            try
            {
                ActiveDocument?.Schematic.Build();
                status.Text = "Circuit build succeeded";
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

    private void SaveScreenshotAndExit(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        RenderTargetBitmap bitmap = new RenderTargetBitmap(new PixelSize((int)Bounds.Width, (int)Bounds.Height), new Vector(96, 96));
        bitmap.Render(this);
        bitmap.Save(path);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void UndoActive()
    {
        ActiveDocument?.Undo();
        ActiveCanvas?.InvalidateVisual();
        RefreshDocumentHeaders();
        propertyInspector.SetSelectedObjects(ActiveCanvas?.SelectedObjects ?? Array.Empty<object>());
        status.Text = "Undo";
    }

    private void RedoActive()
    {
        ActiveDocument?.Redo();
        ActiveCanvas?.InvalidateVisual();
        RefreshDocumentHeaders();
        propertyInspector.SetSelectedObjects(ActiveCanvas?.SelectedObjects ?? Array.Empty<object>());
        status.Text = "Redo";
    }

    private void RunSimulation()
    {
        try
        {
            if (ActiveDocument == null)
                return;

            new WaveformWindow(ActiveDocument.Schematic, settings).Show();
            status.Text = "Simulation window opened";
        }
        catch (Exception ex)
        {
            status.Text = ex.Message;
        }
    }

    private async System.Threading.Tasks.Task CopySelectionAsync()
    {
        string? xml = ActiveCanvas?.CopySelectionXml();
        if (!string.IsNullOrWhiteSpace(xml) && Clipboard != null)
        {
            await Clipboard.SetTextAsync(xml);
            status.Text = "Copied selection";
        }
    }

    private async System.Threading.Tasks.Task CutSelectionAsync()
    {
        await CopySelectionAsync();
        ActiveCanvas?.DeleteSelection();
        status.Text = "Cut selection";
    }

    private async System.Threading.Tasks.Task PasteSelectionAsync()
    {
        if (ActiveCanvas == null || Clipboard == null)
            return;

        string? xml = await Clipboard.GetTextAsync();
        if (!string.IsNullOrWhiteSpace(xml) && ActiveCanvas.PasteSelectionXml(xml))
            status.Text = "Pasted selection";
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        KeyModifiers modifiers = e.KeyModifiers;
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.N:
                    NewDocument();
                    e.Handled = true;
                    return;
                case Key.O:
                    await OpenSchematicAsync();
                    e.Handled = true;
                    return;
                case Key.S when modifiers.HasFlag(KeyModifiers.Shift):
                    await SaveAllAsync();
                    e.Handled = true;
                    return;
                case Key.S:
                    await SaveActiveAsync();
                    e.Handled = true;
                    return;
                case Key.C:
                    await CopySelectionAsync();
                    e.Handled = true;
                    return;
                case Key.X:
                    await CutSelectionAsync();
                    e.Handled = true;
                    return;
                case Key.Z:
                    UndoActive();
                    e.Handled = true;
                    return;
                case Key.Y:
                    RedoActive();
                    e.Handled = true;
                    return;
                case Key.V:
                    await PasteSelectionAsync();
                    e.Handled = true;
                    return;
                case Key.A:
                    ActiveCanvas?.SelectAll();
                    e.Handled = true;
                    return;
            }
        }

        switch (e.Key)
        {
            case Key.Delete:
                ActiveCanvas?.DeleteSelection();
                e.Handled = true;
                break;
            case Key.F5:
                RunSimulation();
                e.Handled = true;
                break;
            case Key.Left:
                ActiveCanvas?.RotateSelection(1);
                e.Handled = true;
                break;
            case Key.Right:
                ActiveCanvas?.RotateSelection(-1);
                e.Handled = true;
                break;
            case Key.Up:
            case Key.Down:
                ActiveCanvas?.FlipSelection();
                e.Handled = true;
                break;
        }
    }
}

internal sealed class ComponentListItem
{
    private readonly LibraryPart? part;

    public ComponentListItem(Type componentType, string category = "")
    {
        ComponentType = componentType;
        CircuitComponent component = (CircuitComponent)Activator.CreateInstance(componentType)!;
        Name = component.TypeName;
        Category = category;
        Description = componentType.CustomAttribute<DescriptionAttribute>()?.Description ?? componentType.Name;
    }

    /// <summary>This item filed under a different category.</summary>
    public ComponentListItem InCategory(string category)
    {
        return ComponentType != null ? new ComponentListItem(ComponentType, category) : this;
    }

    public ComponentListItem(LibraryPart part)
    {
        this.part = part;
        Name = part.Name;
        Category = part.Category;
        Description = part.Description;
    }

    /// <summary>Null for library parts, which are built by deserializing their library entry.</summary>
    public Type? ComponentType { get; }

    public string Name { get; }

    public string Category { get; }

    public string Description { get; }

    public CircuitComponent Create()
    {
        if (part != null)
            return (CircuitComponent)part.Create();
        return (CircuitComponent)Activator.CreateInstance(ComponentType!)!;
    }

    public override string ToString()
    {
        return Category.Length > 0 ? $"{Name}  ({Category})" : Name;
    }
}