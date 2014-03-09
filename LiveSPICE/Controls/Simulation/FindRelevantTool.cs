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
    class FindRelevantTool : SchematicTool
    {
        protected Circuit.Coord a, b;

        public FindRelevantTool(SchematicControl Target)
            : base(Target) 
        {
        }

        public Func<Circuit.Element, bool> Relevant = (x) => true;
        public Action<IEnumerable<Circuit.Element>> Clicked = null;

        public override void Begin() { base.Begin(); Target.Cursor = Cursors.Hand; }

        private bool Movable(Circuit.Coord At)
        {
            return
                RelevantOf(Target.AtPoint(At)).Any(i => ((ElementControl)i.Tag).Selected) &&
                (Keyboard.Modifiers & ModifierKeys.Control) == 0;
        }

        public override void MouseDown(Circuit.Coord At)
        {
            a = b = At;
        }

        public override void MouseMove(Circuit.Coord At)
        {
            b = At;
            Target.Highlight(RelevantOf(Target.AtPoint(b)));
        }
        
        public override void MouseUp(Circuit.Coord At)
        {
            b = At;
            if (a == b)
                if (Clicked != null)
                    Clicked(RelevantOf(Target.AtPoint(a)));
        }

        private IEnumerable<Circuit.Element> RelevantOf(IEnumerable<Circuit.Element> Of)
        {
            return Of.OfType<Circuit.Symbol>().Where(i => Relevant(i));
        }

        private static Point ToPoint(Circuit.Coord x) { return new Point(x.x, x.y); }
    }
}
