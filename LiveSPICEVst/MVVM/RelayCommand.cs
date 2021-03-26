using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Circuit;
using ComputerAlgebra;
using Util;

namespace LiveSPICEVst.MVVM
{

    public class RelayCommand : ICommand
    {
        private Action _execute;
        private Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() != false;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _execute();
            }
        }
    }
}
