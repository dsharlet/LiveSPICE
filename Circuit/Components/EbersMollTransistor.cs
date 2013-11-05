using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    public enum BJTStructure
    {
        NPN,
        PNP,
    };

    /// <summary>
    /// Implementation of the Ebers-Moll transistor model: http://people.seas.harvard.edu/~jones/es154/lectures/lecture_3/bjt_models/ebers_moll/ebers_moll.html
    /// </summary>
    [Category("Transistors")]
    [DisplayName("BJT (Ebers-Moll)")]
    [Description("Bipolar junction transistor implemented using the Ebers-Moll model.")]
    public class EbersMollBJT : Transistor
    {
        private BJTStructure type = BJTStructure.NPN;
        [Description("Structure of this of BJT.")]
        [Serialize]
        public BJTStructure Structure { get { return type; } set { type = value; NotifyChanged("Structure"); } }

        protected Quantity _is = new Quantity(1e-12, Units.A);
        [Description("Saturation current.")]
        [Serialize]
        public Quantity IS { get { return _is; } set { if (_is.Set(value)) NotifyChanged("IS"); } }

        private double bf = 100;
        [Description("Forward common emitter current gain.")]
        [Serialize]
        public double BF { get { return bf; } set { bf = value; NotifyChanged("BF"); } }

        private double br = 1;
        [Description("Reverse common emitter current gain.")]
        [Serialize]
        public double BR { get { return br; } set { br = value; NotifyChanged("BR"); } }

        public EbersMollBJT() { }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            Expression Vbc = Mna.AddNewUnknownEqualTo(Name + "bc", Base.V - Collector.V);
            Expression Vbe = Mna.AddNewUnknownEqualTo(Name + "be", Base.V - Emitter.V);

            double aR = BR / (1 + BR);
            double aF = BF / (1 + BF);

            Expression iBE = (Expression)IS * (Call.Exp(Vbe / VT) - 1);
            Expression iBC = (Expression)IS * (Call.Exp(Vbc / VT) - 1);

            Expression ie = iBE - aR * iBC;
            Expression ic = -iBC + aF * iBE;
            //Expression ib = (1 - aF) * iBE + (1 - aR) * iBC;

            int sign;
            switch (Structure)
            {
                case BJTStructure.NPN: sign = 1; break;
                case BJTStructure.PNP: sign = -1; break;
                default: 
                    throw new NotSupportedException("Unknown BJT structure.");
            }

            ic = Mna.AddNewUnknownEqualTo("i" + Name + "c", sign * ic);
            ie = Mna.AddNewUnknownEqualTo("i" + Name + "e", -sign * ie);
            Mna.AddTerminal(Collector, ic);
            Mna.AddTerminal(Base, -(ic + ie));
            Mna.AddTerminal(Emitter, ie);
        }

        public static void LayoutSymbol(SymbolLayout Sym, BJTStructure Structure, Terminal C, Terminal B, Terminal E, Func<string> Name, Func<string> Part)
        {
            int bx = -5;
            Sym.AddTerminal(B, new Coord(-20, 0), new Coord(bx, 0));
            switch (Structure)
            {
                case BJTStructure.NPN:
                    Sym.AddTerminal(C, new Coord(10, 20), new Coord(10, 17));
                    Sym.AddTerminal(E, new Coord(10, -20), new Coord(10, -17));
                    Sym.DrawLine(EdgeType.Black, new Coord(10, 17), new Coord(bx, 8));
                    Sym.DrawArrow(EdgeType.Black, new Coord(bx, -8), new Coord(10, -17), 0.2, 0.3);
                    break;
                case BJTStructure.PNP:
                    Sym.AddTerminal(E, new Coord(10, 20), new Coord(10, 17));
                    Sym.AddTerminal(C, new Coord(10, -20), new Coord(10, -17));
                    Sym.DrawArrow(EdgeType.Black, new Coord(10, 17), new Coord(bx, 8), 0.2, 0.3);
                    Sym.DrawLine(EdgeType.Black, new Coord(bx, -8), new Coord(10, -17));
                    break;
                default: 
                    throw new NotSupportedException("Unknown BJT structure.");
            }
            Sym.DrawLine(EdgeType.Black, new Coord(bx, 12), new Coord(bx, -12));

            if (Part != null)
                Sym.DrawText(Part, new Coord(8, 20), Alignment.Far, Alignment.Near);
            Sym.DrawText(Name, new Point(8, -20), Alignment.Far, Alignment.Far);

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);
        }

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Structure, Collector, Base, Emitter, () => Name, () => PartName); }

        public static IEnumerable<Component> Parts = new Component[]
        {
            new ModelSpecialization(new EbersMollBJT() { PartName = "NPN", Structure = BJTStructure.NPN, IS = 1e-12m, BF = 100, BR = 1 }) { DisplayName = "NPN", Description = "Generic NPN BJT." },
            new ModelSpecialization(new EbersMollBJT() { PartName = "PNP", Structure = BJTStructure.PNP, IS = 1e-12m, BF = 100, BR = 1 }) { DisplayName = "PNP", Description = "Generic PNP BJT." },

            new ModelSpecialization(new EbersMollBJT() { PartName = "2SC2240", Structure = BJTStructure.NPN, IS = 1e-12m, BF = 200, BR = 0.1 }) { DisplayName = "2SC2240" },
        };
    }
}
