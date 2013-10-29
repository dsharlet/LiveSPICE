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
            // The default circuit layout is a box with the terminals alternating up the sides.
            List<Terminal> terminals = Terminals.ToList();

            int w = 40;
            int h = Math.Max(20, (terminals.Count / 2 + 1) * 10);

            Sym.AddRectangle(EdgeType.Black, new Coord(-w, -h), new Coord(w, h));

            for (int i = 0; i < terminals.Count; ++i)
            {
                int dx = (i % 2) * 2 - 1;
                int x = w * dx;
                int y = h - (i / 2 + 1) * 20;
                Sym.AddTerminal(terminals[i], new Coord(x, y));
                Sym.DrawText(terminals[i].Name, new Coord(x - dx * 3, y), x < 0 ? Alignment.Near : Alignment.Far, Alignment.Center);
            }
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
                c.Analyze(Mna);

            //foreach (Node n in Nodes)
            //{
            //    Mna.AddUnknowns(n.V);
            //    Expression i = n.Kcl();
            //    if (i != null && !i.IsZero())
            //        Mna.AddEquation(i, Constant.Zero);
            //}

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
            // Serialize nodes.
            foreach (Node i in Nodes)
            {
                XElement node = new XElement("Node");
                node.SetAttributeValue("Name", i.Name);
                X.Add(node);
            }
            // Serialize child components.
            foreach (Component i in Components)
            {
                XElement component = i.Serialize();
                // Store connected terminals.
                foreach (Terminal j in i.Terminals.Where(x => x.IsConnected))
                {
                    XElement terminal = new XElement("Terminal");
                    terminal.SetAttributeValue("Name", j.Name);
                    terminal.SetAttributeValue("ConnectedTo", j.ConnectedTo.Name);
                    component.Add(terminal);
                }
                X.Add(component);
            }
            return X;
        }

        protected override void DeserializeImpl(XElement X)
        {
            base.DeserializeImpl(X);

            // Deserialize nodes.
            Nodes.AddRange(X.Elements("Node").Select(i => new Node() { Name = i.Attribute("Name").Value }));

            foreach (XElement i in X.Elements("Component"))
            {
                Component component = Deserialize(i);
                foreach (XElement j in i.Elements("Terminal"))
                    component.Terminals.Single(x => x.Name == j.Attribute("Name").Value).ConnectTo(Nodes[j.Attribute("ConnectedTo").Value]);
                Components.Add(component);
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