using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Nodes maintain a list of connected terminals. 
    /// The voltages of all the terminals is equal, and the currents of all terminals sum to 0.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Name of this node.
        /// </summary>
        public string Name { get; set; }

        public Node() { Name = "_v1"; }

        protected List<Terminal> connected = new List<Terminal>();
        /// <summary>
        /// Connect a terminal to this node.
        /// </summary>
        /// <param name="T"></param>
        public void Connect(Terminal T) { connected.Add(T); }
        /// <summary>
        /// Disconnect a terminal from this node.
        /// </summary>
        /// <param name="T"></param>
        public void Disconnect(Terminal T) { connected.Remove(T); }
        /// <summary>
        /// Terminals connected to this node.
        /// </summary>
        public IEnumerable<Terminal> Connected { get { return connected; } }

        private Call _v;
        /// <summary>
        /// Voltage at this node.
        /// </summary>
        public Expression V => _v ?? Call.New(Name, Component.t);

        /// <summary>
        /// Begin analysis with this node in the given context.
        /// </summary>
        /// <param name="context"></param>
        public void BeginAnalysis(string context)
        {
            if (!(_v is null))
                throw new InvalidOperationException("Node '" + Name + "' is already part of an analysis.");

            _v = Call.New(context + Name, Component.t);
        }

        /// <summary>
        /// End analysis with this node.
        /// </summary>
        public void EndAnalysis()
        {
            _v = null;
        }

        public override string ToString() { return Name; }
    }
}
