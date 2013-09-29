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
        public static RoutedCommand Exit { get { return exitCommand; } }

        static CustomCommands()
        {
            exitCommand = new RoutedUICommand("Exit", "Exit", typeof(CustomCommands));
            exitCommand.InputGestures.Add(new KeyGesture(Key.F4, ModifierKeys.Alt));
        }
        static RoutedCommand exitCommand;
    }
}
