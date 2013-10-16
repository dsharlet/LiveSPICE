using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LiveSPICE
{
    public static class CustomCommands
    {
        public static RoutedCommand Exit { get { return exit; } }
        public static RoutedCommand SaveAll { get { return saveAll; } }

        static CustomCommands()
        {
            exit = new RoutedUICommand("Exit", "Exit", typeof(CustomCommands));
            exit.InputGestures.Add(new KeyGesture(Key.F4, ModifierKeys.Alt));

            saveAll = new RoutedUICommand("Save All", "Save All", typeof(CustomCommands));
            saveAll.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        }
        static RoutedCommand exit;
        static RoutedCommand saveAll;
    }
}
