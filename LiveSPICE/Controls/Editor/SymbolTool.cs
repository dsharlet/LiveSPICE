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
    /// SchematicTool for adding symbols to the schematic.
    /// </summary>
    public class SymbolTool : EditorTool
    {
        protected SymbolControl overlay;
        
        public SymbolTool(SchematicEditor Target, Type Type) : base(Target)
        {
            overlay = new SymbolControl(Type) 
            { 
                Visibility = Visibility.Hidden, 
                ShowText = false,
                Highlighted = true,
                Pen = new Pen(Brushes.Gray, 1.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }
            };
        }

        public override void Begin() { base.Begin(); Target.overlays.Children.Add(overlay); overlay.UpdateLayout(); Target.Cursor = Cursors.None; }
        public override void End() { Target.overlays.Children.Remove(overlay); base.End(); }

        public override void MouseDown(Point At)
        {
            if (overlay.Visibility != Visibility.Visible)
                return;

            Circuit.Symbol S = new Circuit.Symbol((Circuit.Component)Activator.CreateInstance(overlay.Component.GetType()))
            {
                Position = overlay.Symbol.Position,
                Rotation = overlay.Symbol.Rotation,
                Flip = overlay.Symbol.Flip,
            };
            Editor.Add(S);
            
            overlay.Pen.Brush = Brushes.Black;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                Target.Tool = new SelectionTool(Editor);
        }
        public override void MouseUp(Point At)
        {
            overlay.Pen.Brush = Brushes.Gray;
        }

        public override void MouseMove(Point At)
        {
            Circuit.Symbol symbol = overlay.Symbol;

            symbol.Position = new Circuit.Coord((int)At.X, (int)At.Y);

            // Don't allow symbols to be placed on an existing symbol.
            Target.Cursor = Target.InRect(symbol.LowerBound, symbol.UpperBound).Any() ? Cursors.No : Cursors.None;
            overlay.Visibility = Visibility.Visible;
        }

        public override void MouseLeave(Point At)
        {
            overlay.Visibility = Visibility.Hidden;
        }
        
        public override bool KeyDown(Key Key)
        {
            Circuit.Symbol symbol = overlay.Symbol;

            Circuit.Coord x = symbol.Position;
            switch (Key)
            {
                case System.Windows.Input.Key.Left: symbol.Rotation += 1; break;
                case System.Windows.Input.Key.Right: symbol.Rotation -= 1; break;
                case System.Windows.Input.Key.Down: symbol.Flip = !symbol.Flip; break;
                case System.Windows.Input.Key.Up: symbol.Flip = !symbol.Flip; break;
                default: return base.KeyDown(Key);
            }

            symbol.Position = x;
            return true;
        }
    }
}
