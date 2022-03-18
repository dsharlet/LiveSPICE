using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Named wire.
    /// </summary>
    [DisplayName("Named Wire")]
    [Category("Generic")]
    [DefaultProperty("WireName")]
    [Description("Nodes with the same name are connected as if they were connected by a continuous wire.")]
    public class NamedWire : OneTerminal
    {
        public NamedWire() { }

        private string wire = "Name";
        [Serialize, Description("Name of the node connected to this named wire.")]
        public string WireName { get { return wire; } set { wire = value; NotifyChanged(nameof(Wire)); } }

        public override void Analyze(Analysis Mna) { }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Sym.AddWire(Terminal, new Coord(0, -15));
            Sym.AddWire(new Coord(0, -19), new Coord(0, -22));
            Sym.AddWire(new Coord(0, -27), new Coord(0, -30));

            Sym.DrawText(() => WireName, new Coord(2, -15), Alignment.Near, Alignment.Center);
        }
    }
}
