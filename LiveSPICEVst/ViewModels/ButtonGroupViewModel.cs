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
using LiveSPICEVst.MVVM;
using Util;

namespace LiveSPICEVst.ViewModels
{

    /// <summary>
    /// Simple wrapper around IButtonControl to make UI integration easier
    /// </summary>
    public class ButtonGroupViewModel : ComponentViewModel
    {
        readonly List<IButtonControl> _buttons = new List<IButtonControl>();

        public ICommand ClickCommand { get; }

        public int Position
        {
            get => _buttons[0].Position;
            set
            {
                foreach (var button in _buttons)
                {
                    button.Position = value;
                }
                OnPropertyChanged();
            }
        }

        public ButtonGroupViewModel(string name, IButtonControl button)
        {
            Name = name;
            AddButton(button);

            ClickCommand = new RelayCommand(OnClick);
        }

        private void OnClick()
        {
            foreach (var button in _buttons)
            {
                button.Click();
            }
            OnPropertyChanged(nameof(Position));
        }

        public void AddButton(IButtonControl button)
        {
            _buttons.Add(button);
        }
    }
}
