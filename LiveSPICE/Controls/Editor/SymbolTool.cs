using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SchematicControls;

namespace LiveSPICE
{
    /// <summary>
    /// SchematicTool for adding symbols to the schematic.
    /// </summary>
    public class SymbolTool : EditorTool
    {
        protected SymbolControl overlay;

        public SymbolTool(SchematicEditor Target, Circuit.Component C) : base(Target)
        {
            overlay = new SymbolControl(C)
            {
                Visibility = Visibility.Hidden,
                ShowText = false,
                Highlighted = true,
                Pen = new Pen(Brushes.Gray, 1.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }
            };
        }

        public override void Begin() { base.Begin(); Target.AddOverlay(overlay); Target.Cursor = Cursors.None; }
        public override void End() { Target.RemoveOverlay(overlay); base.End(); }

        public override void MouseDown(Circuit.Coord At)
        {
            if (overlay.Visibility != Visibility.Visible)
                return;

            Circuit.Symbol S = new Circuit.Symbol(overlay.Component.Clone())
            {
                Position = overlay.Symbol.Position,
                Rotation = overlay.Symbol.Rotation,
                Flip = overlay.Symbol.Flip,
            };
            Editor.Add(S);
            Editor.Select(S);

            overlay.Pen.Brush = Brushes.Black;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                Target.Tool = new SelectionTool(Editor);
        }
        public override void MouseUp(Circuit.Coord At)
        {
            overlay.Pen.Brush = Brushes.Gray;
        }

        public override void MouseMove(Circuit.Coord At)
        {
            Circuit.Symbol symbol = overlay.Symbol;

            symbol.Position = At;

            // Don't allow symbols to be placed on an existing symbol.
            //Target.Cursor = Target.InRect(symbol.LowerBound, symbol.UpperBound).Any() ? Cursors.No : Cursors.None;
            overlay.Visibility = Visibility.Visible;
        }

        public override void MouseLeave(Circuit.Coord At)
        {
            overlay.Visibility = Visibility.Hidden;
        }

        public override bool KeyDown(KeyEventArgs Event)
        {
            Circuit.Symbol symbol = overlay.Symbol;
            switch (Event.Key)
            {
                case Key.Left: symbol.Rotation += 1; break;
                case Key.Right: symbol.Rotation -= 1; break;
                case Key.Down: symbol.Flip = !symbol.Flip; break;
                case Key.Up: symbol.Flip = !symbol.Flip; break;
                default: return base.KeyDown(Event);
            }
            return true;
        }
    }
}
