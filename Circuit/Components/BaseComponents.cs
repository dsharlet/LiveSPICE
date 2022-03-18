using ComputerAlgebra;
using System.Collections.Generic;
using System.ComponentModel;

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
        [Browsable(false)]
        public Node ConnectedTo
        {
            get { return Terminal.ConnectedTo; }
            set { Terminal.ConnectedTo = value; }
        }

        /// <summary>
        /// Voltage at this component.
        /// </summary>
        [Browsable(false)]
        public Expression V { get { return Terminal.V; } }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Terminal, new Coord(0, 0));
        }
    }

    /// <summary>
    /// Two terminal component.
    /// </summary>
    public abstract class TwoTerminal : Component
    {
        private Terminal anode, cathode;
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

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Anode, new Coord(0, 20));
            Sym.AddTerminal(Cathode, new Coord(0, -20));
        }
    }
}
