using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Token component for storing circuit labels.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Label")]
    public class Label : Component
    {
        public override IEnumerable<Terminal> Terminals { get { return new Terminal[0]; } }

        private string text = "Label";
        private string subtext = "";
        [Serialize]
        public string Text { get { return text; } set { text = value; NotifyChanged("Text"); } }
        [Serialize]
        public string Subtext { get { return subtext; } set { subtext = value; NotifyChanged("Subtext"); } }

        public Label() { Name = "_1"; }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.DrawLine(EdgeType.Gray, new Point(-20, -10), new Point(20, -10));
            Sym.DrawLine(EdgeType.Gray, new Point(-20, -10), new Point(-20, 10));
            Sym.InBounds(new Coord(-20, -10), new Coord(20, 10));
            Sym.DrawText(text, new Point(-18, 0), Alignment.Near, Alignment.Center, Size.Large);
            Sym.DrawText(subtext, new Point(-18, -10), Alignment.Near, Alignment.Far, Size.Normal);
        }
    }
}