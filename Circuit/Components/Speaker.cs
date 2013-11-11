using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Ideal speakers just model input impedance.
    /// </summary>
    [Category("Standard")]
    [DisplayName("Speaker")]
    [DefaultProperty("Impedance")]
    [Description("Ideal speaker.")] 
    public class Speaker : TwoTerminal
    {
        private Quantity impedance = new Quantity(Real.Infinity, Units.Ohm);
        [Serialize]
        [Description("Impedance of this speaker.")]
        public Quantity Impedance { get { return impedance; } set { impedance = value; NotifyChanged("Impedance"); } }

        public Speaker() { Name = "S1"; }

        public override void Analyze(Analysis Mna)
        {
            Resistor.Analyze(Mna, Name, Anode, Cathode, Impedance);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(5, 10),
                new Coord(15, 20),
                new Coord(15, -20),
                new Coord(5, -10),
                new Coord(-10, -10));

            Sym.DrawText(() => Name, new Coord(17, 0), Alignment.Near, Alignment.Center);
        }

        public override string ToString() { return Name; }
    }
}
