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
    public class SchematicTool
    {
        private SchematicControl target;
        public SchematicControl Target { get { return target; } }

        public SchematicTool(SchematicControl Target) { target = Target; }

        public virtual void Begin() { Target.Cursor = Cursors.Cross; }
        public virtual void End() { }
        public virtual void Cancel() { }
                
        public virtual void MouseDown(Point At) { }
        public virtual void MouseMove(Point At) { }
        public virtual void MouseUp(Point At) { }
        public virtual void MouseEnter(Point At) { }
        public virtual void MouseLeave(Point At) { }

        public virtual bool KeyDown(Key Key) 
        {
            switch (Key)
            {
                case System.Windows.Input.Key.Escape:
                    Target.Tool = new SchematicTool(Target); 
                    return true;
                default: return false;
            }
        }
        public virtual bool KeyUp(Key Key) { return false; }
    }
}
