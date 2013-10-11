using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Nodes maintain a list of connected terminals. 
    /// The voltages of all the terminals is equal, and the currents of all terminals sum to 0.
    /// </summary>
    public class Node
    {
        protected Call v;
        /// <summary>
        /// Name of this node.
        /// </summary>
        public string Name { get { return v.Target.Name; } set { v = Call.New(ExprFunction.New(value, Component.t), Component.t); } }

        /// <summary>
        /// Find a unique name for a component in a set of components.
        /// </summary>
        /// <param name="Components"></param>
        /// <returns></returns>
        public static string UniqueName(IEnumerable<Node> Nodes, string Prefix)
        {
            for (int i = 1; ; ++i)
            {
                string name = Prefix + i;
                if (!Nodes.Any(j => j.Name == name))
                    return name;
            }
        }

        private object tag = null;
        [Browsable(false)]
        public object Tag { get { return tag; } set { tag = value; } }

        public Node() : this("v1") { }
        public Node(string Name) { v = Call.New(ExprFunction.New(Name, Component.t), Component.t); }

        protected List<Terminal> connected = new List<Terminal>();
        public void Disconnect(Terminal T) { connected.Remove(T); }
        public void Connect(Terminal T) { connected.Add(T); }
        /// <summary>
        /// Terminals connected to this node.
        /// </summary>
        public IEnumerable<Terminal> Connected { get { return connected; } }

        /// <summary>
        /// Voltage at this node.
        /// </summary>
        public Call V { get { return v; } }

        /// <summary>
        /// Sum of currents at this node.
        /// </summary>
        public Expression Kcl()
        {
            List<Expression> sum = new List<Expression>();
            foreach (Terminal t in connected)
            {
                // If there is a component with undefined current, this node does not have a useful KCL expression.
                if (t.i == null)
                    return null;

                if (!t.i.IsZero())
                    sum.Add(t.i);
            }
            return Add.New(sum);
        }

        public override string ToString() { return Name; }
    }
}
