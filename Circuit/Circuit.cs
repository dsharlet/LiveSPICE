using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{        
    /// <summary>
    /// Circuits contain a list of nodes and components.
    /// </summary>
    public class Circuit : Component
    {
        private ComponentCollection components = new ComponentCollection();
        public ComponentCollection Components { get { return components; } }

        private NodeCollection nodes = new NodeCollection();
        public NodeCollection Nodes { get { return nodes; } }
        
        /// <summary>
        /// External terminals (ports) in this circuit.
        /// </summary>
        public override IEnumerable<Terminal> Terminals 
        {
            get 
            { 
                return Components.OfType<Port>().Select(i => i.Terminal);
            }
        }
        
        public override void LayoutSymbol(SymbolLayout Sym)
        {
            throw new NotImplementedException();
        }
        
        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            foreach (Component c in Components)
            {
                c.Analyze(Mna, Unknowns);
                // Make a fake node for the unconnected terminals.
                foreach (Terminal t in c.Terminals.Where(i => i.ConnectedTo == null))
                {
                    Unknowns.Add(t.V);
                    Expression i = t.i;
                    if (i != null && !i.IsZero())
                        Mna.Add(Equal.New(i, Constant.Zero));
                }
            }
            foreach (Node n in Nodes)
            {
                Unknowns.Add(n.V);
                Expression i = n.Kcl();
                if (i != null && !i.IsZero())
                    Mna.Add(Equal.New(i, Constant.Zero));
            }
        }
        
        /// <summary>
        /// Create a circuit from a SPICE netlist.
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
        public static Circuit FromNetlist(string Filename)
        {
            return Netlist.Parse(Filename);
        }

        // Logging helpers.
        private static void LogExpressions(ILog Log, string Title, IEnumerable<Expression> Expressions)
        {
            if (Expressions.Any())
            {
                Log.WriteLine(MessageType.Info, Title);
                foreach (Expression i in Expressions)
                    Log.WriteLine(MessageType.Info, "  " + i.ToString());
                Log.WriteLine(MessageType.Info, "");
            }
        }
    };
}