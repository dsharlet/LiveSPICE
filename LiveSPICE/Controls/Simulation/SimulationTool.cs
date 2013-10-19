using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LiveSPICE
{
    public class SimulationTool : SchematicTool
    {
        public SimulationSchematic Simulation { get { return (SimulationSchematic)Target; } }

        public SimulationTool(SimulationSchematic Target) : base(Target) { }

        public override bool KeyDown(Key Key) 
        {
            switch (Key)
            {
                case System.Windows.Input.Key.Escape:
                    Target.Tool = new ProbeTool(Simulation); 
                    return true;
                default: return false;
            }
        }
    }
}
