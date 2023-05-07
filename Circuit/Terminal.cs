using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Circuit
{
    /// <summary>
    /// Terminals reference connections to nodes.
    /// </summary>
    public class Terminal
    {
        protected Component owner;
        protected string name;
        public string Name { get { return name != null ? name : owner.Name; } set { name = value; } }
        public string Description { get { return name != null ? owner.Name + "." + name : owner.Name; } }

        // A unique function of t to use when this node isn't connected.
        protected Expression unconnected;
        private static long count = 0;

        public Terminal(Component Owner)
        {
            unconnected = Component.DependentVariable("_v" + Interlocked.Increment(ref count), Component.t);
            owner = Owner;
        }
        public Terminal(Component Owner, string Name) : this(Owner) { name = Name; }

        protected Node connectedTo;
        /// <summary>
        /// The node this terminal is connected to.
        /// </summary>
        public Node ConnectedTo
        {
            get { return connectedTo; }
            set { ConnectTo(value); }
        }

        public bool IsConnected { get { return connectedTo != null; } }

        public Component Owner { get { return owner; } }

        /// <summary>
        /// Connect this terminal to the node.
        /// </summary>
        /// <param name="n"></param>
        /// <returns>true if the connection was changed, false if not.</returns>
        public bool ConnectTo(Node N)
        {
            if (connectedTo == N)
                return false;

            if (connectedTo != null)
                connectedTo.Disconnect(this);
            connectedTo = N;
            if (connectedTo != null)
                connectedTo.Connect(this);

            foreach (EventHandler i in connectionChanged) i(this, null);
            return true;
        }

        private List<EventHandler> connectionChanged = new List<EventHandler>();
        public event EventHandler ConnectionChanged
        {
            add { connectionChanged.Add(value); }
            remove { connectionChanged.Remove(value); }
        }

        /// <summary>
        /// Terminals can be implicitly converted to the node they are connected to.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static implicit operator Node(Terminal x) { return x.ConnectedTo; }

        /// <summary>
        /// Get the voltage expression of the connected node.
        /// </summary>
        public Expression V { get { return ConnectedTo != null ? ConnectedTo.V : unconnected; } }

        public override string ToString() { return Description; }
    }
}
