using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        static Commands()
        {
            exit = new RoutedUICommand("Exit", "Exit", typeof(Commands));
            exit.InputGestures.Add(new KeyGesture(Key.F4, ModifierKeys.Alt));

            saveAll = new RoutedUICommand("Save All", "Save All", typeof(Commands));
            saveAll.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));

            zoomFit = new RoutedUICommand("Zoom Fit", "Zoom Fit", typeof(Commands));
        }
        static RoutedCommand exit;
        static RoutedCommand saveAll;
        static RoutedCommand zoomFit;
    }

    public static class CommandImages
    {
        public static Dictionary<ICommand, ImageSource> Images = new Dictionary<ICommand, ImageSource>()
        {
            { ApplicationCommands.Delete, new BitmapImage(new Uri(@"pack://application:,,,/Images/Clear.png")) },
            { ApplicationCommands.Copy, new BitmapImage(new Uri(@"pack://application:,,,/Images/Copy.png")) },
            { ApplicationCommands.Cut, new BitmapImage(new Uri(@"pack://application:,,,/Images/Cut.png")) },
            { ApplicationCommands.New, new BitmapImage(new Uri(@"pack://application:,,,/Images/New.png")) },
            { ApplicationCommands.Open, new BitmapImage(new Uri(@"pack://application:,,,/Images/Open.png")) },
            { ApplicationCommands.Paste, new BitmapImage(new Uri(@"pack://application:,,,/Images/Paste.png")) },
            { MediaCommands.Pause, new BitmapImage(new Uri(@"pack://application:,,,/Images/Pause.png")) },
            { ApplicationCommands.Redo, new BitmapImage(new Uri(@"pack://application:,,,/Images/Redo.png")) },
            { ApplicationCommands.Save, new BitmapImage(new Uri(@"pack://application:,,,/Images/Save.png")) },
            { Commands.SaveAll, new BitmapImage(new Uri(@"pack://application:,,,/Images/SaveAll.png")) },
            { MediaCommands.Play, new BitmapImage(new Uri(@"pack://application:,,,/Images/Start.png")) },
            { ApplicationCommands.Undo, new BitmapImage(new Uri(@"pack://application:,,,/Images/Undo.png")) },
            { NavigationCommands.Zoom, new BitmapImage(new Uri(@"pack://application:,,,/Images/ZoomIn.png")) },
            { Commands.ZoomFit, new BitmapImage(new Uri(@"pack://application:,,,/Images/ZoomFit.png")) },
            { NavigationCommands.DecreaseZoom, new BitmapImage(new Uri(@"pack://application:,,,/Images/ZoomOut.png")) },
        };
    }
}
