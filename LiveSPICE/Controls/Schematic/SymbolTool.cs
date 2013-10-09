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
    public class SymbolTool : SchematicTool
    {
        protected Symbol overlay;
        
        public SymbolTool(Schematic Target, Type Type) : base(Target)
        {
            overlay = new Symbol(Type) 
            { 
                Visibility = Visibility.Hidden, 
                ShowText = false,
                Pen = new Pen(Brushes.Gray, 1.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }
            };
        }

        public override void Begin() { Target.overlays.Children.Add(overlay); overlay.UpdateLayout(); Target.Cursor = Cursors.None; }
        public override void End() { Target.overlays.Children.Remove(overlay); }

        public override void MouseDown(Point At)
        {
            if (overlay.Visibility != Visibility.Visible)
                return;

            Circuit.Symbol S = new Circuit.Symbol((Circuit.Component)Activator.CreateInstance(overlay.Component.GetType()))
            {
                Rotation = overlay.GetSymbol().Rotation,
                Flip = overlay.GetSymbol().Flip,
                Position = overlay.GetSymbol().Position
            };
            Target.Add(S);
            
            overlay.Pen.Brush = Brushes.Black;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                Target.Tool = null;
        }
        public override void MouseUp(Point At)
        {
            overlay.Pen.Brush = Brushes.Gray;
        }

        public override void MouseMove(Point At)
        {
            //overlay.GetSymbol().Position = At - Target.SnapToGrid(overlay.GetSymbol().Size / 2);

            //// Don't allow symbols to be placed on an existing symbol.
            //Target.Cursor = Target.InRect(
            //    overlay.GetSymbol().Position,
            //    overlay.GetSymbol().Position + (Vector)overlay.Size).Any() ? Cursors.No : Cursors.None;
            //overlay.Visibility = Visibility.Visible;
        }

        public override void MouseLeave(Point At)
        {
            overlay.Visibility = Visibility.Hidden;
        }
        
        public override bool KeyDown(Key Key)
        {
            return base.KeyDown(Key);
            //Point x = overlay.Position + Target.SnapToGrid((Vector)overlay.Size / 2);
            //switch (Key)
            //{
            //    case System.Windows.Input.Key.Left: overlay.Rotation += 1; break;
            //    case System.Windows.Input.Key.Right: overlay.Rotation -= 1; break;
            //    case System.Windows.Input.Key.Down: overlay.Flip = !overlay.Flip; break;
            //    case System.Windows.Input.Key.Up: overlay.Flip = !overlay.Flip; break;
            //    default: return base.KeyDown(Key);
            //}

            //overlay.Position = x - Target.SnapToGrid((Vector)overlay.Size / 2);
            //return true;
        }
    }
}
