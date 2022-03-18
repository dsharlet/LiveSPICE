using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace Circuit
{
    /// <summary>
    /// Circuits contain a list of nodes and components.
    /// </summary>
    public class Circuit : Component
    {
        private ComponentCollection components = new ComponentCollection();
        [Browsable(false)]
        public ComponentCollection Components { get { return components; } }

        private NodeCollection nodes = new NodeCollection();
        [Browsable(false)]
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

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            // Get the ports to add to the symbol.
            List<Port> ports = Components.OfType<Port>().OrderBy(i => i.Name).ToList();

            int number = (Math.Max(ports.Max(i => i.Number, 0), ports.Count()) + 1) & ~1;

            int w = 40;
            int h = (number / 2) * 10;

            Sym.DrawText(() => PartNumber, new Coord(0, h + 2), Alignment.Center, Alignment.Near);
            Sym.AddRectangle(EdgeType.Black, new Coord(-w, -h), new Coord(w, h));
            Sym.DrawText(() => Name, new Coord(0, -h - 2), Alignment.Center, Alignment.Far);

            // Draw a notch at the top of the IC. Port slots are numbered counter-clockwise from here.
            int r = 5;
            Sym.DrawFunction(EdgeType.Black, t => t, t => h - Math.Sqrt(r * r - t * t), -r, r, 12);

            // Remember which port slots are open for the unnumbered terminals.
            List<int> open = Enumerable.Range(1, number + 1).ToList();
            foreach (Port i in ports.OrderBy(i => i.Number > 0 ? 0 : 1))
            {
                int n = i.Number > 0 ? i.Number : open.First();

                Terminal t = i.External;
                Coord x;
                if (n <= number / 2)
                    x = new Coord(-w, h - n * 20 + 10);
                else
                    x = new Coord(w, n * 20 - 3 * h - 10);

                Sym.AddTerminal(t, x);
                Sym.DrawText(() => t.Name, new Coord(x.x - Math.Sign(x.x) * 3, x.y), x.x < 0 ? Alignment.Near : Alignment.Far, Alignment.Center);

                open.Remove(n);
            }
        }

        public override void Analyze(Analysis Mna)
        {
            Mna.PushContext(Name, Nodes);
            foreach (Component c in Components)
                c.Analyze(Mna);
            Mna.PopContext();
        }

        public Analysis Analyze()
        {
            Analysis mna = new Analysis();
            mna.PushContext(null, Nodes);
            foreach (Component c in Components)
                c.Analyze(mna);
            mna.PopContext();
            return mna;
        }

        public override XElement Serialize()
        {
            XElement X = base.Serialize();
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

            foreach (XElement i in X.Elements("Component"))
            {
                Component component = Deserialize(i);
                foreach (XElement j in i.Elements("Terminal"))
                    component.Terminals.Single(x => x.Name == j.Attribute("Name").Value).ConnectTo(Nodes[j.Attribute("ConnectedTo").Value]);
                Components.Add(component);
            }
        }
    };
}