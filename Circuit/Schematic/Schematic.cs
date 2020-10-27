using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Util;

namespace Circuit
{
    /// <summary>
    /// Represents a visual layout of a circuit.
    /// </summary>
    public class Schematic
    {
        protected Circuit circuit = new Circuit();
        /// <summary>
        /// Get the circuit object for this schematic. Use Build to ensure the circuit object is up to date.
        /// </summary>
        public Circuit Circuit { get { return circuit; } }

        protected ElementCollection elements;
        /// <summary>
        /// Enumeration of the symbols in this schematic.
        /// </summary>
        public ElementCollection Elements { get { return elements; } }

        public void Add(IEnumerable<Element> Elements) { elements.AddRange(Elements); }
        public void Add(params Element[] Elements) { elements.AddRange(Elements); }
        public void Remove(IEnumerable<Element> Elements) { elements.RemoveRange(Elements); }
        public void Remove(params Element[] Elements) { elements.RemoveRange(Elements); }

        public IEnumerable<Symbol> Symbols { get { return elements.OfType<Symbol>(); } }
        public IEnumerable<Wire> Wires { get { return elements.OfType<Wire>(); } }

        public Coord LowerBound { get { return elements.Any() ? new Coord(elements.Min(i => i.LowerBound.x), elements.Min(i => i.LowerBound.y)) : new Coord(0, 0); } }
        public Coord UpperBound { get { return elements.Any() ? new Coord(elements.Max(i => i.UpperBound.x), elements.Max(i => i.UpperBound.y)) : new Coord(0, 0); } }
        public Coord Size { get { return UpperBound - LowerBound; } }

        protected ILog log = new NullLog();
        /// <summary>
        /// Get or set the log for messages associated with this schematic.
        /// </summary>
        public ILog Log { get { return log; } set { log = value; } }

        public Schematic(ILog Log) : this() { log = Log; }
        public Schematic()
        {
            elements = new ElementCollection();
            elements.ItemAdded += OnElementAdded;
            elements.ItemRemoved += OnElementRemoved;
        }

        /// <summary>
        /// Build the Schematic into a Circuit object.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public Circuit Build(ILog log)
        {
            int errors = 0;
            int warnings = 0;

            log.WriteLine(MessageType.Info, "Building circuit...");

            // Check for duplicate names.
            foreach (string i in circuit.Components.Select(i => i.Name))
            {
                IEnumerable<Component> named = circuit.Components.Where(j => j.Name == i);
                if (named.Count() > 1)
                {
                    log.WriteLine(MessageType.Error, "Error: Name '{0}' is not unique", i);
                    foreach (Component j in named)
                        log.WriteLine(MessageType.Error, "  " + j.ToString());
                    errors++;
                }
            }

            // Check for unconnected terminals.
            foreach (Component i in circuit.Components)
            {
                foreach (Terminal j in i.Terminals.Where(j => j.ConnectedTo == null))
                {
                    Node dummy = new Node() { Name = "_x1" };
                    circuit.Nodes.Add(dummy);
                    j.ConnectedTo = dummy;

                    log.WriteLine(MessageType.Warning, "Warning: Unconnected terminal '{0}'", j.ToString());
                    warnings++;
                }
            }

            // Check for error symbols.
            foreach (Error i in circuit.Components.OfType<Error>())
            {
                log.WriteLine(MessageType.Error, "Error: {0}", i.Message);
                errors++;
            }

            // Check for warning symbols.
            foreach (Warning i in circuit.Components.OfType<Warning>())
            {
                log.WriteLine(MessageType.Warning, "Warning: {0}", i.Message);
                warnings++;
            }

            // Check for deprecated symbols.
            foreach (Component i in circuit.Components.Where(i => i.GetType().CustomAttribute<ObsoleteAttribute>() != null))
            {
                log.WriteLine(MessageType.Warning, "Warning: Use of deprecated symbol '{0}'", i.ToString());
                warnings++;
            }

            log.WriteLine(MessageType.Info, "Build: {0} errors, {1} warnings", errors, warnings);

            if (errors != 0)
                throw new InvalidOperationException("Build failed");
            else
                return circuit;
        }

        public Circuit Build() { return Build(log); }

        /// <summary>
        /// Get the terminals located at x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public IEnumerable<Terminal> TerminalsAt(Coord x)
        {
            return elements.SelectMany(i => i.Terminals.Where(j => i.MapTerminal(j) == x));
        }

        /// <summary>
        /// Find the node at point x.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="Self"></param>
        /// <returns></returns>
        public Node NodeAt(Coord x, Wire Self)
        {
            IEnumerable<Wire> wires = Wires;
            if (Self != null)
                wires = wires.Except(Self);

            Wire w = wires.FirstOrDefault(i => i.IsConnectedTo(x));
            return w?.Node;
        }
        public Node NodeAt(Coord x) { return NodeAt(x, null); }

        protected void OnElementAdded(object sender, ElementEventArgs e)
        {
            if (e.Element is Symbol symbol)
            {
                circuit.Components.Add(symbol.Component);
                if (symbol.Component is NamedWire)
                    RebuildNodes(null, true);
            }
            OnLayoutChanged(e.Element, null);

            e.Element.LayoutChanged += OnLayoutChanged;
        }

