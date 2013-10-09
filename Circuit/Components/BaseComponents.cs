using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Component with a single terminal.
    /// </summary>
    public abstract class OneTerminal : Component
    {
        protected Terminal terminal;
        [Browsable(false)]
        public Terminal Terminal { get { return terminal; } }
        public override sealed IEnumerable<Terminal> Terminals { get { yield return terminal; } }

        public OneTerminal() { terminal = new Terminal(this); }
        
        /// <summary>
        /// Connect the terminal of this port.
        /// </summary>
        /// <param name="N"></param>
        public void ConnectTo(Node N) { Terminal.ConnectTo(N); }

        /// <summary>
        /// Node this terminal is connected to.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Browsable(false)]
        public Node ConnectedTo
        {
            get { return Terminal.ConnectedTo; }
            set { Terminal.ConnectedTo = value; }
        }

        /// <summary>
        /// Current flowing to this component.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Browsable(false)]
        public Expression i 
        { 
            get { return Terminal.i; } 
            set { Terminal.i = value; } 
        }

        /// <summary>
        /// Voltage at this component.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        [Browsable(false)]
        public Expression V { get { return Terminal.V; } }

        public override sealed void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Terminal, new Coord(0, 0));

            DrawSymbol(Sym);
        }

        protected virtual void DrawSymbol(SymbolLayout Sym)
        {
            //Sym.DrawName(Name, new PointF(0.0f, 0.0f));
        }
    }

    /// <summary>
    /// Two terminal component.
    /// </summary>
    public abstract class TwoTerminal : Component
    {
        protected Terminal anode, cathode;
        [Browsable(false)]
        public Terminal Anode { get { return anode; } }
        [Browsable(false)]
        public Terminal Cathode { get { return cathode; } }
        public override sealed IEnumerable<Terminal> Terminals { get { yield return anode; yield return cathode; } }

        public TwoTerminal()
        {
            anode = new Terminal(this, "Anode");
            cathode = new Terminal(this, "Cathode");
        }

        /// <summary>
        /// Voltage drop across this component, V = Anode - Cathode.
        /// </summary>
        [Browsable(false)]
        public Expression V { get { return Anode.V - Cathode.V; } }

        /// <summary>
        /// Connect the terminals of this component to the given nodes.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="C"></param>
        public void ConnectTo(Node A, Node C)
        {
            Anode.ConnectTo(A);
            Cathode.ConnectTo(C);
        }

        public override sealed void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Anode, new Coord(0, 20));
            Sym.AddTerminal(Cathode, new Coord(0, -20));

            DrawSymbol(Sym);
        }

        protected abstract void DrawSymbol(SymbolLayout Sym);
    }

    /// <summary>
    /// Two terminal component with i = f(V).
    /// </summary>
    public abstract class PassiveTwoTerminal : TwoTerminal
    {
        /// <summary>
        /// Current from the Anode to the Cathode.
        /// </summary>
        [Browsable(false)]
        public abstract Expression i(Expression V);

        public override sealed void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            Expression i = this.i(V);  

            Anode.i = i;
            Cathode.i = Multiply.New(Constant.New(-1), i);
        }
    }
}
