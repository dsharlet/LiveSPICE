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
    public class MoveTool : EditorTool
    {
        Circuit.Coord x;

        public MoveTool(SchematicEditor Target, Circuit.Coord At)
            : base(Target)
        {
            x = At;
        }

        public override void Begin() { base.Begin(); Editor.Edits.BeginEditGroup(); Target.Cursor = Cursors.SizeAll; }
        public override void End() { Editor.Edits.EndEditGroup(); base.End(); }
        public override void Cancel() { Editor.Edits.CancelEditGroup(); Editor.Edits.BeginEditGroup(); }

        public override void MouseUp(Circuit.Coord At)
        {
            Target.Tool = new SelectionTool(Editor);
        }

        public override void MouseMove(Circuit.Coord At)
        {
            Circuit.Coord dx = At - x;
            if (dx.x != 0 || dx.y != 0)
                Editor.Edits.Do(new MoveElements(Target.Selected, dx));
            x = At;
        }
    }
}