        protected void OnElementRemoved(object sender, ElementEventArgs e)
        {
            e.Element.LayoutChanged -= OnLayoutChanged;

            if (e.Element is Symbol symbol)
            {
                Component component = symbol.Component;
                circuit.Components.Remove(component);
                if (component is NamedWire wire)
                    RebuildNodes(wire.ConnectedTo, true);
            }
            else if (e.Element is Wire wire)
            {
                // If the removed element is a wire, we might have to split the node it was a part of.
                RebuildNodes(wire.Node, true);
            }

            foreach (Terminal j in e.Element.Terminals)
            {
                Node n = j.ConnectedTo;
                j.ConnectTo(null);

                // If the node that this terminal was connected to has no more connections, remove it from the circuit.
                if (n != null && n.Connected.Empty())
                    circuit.Nodes.Remove(n);
            }
        }

        // When an element moves, we will need to update its connections.
        protected void OnLayoutChanged(object sender, EventArgs e)
        {
            Element of = (Element)sender;
            if (of is Wire wire)
                RebuildNodes(wire.Node, true);
            UpdateTerminals(of);
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

            Node n = null;

            // If this set of wires is connected to a NamedWire, use that as the node.
            if (n == null)
            {
                foreach (NamedWire i in Symbols.Select(j => j.Component).OfType<NamedWire>())
                {
                    if (Wires.Any(j => j.IsConnectedTo(((Symbol)i.Tag).MapTerminal(i.Terminal))))
                        n = circuit.Nodes[i.WireName];
                }
            }

            // If there are no NamedWires, use one of the nodes already in the set.
            if (n == null)
                n = nodes.FirstOrDefault(i => i != null);

            // If there were no nodes in the set, just make a new one.
            if (n == null)
            {
                n = new Node();
                circuit.Nodes.Add(n);
            }

            foreach (Wire i in Wires)
            {
                i.Node = n;
                UpdateTerminals(i);
            }

            // Everything connected to a node in nodes should now be connected to n.
            foreach (Node i in nodes.Where(j => j != null && j != n))
            {
                foreach (Terminal j in i.Connected.ToArray())
                    j.ConnectTo(n);
                circuit.Nodes.Remove(i);
            }
            return n;
        }

        private void RebuildNodes(Node At, bool MovedWire)
        {
            // If At is not null, only rebuild the wires that are part of that node.
            IEnumerable<Wire> wires = At != null ? Wires.Where(i => i.Node == At).Buffer() : Wires;
            while (!wires.Empty())
            {
                // Find all the wires connected to the first wire in the list.
                IEnumerable<Wire> connected = ConnectedTo(wires, wires.First()).Buffer();

                // Merge all the connected wires into one node.
                Node node = MergeNode(connected);

                // Remove the wires we just made into a node from the list.
                wires = wires.Except(connected).Buffer();

                // Any reminaing wires cannot be connected to the new node.
                foreach (Wire i in wires.Where(j => j.Node == node))
                    i.Node = null;
            }

            if (MovedWire)
                foreach (Element i in Elements)
                    UpdateTerminals(i);
        }

        private void UpdateTerminals(Element Of)
        {
            foreach (Terminal i in Of.Terminals)
                Connect(i, NodeAt(Of.MapTerminal(i), Of as Wire));

            // If Of is a named wire, the nodes might have changed.
            if (Of is Symbol symbol && symbol.Component is NamedWire wire)
                RebuildNodes(wire.ConnectedTo, false);
        }

        private void Connect(Terminal T, Node V)
        {
            if (V != T.ConnectedTo)
                T.ConnectTo(V);
        }

        private void LogComponents()
        {
            foreach (Element i in Symbols)
                Log.WriteLine(MessageType.Verbose, "  " + i.ToString());
            Log.WriteLine(MessageType.Verbose, "  (" + Wires.Count() + " wires)");
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

        public static Schematic Load(string FileName) { return Load(FileName, new NullLog()); }

        public XElement Serialize()
        {
            XElement x = new XElement("Schematic");
            x.SetAttributeValue("Name", Circuit.Name);
            x.SetAttributeValue("Description", Circuit.Description);
            x.SetAttributeValue("PartNumber", Circuit.PartNumber);
            foreach (Element i in Elements)
                x.Add(i.Serialize());
            return x;
        }

        public static Schematic Deserialize(XElement X, ILog Log)
        {
            Schematic s = new Schematic(Log);
            s.Elements.AddRange(X.Elements("Element").Select(i => Element.Deserialize(i)));
            s.Circuit.Name = Value(X.Attribute("Name"));
            s.Circuit.Description = Value(X.Attribute("Description"));
            s.Circuit.PartNumber = Value(X.Attribute("PartNumber"));
            return s;
        }
        public static Schematic Deserialize(XElement X) { return Deserialize(X, new NullLog()); }

        private static string Value(XAttribute X) { return X != null ? X.Value : ""; }
    }
}
