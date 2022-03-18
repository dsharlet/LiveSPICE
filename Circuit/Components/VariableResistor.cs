using ComputerAlgebra;
using System;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// What kind of sweep a parameter should make.
    /// </summary>
    public enum SweepType
    {
        Linear,
        Logarithmic,
        // These are equivalent to just swapping around connections on potentiometers,
        // but variable resistors sometimes need them.
        ReverseLinear,
        ReverseLogarithmic,
    }

    static class SweepTypeMethods
    {
        /// <summary>
        /// Return the Asian/American marking letter of the sweep.
        /// </summary>
        public static string GetCode(this SweepType sweep)
        {
            switch (sweep)
            {
                case SweepType.Linear:
                case SweepType.ReverseLinear:
                    return "B";
                case SweepType.Logarithmic:
                    return "A";
                case SweepType.ReverseLogarithmic:
                    return "C";
                default:
                    return "";
            }
        }
    }

    [Category("Generic")]
    [DisplayName("Variable Resistor")]
    [DefaultProperty("Resistance")]
    [Description("Variable resistor.")]
    public class VariableResistor : TwoTerminal, IPotControl
    {
        protected Quantity resistance = new Quantity(100, Units.Ohm);
        [Serialize, Description("Resistance of this variable resistor.")]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged(nameof(Resistance)); } }

        protected double wipe = 0.5;
        [Serialize, Description("Position of the wiper on this variable resistor, between 0 and 1.")]
        public double Wipe { get { return wipe; } set { wipe = value; NotifyChanged(nameof(Wipe)); } }
        // IPotControl
        double IPotControl.PotValue { get { return Wipe; } set { Wipe = value; } }

        protected SweepType sweep = SweepType.Linear;
        [Serialize, Description("Sweep mapping of the wiper.")]
        public SweepType Sweep { get { return sweep; } set { sweep = value; NotifyChanged(nameof(Sweep)); } }

        private string group = "";
        [Serialize, Description("Potentiometer group this potentiometer is a section of.")]
        public string Group { get { return group; } set { group = value; NotifyChanged(nameof(Group)); } }

        public VariableResistor() { Name = "R1"; }

        public override void Analyze(Analysis Mna)
        {
            Expression P = AdjustWipe(wipe, sweep);

            Resistor.Analyze(Mna, Name, Anode, Cathode, (Expression)Resistance * P);
        }

        public static double AdjustWipe(double x, SweepType Sweep)
        {
            x = Math.Min(Math.Max(x, 1e-3), 1.0 - 1e-3);
            
            // If we want the parameter to be backwards, swap it.
            if (Sweep == SweepType.ReverseLinear || Sweep == SweepType.ReverseLogarithmic)
                x = 1 - x;
            
            // If we want the parameter to be logarithmic, apply an exponential curve
            // passing through 0 and 1.
            double exp = Math.Exp(2);
            if (Sweep == SweepType.Logarithmic || Sweep == SweepType.ReverseLogarithmic)
                x = (Math.Pow(exp, x) - 1.0) / (exp - 1.0);

            return x;
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Resistor.Draw(Sym, 0, -16, 16, 7);

            Sym.DrawArrow(EdgeType.Black, new Coord(-6, -15), new Coord(6, 15), 0.1);

            Sym.DrawText(() => Sweep.GetCode()+Resistance.ToString(), new Coord(-7, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(() => Wipe.ToString("G3"), new Coord(9, 3), Alignment.Near, Alignment.Near);
            Sym.DrawText(() => Name, new Coord(9, -3), Alignment.Near, Alignment.Far);
        }
    }
}
