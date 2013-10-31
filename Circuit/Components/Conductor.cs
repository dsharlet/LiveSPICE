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
    /// This component isn't useful. It is present for completeness.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Wire")]
    [Description("Component with zero impedance between the terminals.")]
    public class Conductor : TwoTerminal
    {
        public Conductor() { Name = "_1"; }

        public static Expression Analyze(ModifiedNodalAnalysis Mna, Terminal Anode, Terminal Cathode)
        {
            Expression i = Mna.AddNewUnknown();
            Mna.AddPassiveComponent(Anode, Cathode, i);
            Mna.AddEquation(Anode.V, Cathode.V);
            return i;
        }

        public override void Analyze(ModifiedNodalAnalysis Mna) { Analyze(Mna, Anode, Cathode); }
        
        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);
            Sym.AddWire(Anode, Cathode);
        }
    }
}