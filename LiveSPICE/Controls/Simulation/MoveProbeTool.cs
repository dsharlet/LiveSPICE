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
    /// <summary>
    /// Tool for moving elements.
    /// </summary>
    public class MoveProbeTool : SimulationTool
    {
        Point x;

        public MoveProbeTool(SimulationSchematic Target, Point At) : base(Target)
        {
            x = At;
        }

        public override void Begin() { base.Begin(); Target.Cursor = Cursors.SizeAll; }
        public override void End() 
        { 
            base.End(); 
        }
        public override void Cancel() { }
        
        public override void MouseUp(Point At)
        {
            Target.Tool = new ProbeSelectionTool(Simulation);
        }

        public override void MouseMove(Point At)
        {
            Circuit.Coord dx = new Circuit.Coord((int)Math.Round(At.X - x.X), (int)Math.Round(At.Y - x.Y));
            if (dx.x != 0 || dx.y != 0)
            {
                foreach (Circuit.Symbol i in Target.Selected)
                    i.Position += dx;
            }
            x = At;
        }
    }
}
