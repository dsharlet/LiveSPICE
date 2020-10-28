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
        private string name;
        /// <summary>
        /// Name of this node.
        /// </summary>
        public string Name { get { return name; } set { name = value; } }

        private object tag = null;
        [Browsable(false)]
        public object Tag { get { return tag; } set { tag = value; } }

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

        private Call v = null;
        /// <summary>
        /// Voltage at this node.
        /// </summary>
        public Expression V
        {
            get
            {
                if (!(v is null))
                    return v;
                else
                    return Call.New(name, Component.t);
            }
        }

        /// <summary>
        /// Begin analysis with this node in the given context.
        /// </summary>
        /// <param name="Context"></param>
        public void BeginAnalysis(string Context)
        {
            if (!(v is null))
                throw new InvalidOperationException("Node '" + name + "' is already part of an analysis.");

            v = Call.New(Context + name, Component.t);
        }

        /// <summary>
        /// End analysis with this node.
        /// </summary>
        public void EndAnalysis()
        {
            v = null;
        }

        public override string ToString() { return Name; }
    }
}
