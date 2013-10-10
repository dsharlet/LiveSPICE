using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuit
{
    /// <summary>
    /// Collection of nodes. Ensures that the node names are unique.
    /// </summary>
    public class NodeCollection : ICollection<Node>, IEnumerable<Node>
    {
        protected List<Node> x = new List<Node>();
        
        public Node this[int index] { get { return x[index]; } }

        // ICollection<Node>
        public int Count { get { return x.Count; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(Node item)
        {
            if (x.Any(i => i.Name == item.Name))
                throw new InvalidOperationException("Node with name '" + item.Name + "' already exists.");
            x.Add(item);
        }
        public void AddRange(IEnumerable<Node> items)
        {
            foreach (Node i in items)
                Add(i);
        }
        public void AddRange(params Node[] items) { AddRange(items.AsEnumerable()); }
        public void Clear() { x.Clear(); }
        public bool Contains(Node item) { return x.Contains(item); }
        public void CopyTo(Node[] array, int arrayIndex) { x.CopyTo(array, arrayIndex); }
        public bool Remove(Node item) { return x.Remove(item); }

        // IEnumerable<Node>
        public IEnumerator<Node> GetEnumerator() { return x.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
