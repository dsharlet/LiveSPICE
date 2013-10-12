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

        public IEnumerable<Symbol> Symbols { get { return elements.OfType<Symbol>(); } }
        public IEnumerable<Wire> Wires { get { return elements.OfType<Wire>(); } }

        protected int width;
        protected int height;
        /// <summary>
        /// Get the dimensions of the schematic.
        /// </summary>
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        protected ILog log = new ConsoleLog();
        /// <summary>
        /// Get or set the log for messages associated with this schematic.
        /// </summary>
        public ILog Log { get { return log; } set { log = value; } }

        public Schematic(int Width, int Height, ILog Log)
        {
            log = Log;
            width = Width;
            height = Height;

            elements = new ElementCollection();
            elements.ItemAdded += OnElementAdded;
            elements.ItemRemoved += OnElementRemoved;
        }

        public Schematic(ILog Log) : this(1600, 1600, Log) 
        {
        }

        public Circuit Build() { return Circuit; }

        /// <summary>
        /// Get the terminals located at x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public IEnumerable<Terminal> TerminalsAt(Coord x)
        {
            return elements.SelectMany(i => i.Terminals.Where(j => i.MapTerminal(j) == x));
        }

        public Node NodeAt(Coord x)
        {
            Wire w = Wires.FirstOrDefault(i => i.IsConnectedTo(x));
            return w != null ? w.Node : null;
        }

        protected void OnElementAdded(object sender, ElementEventArgs e)
        {
            if (e.Element is Symbol)
                Circuit.Components.Add(((Symbol)e.Element).Component);
            OnLayoutChanged(e.Element, null);

            e.Element.LayoutChanged += OnLayoutChanged;

            Log.WriteLine(MessageType.Verbose, "Added '" + e.Element.ToString() + "'");
        }

        protected void OnElementRemoved(object sender, ElementEventArgs e)
        {
            e.Element.LayoutChanged -= OnLayoutChanged;

            // If the removed element is a wire, we might have to split the node it was a part of.
            if (e.Element is Symbol)
                Circuit.Components.Remove(((Symbol)e.Element).Component);
            if (e.Element is Wire)
                RebuildNodes();

            foreach (Terminal i in e.Element.Terminals)
            {
                Node n = i.ConnectedTo;
                i.ConnectTo(null);

                // If the node that this terminal was connected to has no more connections, remove it from the circuit.
                if (n != null && n.Connected.Empty())
                {
                    Log.WriteLine(MessageType.Verbose, "Removed node '" + n.ToString() + "'");
                    circuit.Nodes.Remove(n);
                }
            }

            Log.WriteLine(MessageType.Verbose, "Removed '" + e.Element.ToString() + "'");
        }

        // When an element moves, we will need to update its connections.
        protected void OnLayoutChanged(object sender, EventArgs e)
        {
            Element of = (Element)sender;

            if (of is Wire)
                RebuildNodes();
            else if (of is Symbol)
                UpdateTerminals((Symbol)of);
        }

        // Wires was a single node but it is now two. Break it into two sets of nodes
        // and return the one containing Target.
        private IEnumerable<Wire> ConnectedTo(IEnumerable<Wire> Wires, Wire Target)
        {
            // Repeatedly search for connections with the target.
            IEnumerable<Wire> connected = new Wire[] { Target };
            int count = connected.Count();
            while (true)
            {
                connected = Wires.Where(i => connected.Any(j => i.IsConnectedTo(j))).ToArray();
                if (connected.Count() == count)
                    break;
                count = connected.Count();
            }

            return connected;
        }

        // Merge all of the nodes contained in wires to one.
        private Node MergeNode(IEnumerable<Wire> Wires)
        {
            // All the existing nodes contained in wires.
            List<Node> nodes = Wires.Select(i => i.Node).Distinct().ToList();

            // If this set of wires is connected to a NamedWire, use that as the node.
            Node n = null;
            foreach (NamedWire i in Symbols.Select(j => j.Component).OfType<NamedWire>())
            {
                if (Wires.Any(j => j.IsConnectedTo(((Symbol)i.Tag).MapTerminal(i.Terminal))))
                {
                    if (n != null)
                        Log.WriteLine(MessageType.Warning, "Multiple Named Wires connected to node.");
                    n = circuit.Nodes[i.WireName];
                }
            }

            // Only make one node for ground. This isn't strictly necessary, but it results in less log spew.
            foreach (Ground i in Symbols.Select(j => j.Component).OfType<Ground>())
            {
                if (Wires.Any(j => j.IsConnectedTo(((Symbol)i.Tag).MapTerminal(i.Terminal))))
                    n = circuit.Nodes["v0"];
            }

            // If there are no NamedWires, use one of the nodes already in the set.
            if (n == null)
                n = nodes.FirstOrDefault(i => i != null);

            // If there were no nodes in the set, just make a new one.
            if (n == null)
            {
                n = new Node();
                circuit.Nodes.Add(n);
                Log.WriteLine(MessageType.Verbose, "Created new node '" + n.ToString() + "'");
            }

            foreach (Wire i in Wires)
                i.Node = n;

            // Everything connected to a node in nodes should now be connected to n.
            foreach (Node i in nodes.Where(j => j != null && j != n))
            {
                foreach (Terminal j in i.Connected.ToArray())
                    j.ConnectTo(n);
                circuit.Nodes.Remove(i);
                Log.WriteLine(MessageType.Verbose, "Removed node '" + i.ToString() + "'");
            }
            return n;
        }

        private void RebuildNodes()
        {
            IEnumerable<Wire> wires = Wires;

            while (!wires.Empty())
            {
                // Find all the wires connected to the first wire in the list.
                IEnumerable<Wire> connected = ConnectedTo(wires, wires.First());

                // Merge all the connected wires into one node.
                Node node = MergeNode(connected);

                // Remove the wires we just made into a node from the list.
                wires = wires.Except(connected);

                // Any reminaing wires cannot be connected to the new node.
                foreach (Wire i in wires.Where(j => j.Node == node))
                    i.Node = null;
            }

            foreach (Symbol i in Symbols)
                UpdateTerminals(i);
        }

        private void UpdateTerminals(Symbol Of)
        {
            foreach (Terminal i in Of.Terminals)
            {
                Node n = NodeAt(Of.MapTerminal(i));
                if (n != i.ConnectedTo)
                {
                    i.ConnectTo(n);
                    if (n != null)
                        Log.WriteLine(MessageType.Verbose, "Terminal '" + i.ToString() + "' connected to node '" + n.ToString() + "'");
                    else
                        Log.WriteLine(MessageType.Verbose, "Terminal '" + i.ToString() + "' disconnected");
                }
            }

            // If Of is a named wire, the nodes might have changed.
            if (Of.Component is NamedWire)
                RebuildNodes();
        }

        private void LogComponents()
        {
            foreach (Element i in Symbols)
                Log.WriteLine(MessageType.Info, "  " + i.ToString());
            Log.WriteLine(MessageType.Info, "  (" + Wires.Count() + " wires)");
        }

        public void Save(string FileName)
        {
            XDocument doc = new XDocument();
            doc.Add(Serialize());
            doc.Save(FileName);
            Log.WriteLine(MessageType.Info, "Schematic saved to '" + FileName + "'");
            LogComponents();
        }

        public static Schematic Load(string FileName, ILog Log)
        {
            XDocument doc = XDocument.Load(FileName);
            Schematic S = Schematic.Deserialize(doc.Root, Log);
            Log.WriteLine(MessageType.Info, "Schematic loaded from '" + FileName + "'");
            S.LogComponents();
            return S;
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

        public static Schematic Deserialize(XElement X, ILog Log)
        {
            int width, height;
            try { width = int.Parse(X.Attribute("Width").Value); }
            catch (Exception) { width = 1600;  }
            try { height = int.Parse(X.Attribute("Height").Value); }
            catch (Exception) { height = 1600;  }

            Schematic s = new Schematic(width, height, Log);
            s.Elements.AddRange(X.Elements("Element").Select(i => Element.Deserialize(i)));

            return s;
        }
    }
}
