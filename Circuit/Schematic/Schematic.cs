using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            elements = new ElementCollection(this);
        }
                
        /// <summary>
        /// Map the coordinate x to a Node.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public Node NodeAt(Coord x)
        {
            WireElement e = elements.OfType<WireElement>().FirstOrDefault(i => i.Intersects(x, x));
            if (e == null)
                return null;
            if (e.Node == null)
            {
                e.Node = new Node();
                circuit.Nodes.Add(e.Node);
            }
            return e.Node;
        }

        // When an element moves, we will need to update its connections.
        private List<Element> dirty = new List<Element>();
        protected void OnLayoutChanged(object sender, EventArgs e)
        {
            dirty.Add((Element)sender);
            Update();
        }

        protected void Update()
        {
            Element[] update = dirty.ToArray();
            dirty.Clear();
            foreach (Element i in update)
            {
                foreach (Terminal j in i.Terminals)
                {
                    Coord x = i.MapTerminal(j);

                    // Get the node at this location.
                    Node n = NodeAt(x);

                    // If this coord lands in the middle of a wire, we need to split the wire.
                    WireElement split = elements.OfType<WireElement>().SingleOrDefault(k => k.Intersects(x, x));
                    if (split != null)
                    {
                        WireElement other = new WireElement(split.A, x);
                        split.A = x;

                        elements.Add(other);
                    }

                    j.ConnectTo(NodeAt(x));
                }
            }
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
