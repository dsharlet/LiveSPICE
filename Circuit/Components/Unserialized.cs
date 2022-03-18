using System.Collections.Generic;
using System.Xml.Linq;

namespace Circuit
{
    /// <summary>
    /// Special component for indicating a serialization error.
    /// </summary>
    abstract class UnserializedComponent : Component
    {
        public override IEnumerable<Terminal> Terminals { get { return new Terminal[0]; } }

        private XElement data;
        [Serialize]
        public XElement Data { get { return data; } }

        private string message;
        [Serialize]
        public string Message { get { return message; } }

        protected UnserializedComponent() { }
        public UnserializedComponent(XElement Data, string Message) { data = Data; message = Message; }

        /// <summary>
        /// Serializing an unserialized component replaces the data that could not be serialized.
        /// </summary>
        /// <returns></returns>
        public override XElement Serialize() { return Data; }

        public override string ToString() { return Message; }
    }

    /// <summary>
    /// Component that prevent analysis.
    /// </summary>
    class Error : UnserializedComponent
    {
        public Error(XElement Data, string Message) : base(Data, Message) { }

        public override void Analyze(Analysis Mna) { throw new AnalysisException("Cannot analyze a circuit with Error component."); }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
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

    /// <summary>
    /// Component that prevent analysis.
    /// </summary>
    class Warning : UnserializedComponent
    {
        public Warning(XElement Data, string Message) : base(Data, Message) { }

        public override void Analyze(Analysis Mna) { }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.InBounds(new Coord(-20, 20), new Coord(20, -20));
            Sym.AddLoop(EdgeType.Orange,
                new Coord(-18, -20),
                new Coord(-19, -19),
                new Coord(-1, 19),
                new Coord(1, 19),
                new Coord(19, -19),
                new Coord(18, -20));
            Sym.AddRectangle(EdgeType.Orange, new Coord(-2, 10), new Coord(2, -8));
            Sym.AddRectangle(EdgeType.Orange, new Coord(-2, -12), new Coord(2, -16));
        }
    }
}
