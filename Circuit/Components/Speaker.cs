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
    [CategoryAttribute("IO")]
    [DisplayName("Speaker")]
    public class Speaker : PassiveTwoTerminal
    {
        private Quantity impedance = new Quantity(Constant.Infinity, Units.Ohm);

        [SchematicPersistent]
        public Quantity Impedance { get { return impedance; } set { impedance = value; NotifyChanged("Impedance"); } }

        public Speaker() { Name = "S1"; }

        /// <summary>
        /// Get an expression describing the sound of this speaker.
        /// </summary>
        public virtual Expression Sound { get { return V; } }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            i = Resistor.Analyze(Mna, V, Impedance);
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(5, 10),
                new Coord(15, 20),
                new Coord(15, -20),
                new Coord(5, -10),
                new Coord(-10, -10));

            Sym.DrawText(Name, new Coord(15, 0), Alignment.Near, Alignment.Center);
        }

        public override string ToString() { return Name; }
    }
}
