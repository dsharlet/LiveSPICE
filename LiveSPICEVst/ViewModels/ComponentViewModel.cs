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

    public class ComponentViewModel : BaseViewModel
    {
        public string Name { get; set; }
        public bool NeedUpdate { get; set; }
        public bool NeedRebuild { get; set; }
    }
}
