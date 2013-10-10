using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

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

        public Terminal(Component Owner) { owner = Owner; }
        public Terminal(Component Owner, string Name) { owner = Owner; name = Name; }
        
        protected Node connectedTo;
        /// <summary>
        /// The node this terminal is connected to.
        /// </summary>
        public Node ConnectedTo
        {
            get { return connectedTo; }
            set { ConnectTo(value); }
        }

        public Component Owner { get { return owner; } }

        /// <summary>
        /// Connect this terminal to the node.
        /// </summary>
        /// <param name="n"></param>
        public void ConnectTo(Node N)
        {
            if (connectedTo == N)
                return;

            if (connectedTo != null)
                connectedTo.Disconnect(this);
            connectedTo = N;
            if (connectedTo != null)
                connectedTo.Connect(this);

            foreach (EventHandler i in connectionChanged) i(this, null);
        }

        private List<EventHandler> connectionChanged = new List<EventHandler>();
        public event EventHandler ConnectionChanged
        {
            add { connectionChanged.Add(value); }
            remove { connectionChanged.Remove(value); }
        }

        public Expression i = null;
        public Expression V { get { return ConnectedTo.V; } }

        public override string ToString() { return Description; }
    }
}
