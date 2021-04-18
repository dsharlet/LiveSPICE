using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    public abstract class Decoration : Component
    {
        public override sealed void Analyze(Analysis Mna) { }
    }

    /// <summary>
    /// Token component for storing circuit labels.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Label")]
    [DefaultProperty("Text")]
    [Description("Displays a text label on a schematic.")]
    public class Label : Decoration
    {
        public override IEnumerable<Terminal> Terminals { get { return new Terminal[0]; } }

        private string text = "Label";
        private string subtext = "";
        [Serialize]
        public string Text { get { return text; } set { text = value; NotifyChanged(nameof(Text)); } }
        [Serialize]
        public string Subtext { get { return subtext; } set { subtext = value; NotifyChanged(nameof(Subtext)); } }

        public Label() { Name = "_1"; }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.DrawLine(EdgeType.Gray, new Point(-20, -10), new Point(20, -10));
            Sym.DrawLine(EdgeType.Gray, new Point(-20, -10), new Point(-20, 10));
            Sym.InBounds(new Coord(-20, -10), new Coord(20, 10));
            Sym.DrawText(() => text, new Point(-18, 0), Alignment.Near, Alignment.Center, Size.Large);
            Sym.DrawText(() => subtext, new Point(-18, -10), Alignment.Near, Alignment.Far, Size.Normal);
        }
    }
}