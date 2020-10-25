using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiveSPICE
{
    public static class Commands
    {
        public static RoutedCommand Exit { get { return exit; } }
        public static RoutedCommand SaveAll { get { return saveAll; } }
        public static RoutedCommand ZoomFit { get { return zoomFit; } }

        public static RoutedCommand Simulate { get { return simulate; } }

        public static RoutedCommand Rename { get { return rename; } }

        static Commands()
        {
            exit = new RoutedUICommand("Exit", "Exit", typeof(Commands));
            exit.InputGestures.Add(new KeyGesture(Key.F4, ModifierKeys.Alt));

            saveAll = new RoutedUICommand("Save All", "Save All", typeof(Commands));
            saveAll.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));

            zoomFit = new RoutedUICommand("Zoom Fit", "Zoom Fit", typeof(Commands));

            simulate = new RoutedUICommand("Simulate", "Simulate", typeof(Commands));
            simulate.InputGestures.Add(new KeyGesture(Key.F5));

            rename = new RoutedUICommand("Rename", "Rename", typeof(Commands));
            rename.InputGestures.Add(new KeyGesture(Key.F2));
        }
        static RoutedCommand exit;
        static RoutedCommand saveAll;
        static RoutedCommand zoomFit;
        static RoutedCommand simulate;
        static RoutedCommand rename;
    }

    public static class Images
    {
        public static readonly BitmapImage Clear = new BitmapImage(new Uri(@"pack://application:,,,/Images/Clear.png"));
        public static readonly BitmapImage Copy = new BitmapImage(new Uri(@"pack://application:,,,/Images/Copy.png"));
        public static readonly BitmapImage Cut = new BitmapImage(new Uri(@"pack://application:,,,/Images/Cut.png"));
        public static readonly BitmapImage Gears = new BitmapImage(new Uri(@"pack://application:,,,/Images/Gears.png"));
        public static readonly BitmapImage New = new BitmapImage(new Uri(@"pack://application:,,,/Images/New.png"));
        public static readonly BitmapImage Open = new BitmapImage(new Uri(@"pack://application:,,,/Images/Open.png"));
        public static readonly BitmapImage Paste = new BitmapImage(new Uri(@"pack://application:,,,/Images/Paste.png"));
        public static readonly BitmapImage Pause = new BitmapImage(new Uri(@"pack://application:,,,/Images/Pause.png"));
        public static readonly BitmapImage Redo = new BitmapImage(new Uri(@"pack://application:,,,/Images/Redo.png"));
        public static readonly BitmapImage Restart = new BitmapImage(new Uri(@"pack://application:,,,/Images/Restart.png"));
        public static readonly BitmapImage Save = new BitmapImage(new Uri(@"pack://application:,,,/Images/Save.png"));
        public static readonly BitmapImage SaveAll = new BitmapImage(new Uri(@"pack://application:,,,/Images/SaveAll.png"));
        public static readonly BitmapImage Start = new BitmapImage(new Uri(@"pack://application:,,,/Images/Start.png"));
        public static readonly BitmapImage Undo = new BitmapImage(new Uri(@"pack://application:,,,/Images/Undo.png"));
        public static readonly BitmapImage ZoomIn = new BitmapImage(new Uri(@"pack://application:,,,/Images/ZoomIn.png"));
        public static readonly BitmapImage ZoomFit = new BitmapImage(new Uri(@"pack://application:,,,/Images/ZoomFit.png"));
        public static readonly BitmapImage ZoomOut = new BitmapImage(new Uri(@"pack://application:,,,/Images/ZoomOut.png"));

        public static ImageSource ForCommand(ICommand Command)
        {
            ImageSource image;
            if (commands.TryGetValue(Command, out image))
                return image;
            return null;
        }

        private static Dictionary<ICommand, ImageSource> commands = new Dictionary<ICommand, ImageSource>()
        {
            { ApplicationCommands.Delete,       Clear },
            { ApplicationCommands.Copy,         Copy },
            { ApplicationCommands.Cut,          Cut },
            { ApplicationCommands.New,          New },
            { ApplicationCommands.Open,         Open },
            { ApplicationCommands.Paste,        Paste },
            { MediaCommands.Pause,              Pause },
            { ApplicationCommands.Redo,         Redo },
            { ApplicationCommands.Save,         Save },
            { Commands.SaveAll,                 SaveAll },
            { Commands.Simulate,                Start },
            { ApplicationCommands.Undo,         Undo },
            { NavigationCommands.Zoom,          ZoomIn },
            { Commands.ZoomFit,                 ZoomFit },
            { NavigationCommands.DecreaseZoom,  ZoomOut },
        };
    }
}
