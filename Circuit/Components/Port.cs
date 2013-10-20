using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Circuit port component. Connections between circuit ports form a buffer.
    /// </summary>
    public abstract class Port : OneTerminal { }

    /// <summary>
    /// Output ports have infinite output impedance, i.e. they do not accept any current.
    /// </summary>
    [CategoryAttribute("IO")]
    [DisplayName("Output Port")]
    public class OutputPort : Port
    {
        public override void Analyze(ICollection<Equal> Mna, ICollection<Expression> Unknowns) { i = Constant.Zero; }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 0),
                new Coord(10, 0),
                new Coord(10, -10),
                new Coord(0, -20),
                new Coord(-10, -10));

            Sym.DrawText(Name, new Coord(12, -10), Alignment.Near, Alignment.Center);
        }
    }

    /// <summary>
    /// Input ports have zero input impedance, i.e. they can supply infinite current to maintain the input voltage.
    /// </summary>
    [CategoryAttribute("IO")]
    [DisplayName("Input Port")]
    public class InputPort : Port
    {
        public override void Analyze(ICollection<Equal> Mna, ICollection<Expression> Unknowns)
        { 
            i = null; 
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 20),
                new Coord(10, 20),
                new Coord(10, 10),
                new Coord(0, 0),
                new Coord(-10, 10));

            Sym.DrawText(Name, new Coord(12, 10), Alignment.Near, Alignment.Center);
        }
    }
}
