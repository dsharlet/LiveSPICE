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
    public class Wire : TwoTerminal
    {
        public Wire() { Name = "W1"; }

        public override void Analyze(IList<Equal> Kcl, IList<SyMath.Expression> Unknowns) 
        {
            Expression i = Call.New(ExprFunction.New("i" + Name, t), t);
            Unknowns.Add(i);

            Kcl.Add(Equal.New(Anode.V, Cathode.V));
            Anode.i = i;
            Cathode.i = -i;
        }

        protected override void DrawSymbol(SymbolLayout Sym) { Sym.AddWire(Anode, Cathode); }
    }
}