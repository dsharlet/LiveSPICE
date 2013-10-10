using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SyMath;
using System.Diagnostics;

namespace Circuit
{
    /// <summary>
    /// Represents a visual layout of a circuit.
    /// </summary>
    public class Schematic
    {
        protected Circuit circuit = new Circuit();
        /// <summary>
        /// Get the circuit this schematic represents.
        /// </summary>
        public Circuit Circuit { get { return circuit; } }

        protected ElementCollection elements;
        /// <summary>
        /// Enumeration of the symbols in this schematic.
        /// </summary>
        public ElementCollection Elements { get { return elements; } }

        protected int width = 1600;
        protected int height = 1600;
        /// <summary>
        /// Get the dimensions of the schematic.
        /// </summary>
        public int Width { get { return width; } }
        public int Height { get { return height; } }
        
        public Schematic()
        {
            elements = new ElementCollection();
            elements.ItemAdded += OnElementAdded;
            elements.ItemRemoved += OnElementRemoved;
        }

        /// <summary>
        /// Get the terminals located at x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public IEnumerable<Terminal> TerminalsAt(Coord x)
        {
            return elements
                .Select(i => i.Terminals.SingleOrDefault(j => i.MapTerminal(j) == x))
                .Where(i => i != null);
        }

        /// <summary>
        /// Build a node at the coordinate x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        //public Node BuildNodeAt(Coord x)
        //{
        //    // Find all the terminals at x.
        //    IEnumerable<Terminal> terminals = elements.SelectMany(i => i.Terminals.Where(j => i.MapTerminal(j) == x)).ToList();
        //    // If there aren't two terminals to connect here, we don't need a node.
        //    if (terminals.Count() <= 1)
        //    {
        //        foreach (Terminal i in terminals)
        //            i.ConnectTo(null);
        //        return null;
        //    }

        //    // Find all the nodes at the terminals at x.
        //    IEnumerable<Node> connected = terminals.Select(i => i.ConnectedTo).Distinct();
        //    // Find or make a non-null node.
        //    Node n = connected.FirstOrDefault(i => i != null);
        //    if (n == null)
        //        n = new Node();

        //    // Connect all the terminals at x to n.
        //    foreach (Terminal i in terminals)
        //        i.ConnectTo(n);

        //    return n;
        //}

        protected void RebuildNodes()
        {
            foreach (Element i in Elements)
                foreach (Terminal j in i.Terminals)
                    j.ConnectTo(null);

            // Build a list of wires that are connected to other wires in the circuit.
            List<List<Wire>> nodes = new List<List<Wire>>();
            foreach (Wire i in Elements.OfType<Wire>())
            {
                List<Wire> n = nodes.FirstOrDefault(j => j.Any(k => i.A == k.A || i.A == k.B || i.B == k.A || i.B == k.B));
                if (n != null)
                    n.Add(i);
                else
                    nodes.Add(new List<Wire> { i });
            }

            Circuit.Nodes.Clear();

            foreach (List<Wire> i in nodes)
            {
                Node n = new Node();
                Circuit.Nodes.Add(n);

                foreach (Coord j in i.SelectMany(k => new Coord[] { k.A, k.B }))
                    foreach (Terminal k in TerminalsAt(j))
                        if (!(k.Owner is Conductor))
                            k.ConnectTo(n);
            }
        }

        protected void OnElementAdded(object sender, ElementEventArgs e)
        {
            if (e.Element is Symbol)
                Circuit.Components.Add(((Symbol)e.Element).Component);
            ConnectTerminals(e.Element);

            e.Element.LayoutChanged += OnLayoutChanged;
        }

        protected void OnElementRemoved(object sender, ElementEventArgs e)
        {
            e.Element.LayoutChanged -= OnLayoutChanged;

            foreach (Terminal i in e.Element.Terminals)
                i.ConnectTo(null);
            if (e.Element is Symbol)
                Circuit.Components.Remove(((Symbol)e.Element).Component);
            RemoveOrphanNodes();
        }

        // Find and remove orphan nodes (not connected to anything) in the circuit.
        protected void RemoveOrphanNodes()
        {
            foreach (Node i in circuit.Nodes.Where(i => i.Connected.Count() <= 1).ToArray())
            {
                foreach (Terminal j in i.Connected)
                    j.ConnectTo(null);
                circuit.Nodes.Remove(i);
            }
        }

        // When an element moves, we will need to update its connections.
        protected void OnLayoutChanged(object sender, EventArgs e)
        {
            ConnectTerminals((Element)sender);
        }

        protected void ConnectTerminals(Element e)
        {
            RebuildNodes();
        }

        public XElement Serialize()
        {
            XElement x = new XElement("Schematic");

            foreach (Element i in Elements)
                x.Add(i.Serialize());

            x.SetAttributeValue("Width", Width);
            x.SetAttributeValue("Height", Height);

            return x;
        }

        public static Schematic Deserialize(XElement X)
        {
            Schematic s = new Schematic();

            try { s.width = int.Parse(X.Attribute("Width").Value); }
            catch (Exception) { }
            try { s.height = int.Parse(X.Attribute("Height").Value); }
            catch (Exception) { }

            s.Elements.AddRange(X.Elements("Element").Select(i => Element.Deserialize(i)));

            return s;
        }
    }
}
