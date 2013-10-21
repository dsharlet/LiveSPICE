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
using SyMath;

namespace LiveSPICE
{
    public class ProbeTool : SchematicTool
    {
        public SimulationSchematic Simulation { get { return (SimulationSchematic)Target; } }

        protected Circuit.Coord a, b;

        public ProbeTool(SimulationSchematic Target) : base(Target) 
        {
        }

        public override void Begin() { base.Begin(); Target.Cursor = Cursors.Cross; }

        private bool Movable(Circuit.Coord At)
        {
            return 
                ProbesOf(Target.AtPoint(At)).Any(i => ((ElementControl)i.Tag).Selected) &&
                (Keyboard.Modifiers & ModifierKeys.Control) == 0;
        }

        public override void MouseDown(Circuit.Coord At)
        {
            a = b = At;
        }

        public override void MouseMove(Circuit.Coord At)
        {
            b = At;
            Target.Highlight(ProbesOf(Target.AtPoint(b)));
        }

        protected static Circuit.EdgeType[] Colors =
        {
            Circuit.EdgeType.Red,
            Circuit.EdgeType.Green,
            Circuit.EdgeType.Blue,
            Circuit.EdgeType.Yellow,
            Circuit.EdgeType.Cyan,
            Circuit.EdgeType.Magenta,
        };

        public override void MouseUp(Circuit.Coord At)
        {
            b = At;
            Target.Select();
            if (a == b)
            {
                IEnumerable<Circuit.Element> at = Target.AtPoint(a);
                IEnumerable<Circuit.Symbol> probes = ProbesOf(at);
                if (!probes.Any() && at.Any(i => i is Circuit.Wire))
                {
                    Probe p = new Probe(Colors.ArgMin(i => Simulation.Probes.Count(j => j.Color == i)));
                    Target.Schematic.Add(new Circuit.Symbol(p) { Position = a });
                }
                else if (probes.Any())
                {
                    Target.Select(ProbesOf(Target.AtPoint(a)));
                }
            }
        }

        private static IEnumerable<Circuit.Symbol> ProbesOf(IEnumerable<Circuit.Element> Of)
        {
            return Of.OfType<Circuit.Symbol>().Where(i => i.Component is Probe);
        }

        private static Point ToPoint(Circuit.Coord x) { return new Point(x.x, x.y); }
    }
}
