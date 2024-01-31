using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        public Schematic(ILog Log, IEnumerable<Element> Elements = null) : this(Elements) 
        {
            log = Log; 
        }
        public Schematic(IEnumerable<Element> Elements = null)
        {
            elements = new ElementCollection();
            if (Elements != null)
            {
                // If we rely on the normal events for adding/removing these components,
                // it's very slow because the nodes get rebuilt every time a new
                // component is added. To avoid this, just manually connect everything
                // here, and then rebuild the nodes once.
                elements.AddRange(Elements);
                foreach (Symbol i in elements.OfType<Symbol>())
                    circuit.Components.Add(i.Component);
                RebuildNodes();
                ReconnectAllTerminals();
            }

            // We skipped this usual event setup for the initial elements, add the events now.
            foreach (Element i in elements)
                i.LayoutChanged += OnLayoutChanged;
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

            RebuildNodes();
            ReconnectAllTerminals();

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
        public Node NodeAt(Coord x)
        {
            foreach (Element e in elements)
                if (e is Wire w && w.IsConnectedTo(x))
                    return w.Node;
            return null;
        }

        protected void OnElementAdded(object sender, ElementEventArgs e)
        {
            if (e.Element is Symbol symbol)
                circuit.Components.Add(symbol.Component);
            OnLayoutChanged(e.Element, null);

            e.Element.LayoutChanged += OnLayoutChanged;
        }

        protected void OnElementRemoved(object sender, ElementEventArgs e)
        {
            e.Element.LayoutChanged -= OnLayoutChanged;

            if (e.Element is Symbol symbol)
            {
                circuit.Components.Remove(symbol.Component);
            }
            else if (e.Element is Wire wire)
            {
                // If the removed element is a wire, we might have to split the node it was a part of.
                RebuildNode(wire.Node);

                // Reconnect any terminals connected to this wire's node, in case they are no longer connected.
                ReconnectAllTerminals(wire.Node.Connected.Select(i => (Element)i.Owner.Tag).ToArray());
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
            {
                RebuildNode(wire.Node);

                // Reconnect all the elements connected to this wire's node. This will disconnect any newly
                // disconnected nodes.
                ReconnectAllTerminals(wire.Node.Connected.Select(i => (Element)i.Owner.Tag).ToArray());
                
                // Now reconnect all terminals in the bounding box of this wire.
                ReconnectAllTerminals(Elements.Where(i => i.Intersects(wire.LowerBound, wire.UpperBound)));
            }
            else
            {
                ReconnectAllTerminals(new Element[] { of });
            }
        }

        private IEnumerable<Wire> ConnectedTo(IEnumerable<Wire> Wires, Wire Target, HashSet<Wire> visited)
        {
            foreach (Wire i in Wires)
            {
                if (!visited.Contains(i) && i.IsConnectedTo(Target))
                {
                    visited.Add(i);
                    yield return i;
                    foreach (Wire j in ConnectedTo(Wires, i, visited))
                        yield return j;
                }
            }
        }

        // Wires was a single node but it is now two. Break it into two sets of nodes
        // and return the one containing Target.
        private IEnumerable<Wire> ConnectedTo(IEnumerable<Wire> Wires, Wire Target)
        {
            HashSet<Wire> visited = new HashSet<Wire>();
            return ConnectedTo(Wires.Buffer(), Target, visited);
        }

        // Merge all of the nodes contained in wires to one.
        private Node MergeNode(IEnumerable<Wire> Wires)
        {
            // All the existing nodes contained in wires.
            List<Node> nodes = Wires.Select(i => i.Node).Where(i => i != null).Distinct().ToList();

            Node n = null;

            // If this set of wires is connected to a NamedWire, use that as the node.
            if (n == null)
            {
                foreach (Node i in nodes)
                    foreach (Terminal j in i.Connected)
                        if (j.Owner is NamedWire w)
                            n = circuit.Nodes[w.WireName];
            }

            // If there are no NamedWires, use one of the nodes already in the set.
            if (n == null)
                n = nodes.FirstOrDefault();

            // If there were no nodes in the set, just make a new one.
            if (n == null)
            {
                n = new Node();
                circuit.Nodes.Add(n);
            }

            foreach (Wire i in Wires.Where(j => j.Node != n))
            {
                i.Node = n;
                foreach (Terminal j in i.Terminals)
                    j.ConnectTo(n);
            }

            // Everything connected to a node in nodes should now be connected to n.
            foreach (Node i in nodes.Where(j => j != n))
            {
                foreach (Terminal j in i.Connected.ToArray())
                    j.ConnectTo(n);
                circuit.Nodes.Remove(i);
            }
            return n;
        }

        private void ReconnectAllTerminals(IEnumerable<Element> Elements)
        {
            HashSet<Node> modified = new HashSet<Node>();
            foreach (Element i in Elements)
            {
                foreach (Terminal j in i.Terminals)
                {
                    Node n = NodeAt(i.MapTerminal(j));
                    if (j.ConnectTo(n))
                        if (n != null && j.Owner is NamedWire)
                            modified.Add(n);
                }
            }
            // Rebuild the nodes modified by a named wire case a named wire changed the name of the nodes.
            if (modified.Any())
                RebuildNodes(modified);
        }
        private void ReconnectAllTerminals() { ReconnectAllTerminals(elements); }

        private void RebuildNodes(IEnumerable<Node> Nodes = null)
        {
            HashSet<Wire> wires = new HashSet<Wire>();
            foreach (Wire i in Wires)
                if (Nodes == null || i.Node == null || Nodes.Contains(i.Node))
                    wires.Add(i);
            while (!wires.Empty())
            {
                // Find all the wires connected to the first wire in the list.
                IEnumerable<Wire> connected = ConnectedTo(wires, wires.First()).Buffer();

                // Merge all the connected wires into one node.
                Node node = MergeNode(connected);

                // Remove the wires we just made into a node from the list.
                foreach (Wire i in connected)
                    wires.Remove(i);

                // Any reminaing wires cannot be connected to the new node.
                foreach (Wire i in wires.Where(j => j.Node == node))
                    i.Node = null;
            }
        }
        private void RebuildNode(Node At)
        {
            RebuildNodes(new Node[] { At });
        }

        private void LogComponents()
        {
            foreach (Element i in Symbols)
                Log.WriteLine(MessageType.Verbose, "  " + i.ToString());
            Log.WriteLine(MessageType.Verbose, "  (" + Wires.Count() + " wires)");
        }

        // The .NET wrapper for this doesn't support allowing overwriting until .NET 8 :(
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);

        static int MOVEFILE_REPLACE_EXISTING = 1;
        static int MOVEFILE_COPY_ALLOWED = 2;

        public void Save(string FileName)
        {
            XDocument doc = new XDocument();
            doc.Add(Serialize());
            // Try to make file writing atomic, in case other applications like
            // VST plugins are watching for changes.
            string temp = FileName + ".temp";
            doc.Save(temp);
            if (!MoveFileEx(temp, FileName, MOVEFILE_COPY_ALLOWED | MOVEFILE_REPLACE_EXISTING))
            {
                // If the MoveFileEx call failed, just save it the regular way. This should never
                // actually work, but we'll get a more descriptive error message if it fails.
                // The only reasons MoveFileEx can fail either don't apply here (file is on a
                // different volume) or reasons that will cause this to fail as well (file can't
                // be written for some reason).
                doc.Save(FileName);
            }
            Log.WriteLine(MessageType.Info, "Schematic saved to '" + FileName + "'");
            LogComponents();
        }

        public static Schematic Load(string FileName, ILog Log)
        {
            XDocument doc = XDocument.Load(FileName);
            Schematic S = Deserialize(doc.Root, Log);
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
            IEnumerable<Element> elements = X.Elements("Element").Select(i => Element.Deserialize(i));
            Schematic s = new Schematic(Log, elements);
            s.Circuit.Name = Value(X.Attribute("Name"));
            s.Circuit.Description = Value(X.Attribute("Description"));
            s.Circuit.PartNumber = Value(X.Attribute("PartNumber"));
            return s;
        }
        public static Schematic Deserialize(XElement X) { return Deserialize(X, new NullLog()); }

        private static string Value(XAttribute X) { return X != null ? X.Value : ""; }
    }
}
