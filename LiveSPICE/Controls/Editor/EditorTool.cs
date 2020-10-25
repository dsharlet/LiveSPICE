using System.Windows.Input;

namespace LiveSPICE
{
    public class EditorTool : SchematicTool
    {
        public SchematicEditor Editor { get { return (SchematicEditor)Target; } }

        public EditorTool(SchematicEditor Target) : base(Target) { }

        public override bool KeyDown(KeyEventArgs Event)
        {
            if (Event.Key == Key.Escape)
            {
                Target.Tool = new SelectionTool(Editor);
                return true;
            }
            return false;
        }
    }
}
