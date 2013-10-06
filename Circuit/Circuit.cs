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
        public ComponentCollection Components = new ComponentCollection();
        public NodeCollection Nodes = new NodeCollection();
        
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

        /// <summary>
        /// Create a circuit from a SPICE netlist.
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
        public static Circuit FromNetlist(string Filename)
        {
            return Netlist.Parse(Filename);
        }

        public override void Analyze(IList<Equal> Kcl, IList<Expression> Unknowns)
        {
            foreach (Component c in Components)
                c.Analyze(Kcl, Unknowns);
            foreach (Node n in Nodes)
            {
                Unknowns.Add(n.V);
                Expression i = n.Kcl();
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