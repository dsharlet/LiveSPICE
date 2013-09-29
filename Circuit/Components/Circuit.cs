using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using SyMath;

namespace Circuit
{
    public class NonLinearCircuit : Exception
    {
    }
        
    /// <summary>
    /// Circuits contain a list of nodes and components.
    /// </summary>
    public class Circuit : Component, IXmlSerializable
    {
        public List<Component> Components = new List<Component>();
        public List<Node> Nodes = new List<Node>();
        
        /// <summary>
        /// External terminals (ports) in this circuit.
        /// </summary>
        public override IEnumerable<Terminal> Terminals 
        {
            get 
            { 
                return Components.OfType<Port>().Select(i => i.Terminal);
            }
        }

        protected override void Analyze(IList<Equal> Kcl)
        {
            foreach (Component c in Components)
                foreach (Equal i in c.Analyze())
                    Kcl.Add(i);
            foreach (Node n in Nodes)
            {
                Expression i = n.i;
                if (i != null)
                    Kcl.Add(Equal.New(i, Constant.Zero));
            }
        }

        /// <summary>
        /// Get the single input port if there is exactly one input port. Throws otherwise.
        /// </summary>
        InputPort Input { get { return (InputPort)Components.Single(x => x is InputPort); } }
        /// <summary>
        /// Get the single output port if there is exactly one input port. Throws otherwise.
        /// </summary>
        OutputPort Output { get { return (OutputPort)Components.Single(x => x is OutputPort); } }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            throw new NotImplementedException();
        }
        
        public void ReadXml(XmlReader Reader)
        {
        }

        public void WriteXml(XmlWriter Writer)
        {
        }

        public XmlSchema GetSchema()
        {
            return null;
        }
    };
}