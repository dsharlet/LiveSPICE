using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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
                return Components.OfType<Port>().Select(i => i.External);
            }
        }
        
        public override void LayoutSymbol(SymbolLayout Sym)
        {
            throw new NotImplementedException();
        }

        private static readonly Expression MatchName = Variable.New("name");
        private static readonly Expression MatchV = DependentVariable("V", MatchName);
        /// <summary>
        /// Evaluates x for the components in this circuit.
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public Expression Evaluate(Expression x)
        {
            MatchContext m = MatchV.Matches(x);
            if (m != null && m[MatchName] != Component.t)
            {
                string name = m[MatchName].ToString();
                Component of = Components.Single(i => i.Name == name);
                if (of is TwoTerminal)
                    return ((TwoTerminal)of).V;
                else if (of is OneTerminal)
                    return ((OneTerminal)of).V;
            }
            return x;
        }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            foreach (Component c in Components)
            {
                c.Analyze(Mna);
                // Make a fake node for the unconnected terminals.
                foreach (Terminal t in c.Terminals.Where(i => i.ConnectedTo == null))
                {
                    Mna.AddUnknowns(t.V);
                    Expression i = t.i;
                    if (i != null && !i.IsZero())
                        Mna.AddEquation(i, Constant.Zero);
                }
            }
            foreach (Node n in Nodes)
            {
                Mna.AddUnknowns(n.V);
                Expression i = n.Kcl();
                if (i != null && !i.IsZero())
                    Mna.AddEquation(i, Constant.Zero);
            }

            // Add equations for any depenent expressions.
            foreach (MatchContext v in Mna.Equations.SelectMany(i => i.FindMatches(MatchV)).Distinct().ToArray())
            {
                if (v[MatchName] != Component.t)
                {
                    string name = v[MatchName].ToString();
                    Mna.AddEquation(v.Matched, Evaluate(v.Matched));
                    Mna.AddUnknowns(v.Matched);
                }
            }
        }

        public ModifiedNodalAnalysis Analyze()
        {
            ModifiedNodalAnalysis mna = new ModifiedNodalAnalysis();
            Analyze(mna);
            return mna;
        }

        public override XElement Serialize()
        {
            XElement X = base.Serialize();
            return X;
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