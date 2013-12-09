using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerAlgebra;

namespace Circuit
{
    /// <summary>
    /// This component isn't useful. It is present for completeness.
    /// </summary>
    [Category("Standard")]
    [DisplayName("Wire")]
    [Description("Component with zero impedance between the terminals.")]
    public class Conductor : TwoTerminal
    {
        public Conductor() { Name = "_1"; }

        public static Expression Analyze(Analysis Mna, string Name, Node Anode, Node Cathode)
        {
            Expression i = Mna.AddUnknown("i" + Name);
            Mna.AddPassiveComponent(Anode, Cathode, i);
            Mna.AddEquation(Anode.V, Cathode.V);
            return i;
        }
        public static Expression Analyze(Analysis Mna, Node Anode, Node Cathode) { return Analyze(Mna, Mna.AnonymousName(), Anode, Cathode); }

        public override void Analyze(Analysis Mna) { Analyze(Mna, Name, Anode, Cathode); }
        
        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);
            Sym.AddWire(Anode, Cathode);
        }
    }
}