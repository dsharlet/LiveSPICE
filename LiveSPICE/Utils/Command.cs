using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace LiveSPICE
{
    /// <summary>
    /// Command implementation mapped to delegates.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Command<T> : ICommand
    {
        protected Action<T> execute;
        protected Predicate<T> canExecute;

        public Command(Action<T> Execute, Predicate<T> CanExecute)
        {
            execute = Execute;
            canExecute = CanExecute;
        }

        public void Execute(object parameter) { execute((T)parameter); }
        public bool CanExecute(object parameter) { return canExecute((T)parameter); }

        protected List<EventHandler> canExecuteChanged = new List<EventHandler>();
        public event EventHandler CanExecuteChanged
        {
            add { canExecuteChanged.Add(value); }
            remove { canExecuteChanged.Remove(value); }
        }
    }
}
