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
        
        protected void OnElementAdded(object sender, ElementEventArgs e)
        {
            if (e.Element is Symbol)
                Circuit.Components.Add(((Symbol)e.Element).Component);
            UpdateTerminals(e.Element);

            e.Element.LayoutChanged += OnLayoutChanged;

            Log.WriteLine(MessageType.Info, "Added '" + e.Element.ToString() + "'");
        }

        protected void OnElementRemoved(object sender, ElementEventArgs e)
        {
            e.Element.LayoutChanged -= OnLayoutChanged;

            foreach (Terminal i in e.Element.Terminals)
            {
                Node n = i.ConnectedTo;
                i.ConnectTo(null);

                // If the node that this terminal was connected to has no more connections, remove it from the circuit.
                if (n != null && n.Connected.Empty())
                {
                    Log.WriteLine(MessageType.Info, "Removed node '" + n.ToString() + "'");
                    circuit.Nodes.Remove(n);
                }
            }
            if (e.Element is Symbol)
                Circuit.Components.Remove(((Symbol)e.Element).Component);

            Log.WriteLine(MessageType.Info, "Removed '" + e.Element.ToString() + "'");
        }

        // When an element moves, we will need to update its connections.
        protected void OnLayoutChanged(object sender, EventArgs e)
        {
            UpdateTerminals((Element)sender);
        }

        protected void UpdateTerminals(Element e)
        {
        }

        
        public void Save(string FileName)
        {
            XDocument doc = new XDocument();
            doc.Add(Serialize());
            doc.Save(FileName);
        }

        public static Schematic Load(string FileName, ILog Log)
        {
            XDocument doc = XDocument.Load(FileName);
            return Schematic.Deserialize(doc.Root, Log);
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
