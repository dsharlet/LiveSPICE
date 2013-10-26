using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;
using System.Xml.Linq;

namespace Circuit
{
    /// <summary>
    /// Special component for indicating a serialization error.
    /// </summary>
    public class Error : Component
    {
        public override IEnumerable<Terminal> Terminals { get { return new Terminal[0]; } }

        private XElement data;
        [SchematicPersistent]
        public XElement Data { get { return data; } }

        private string message;
        [SchematicPersistent]
        public string Message { get { return message; } }

        public Error() { }
        public Error(XElement Data, string Message) { data = Data; message = Message; }
        
        public override void Analyze(ModifiedNodalAnalysis Mna) { throw new NotImplementedException("Cannot analyze a circuit with an Error component"); }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.InBounds(new Coord(-20, 20), new Coord(20, -20));
            Sym.AddLoop(EdgeType.Red,
                new Coord(-18, -20),
                new Coord(-19, -19),
                new Coord(-1, 19),
                new Coord(1, 19),
                new Coord(19, -19),
                new Coord(18, -20));
            Sym.AddRectangle(EdgeType.Red, new Coord(-2, 10), new Coord(2, -8));
            Sym.AddRectangle(EdgeType.Red, new Coord(-2, -12), new Coord(2, -16));
        }
    }
}
