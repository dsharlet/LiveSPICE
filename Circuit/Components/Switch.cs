using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// single-pole N-throw switch.
    /// </summary>
    public abstract class SinglePoleSwitch : Component, IButtonControl
    {
        protected int position = 0;
        [Serialize, Description("Switch position.")]
        public int Position { get { return position; } set { position = value; NotifyChanged(nameof(Position)); } }
        public int NumPositions { get; private set; }

        private string group = "";
        [Serialize, Description("Switch group this switch is a part of.")]
        public string Group { get { return group; } set { group = value; NotifyChanged(nameof(Group)); } }

        public void Click() { Position = (Position + 1) % throws.Length; }

        private Terminal common;
        public Terminal Common { get { return common; } }

        private Terminal[] throws = null;
        public Terminal[] Throws { get { return throws; } }

        public override IEnumerable<Terminal> Terminals { get { return Throws.Append(Common); } }

        public SinglePoleSwitch(int ThrowCount)
        {
            if (ThrowCount < 2 || ThrowCount > 100)
                throw new ArgumentOutOfRangeException("ThrowCount", "ThrowCount must be in [2, 100]");

            throws = new Terminal[ThrowCount];
            for (int i = 0; i < ThrowCount; ++i)
                throws[i] = new Terminal(this, "Throw" + i.ToString());

            common = new Terminal(this, "Common");

            this.NumPositions = ThrowCount;

            Name = "S1";
        }

        public override void Analyze(Analysis Mna)
        {
            if (0 <= position && position < Throws.Length)
                Conductor.Analyze(Mna, Name, Common, Throws[Position]);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(common, new Coord(0, -20), new Coord(0, -12));
            Sym.AddCircle(EdgeType.Black, new Coord(0, -12), 2);

            for (int i = 0; i < Throws.Length; ++i)
            {
                int x = (i - Throws.Length / 2) * 20 + (Throws.Length % 2 == 0 ? 10 : 0);
                Sym.AddTerminal(throws[i], new Coord(x, 20), new Coord(x, 12));
                Sym.DrawEllipse(EdgeType.Black, new Coord(x - 2, 10), new Coord(x + 2, 14));
                //Sym.DrawText(i.ToString(), new Coord(x, 12), Alignment.Near, Alignment.Near);

                if (i == Position)
                    Sym.AddWire(new Coord(0, -12), new Coord(x, 12));
            }

            Sym.DrawText(() => Group, new Coord(-2, -12), Alignment.Far, Alignment.Far);
            Sym.DrawText(() => Name, new Coord(2, -12), Alignment.Near, Alignment.Far);
        }
    }

    [Category("Generic")]
    [DisplayName("SPDT")]
    [DefaultProperty("Position")]
    [Description("single pole double-throw switch.")]
    public class SPDT : SinglePoleSwitch { public SPDT() : base(2) { } }

    [Category("Generic")]
    [DisplayName("SP3T")]
    [DefaultProperty("Position")]
    [Description("single pole triple-throw switch.")]
    public class SP3T : SinglePoleSwitch { public SP3T() : base(3) { } }


    /// <summary>
    /// Switch component that is open or closed. 
    /// Deprecated.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Switch")]
    [DefaultProperty("Closed")]
    [Description("Switch.")]
    [Obsolete]
    public class Switch : TwoTerminal
    {
        protected bool closed = false;
        [Serialize, Description("Switch position.")]
        public bool Closed { get { return closed; } set { closed = value; NotifyChanged(nameof(Closed)); } }

        public Switch() { Name = "S1"; }
        public Switch(bool Closed) : this() { closed = Closed; }

        public override void Analyze(Analysis Mna)
        {
            if (closed)
                Conductor.Analyze(Mna, Name, Anode, Cathode);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 12));
            Sym.AddWire(Cathode, new Coord(0, -12));
            Sym.AddCircle(EdgeType.Black, new Coord(0, 12), 2);
            Sym.AddCircle(EdgeType.Black, new Coord(0, -12), 2);
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            if (closed)
                Sym.AddWire(new Coord(0, -12), new Coord(0, 12));
            else
                Sym.AddWire(new Coord(0, -12), new Coord(-8, 10));

            Sym.DrawText(() => Name, new Coord(2, 0), Alignment.Near, Alignment.Center);
        }
    }
}
