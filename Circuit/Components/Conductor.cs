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
    public class Conductor : TwoTerminal
    {
        public Conductor() { Name = "_1"; }

        public override void Analyze(ICollection<Equal> Mna, ICollection<Expression> Unknowns) 
        {
            Expression i = DependentVariable("i" + Name, t);
            Anode.i = i;
            Cathode.i = -i;
            Unknowns.Add(i);

            Mna.Add(Equal.New(Anode.V, Cathode.V));
        }

        protected override void DrawSymbol(SymbolLayout Sym) { Sym.AddWire(Anode, Cathode); }
    }
}