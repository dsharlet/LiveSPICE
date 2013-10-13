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
    public class EditorTool : SchematicTool
    {
        public SchematicEditor Editor { get { return (SchematicEditor)Target; } }

        public EditorTool(SchematicEditor Target) : base(Target) { }

        public override bool KeyDown(Key Key) 
        {
            switch (Key)
            {
                case System.Windows.Input.Key.Escape:
                    Target.Tool = new SelectionTool(Editor); 
                    return true;
                default: return false;
            }
        }
    }
}
