using System;
using System.ComponentModel;
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
        [Browsable(false)]
        public ComponentCollection Components { get { return components; } }

        private NodeCollection nodes = new NodeCollection();
        [Browsable(false)]
        public NodeCollection Nodes { get { return nodes; } }

        private string displayName = "";
        [Serialize, DefaultValue("")]
        public string DisplayName { get { return displayName; } set { displayName = value; NotifyChanged("DisplayName"); } }

        private string description = "";
        [Serialize, DefaultValue("")]
        public string Description { get { return description; } set { description = value; NotifyChanged("Description"); } }

        private string category = "";
        [Serialize, DefaultValue("")]
        public string Category { get { return category; } set { category = value; NotifyChanged("Category"); } }

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
            List<Port> ports = Components.OfType<Port>().ToList();

            int w = 40;
            int h = Math.Max(
                Math.Max(ports.OfType<InputPort>().Max(i => i.Number), ports.OfType<InputPort>().Count()),
                Math.Max(ports.OfType<OutputPort>().Max(i => i.Number), ports.OfType<OutputPort>().Count())) * 10;

            Sym.DrawText(() => PartNumber, new Coord(0, h + 2), Alignment.Center, Alignment.Near);
            Sym.AddRectangle(EdgeType.Black, new Coord(-w, -h), new Coord(w, h));
            Sym.DrawText(() => Name, new Coord(0, -h - 2), Alignment.Center, Alignment.Far);

            int number = 0;
            foreach (Port i in ports.OfType<InputPort>())
            {
                Terminal t = i.External;
                int y = h - (i.Number != -1 ? i.Number : number++) * 20 - 10;
                Sym.AddTerminal(t, new Coord(-w, y));
                Sym.DrawText(() => t.Name, new Coord(-w + 3, y), Alignment.Near, Alignment.Center);
            }
            number = 0;
            foreach (Port i in ports.OfType<OutputPort>())
            {
                Terminal t = i.External;
                int y = h - (i.Number != -1 ? i.Number : number++) * 20 - 10;
                Sym.AddTerminal(t, new Coord(w, y));
                Sym.DrawText(() => t.Name, new Coord(w - 3, y), Alignment.Far, Alignment.Center);
            }
        }

        public override void Analyze(Analysis Mna)
        {
            Mna.PushContext(this);
            foreach (Component c in Components)
                c.Analyze(Mna);
            Mna.PopContext();
        }

        public Analysis Analyze()
        {
            Analysis mna = new Analysis();
            Analyze(mna);
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

        public override string GetDisplayName() { return DisplayName != "" ? DisplayName : PartNumber; }
        public override string GetDescription() { return Description; }
        public override string GetCategory() { return Category; }

        /// <summary>
        /// Create a circuit from a SPICE netlist.
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
        public static Circuit FromNetlist(string Filename)
        {
            return Netlist.Parse(Filename);
        }
    };
}