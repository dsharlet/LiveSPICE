using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SchematicControls;

namespace LiveSPICE
{
    class ProbeTool : SchematicTool
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
            Circuit.EdgeType.Orange,
        };

        public override void MouseUp(Circuit.Coord At)
        {
            b = At;
            Target.Select();
            if (a == b)
            {
                Circuit.Node node = Simulation.NodeAt(At);
                IEnumerable<Circuit.Element> at = Target.AtPoint(a);
                IEnumerable<Circuit.Symbol> probes = ProbesOf(at);
                if (!probes.Any() && node != null)
                {
                    Probe probe = Simulation.Probes.FirstOrDefault(i => i.ConnectedTo == node);
                    if (probe != null)
                    {
                        // There's already a probe on this node, move the probe here.
                        ((Circuit.Symbol)probe.Tag).Position = a;
                    }
                    else
                    {
                        // Make a new probe connected to this node.
                        probe = new Probe(Colors.ArgMin(i => Simulation.Probes.Count(j => j.Color == i)));
                        Target.Schematic.Add(new Circuit.Symbol(probe) { Position = a });
                    }
                }
                else
                {
                    Target.Select(probes.FirstOrDefault());
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
