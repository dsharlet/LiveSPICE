using ComputerAlgebra;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Transformer with parasitic series resistance and leakage inductance.
    /// </summary>
    [Category("Generic")]
    [DisplayName("NonIdealTransformer")]
    [DefaultProperty("Turns")]
    [Description("Transformer with parasitic series resistance and leakage inductance.")]
    public class NonIdealTransformer : Component
    {
        private Terminal pa, pc, sa, sc;

        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                yield return pa;
                yield return pc;
                yield return sa;
                yield return sc;
            }
        }

        protected Ratio turns = new Ratio(1, 1);
        [Serialize, Description("Primary:secondary turns ratio.")]
        public Ratio Turns { get { return turns; } set { turns = value; NotifyChanged(nameof(Turns)); } }

        //ADDED: Series resistance and leakage inductance
        private Quantity primaryResistance = new Quantity(0, Units.Ohm);
        [Serialize, Description("Primary series resistance.")]
        public Quantity PrimaryResistance { get { return primaryResistance; } set { primaryResistance = value; NotifyChanged(nameof(PrimaryResistance)); } }

        private Quantity secondaryResistance = new Quantity(0, Units.Ohm);
        [Serialize, Description("Secondary series resistance.")]
        public Quantity SecondaryResistance { get { return secondaryResistance; } set { secondaryResistance = value; NotifyChanged(nameof(SecondaryResistance)); } }

        private Quantity primaryLeakage= new Quantity(0, Units.H);
        [Serialize, Description("Primary leakage inductance.")]
        public Quantity PrimaryLeakage { get { return primaryLeakage; } set { primaryLeakage = value; NotifyChanged(nameof(PrimaryLeakage)); } }

        private Quantity secondaryLeakage = new Quantity(0, Units.H);
        [Serialize, Description("Secondary leakage inductance.")]
        public Quantity SecondaryLeakage { get { return secondaryLeakage; } set { secondaryLeakage = value; NotifyChanged(nameof(SecondaryLeakage)); } }

        private Quantity magnetizingInductance = new Quantity(0, Units.H);
        [Serialize, Description("Magnetizing inductance.")]
        public Quantity MagnetizingInductance { get { return magnetizingInductance; } set { magnetizingInductance = value; NotifyChanged(nameof(MagnetizingInductance)); } }

        private Quantity coreLossResistance = new Quantity(0, Units.Ohm);
        [Serialize, Description("Core loss resistance.")]
        public Quantity CoreLossResistance { get { return coreLossResistance; } set { coreLossResistance = value; NotifyChanged(nameof(CoreLossResistance)); } }

        public NonIdealTransformer()
        {
            pa = new Terminal(this, "PA");
            pc = new Terminal(this, "PC");
            sa = new Terminal(this, "SA");
            sc = new Terminal(this, "SC");
            Name = "TX1";
        }

        public override void Analyze(Analysis Mna)
        {
            Expression Ip = Mna.AddUnknown("i" + Name + "p");
            Mna.AddPassiveComponent(pa, pc, Ip);
            Expression Is = Mna.AddUnknown("i" + Name + "s");
            Mna.AddPassiveComponent(sc, sa, Is);

            //CHANGED: core voltage equation to include series resistance and leakage inductance
            Expression Vp = pa.V - pc.V;
            Expression Vs = sa.V - sc.V;
            Expression VcoreP = Vp - Ip * PrimaryResistance - D(Ip, t) * PrimaryLeakage;
            Expression VcoreS = Vs + Is * SecondaryResistance + D(Is, t) * SecondaryLeakage;
            Mna.AddEquation(VcoreP, turns * VcoreS);

            Expression Iexcitation = 0;
            if (!MagnetizingInductance.Equals(0))
            {
                Expression Im = Mna.AddUnknown("i" + Name + "m");
                Mna.AddEquation(VcoreP, D(Im, t) * MagnetizingInductance);
                Iexcitation += Im;
            }
            if (!CoreLossResistance.Equals(0))
            {
                Expression Ic = VcoreP / CoreLossResistance;
                Iexcitation += Ic;
            }
            Expression Iload = Ip - Iexcitation;
            Mna.AddEquation(Iload * turns, Is);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(pa, new Coord(-10, 20));
            Sym.AddTerminal(pc, new Coord(-10, -20));
            Sym.AddTerminal(sa, new Coord(10, 20));
            Sym.AddTerminal(sc, new Coord(10, -20));

            Sym.AddWire(pa, new Coord(-10, 16));
            Sym.AddWire(pc, new Coord(-10, -16));
            Sym.AddWire(sa, new Coord(10, 16));
            Sym.AddWire(sc, new Coord(10, -16));
            Sym.InBounds(new Coord(-20, 0), new Coord(20, 0));

            Inductor.Draw(Sym, -10, -16.0, 16.0, 4, 4.0);
            Sym.DrawLine(EdgeType.Black, new Coord(-2, 16), new Coord(-2, -16));
            Sym.DrawLine(EdgeType.Black, new Coord(2, 16), new Coord(2, -16));
            Inductor.Draw(Sym, 10, -16.0, 16.0, 4, -4.0);

            Sym.DrawText(() => Name, new Coord(-16, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(() => Turns.ToString(), new Coord(16, 0), Alignment.Near, Alignment.Center);
        }
    }
}
