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

namespace LiveSPICEVst.ViewModels
{

    /// <summary>
    /// Simple wrapper around IPotControl to make UI integration easier
    /// </summary>
    public class PotViewModel : ComponentViewModel
    {
        IPotControl _pot;

        public PotViewModel(IPotControl pot, string name)
        {
            _pot = pot;
            Name = name;
        }

        public double PotValue
        {
            get
            {
                return _pot.PotValue;
            }

            set
            {
                if (_pot.PotValue != value)
                {
                    _pot.PotValue = value;

                    NeedUpdate = true;
                }
            }
        }
    }
}
