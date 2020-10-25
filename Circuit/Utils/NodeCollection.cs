using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Circuit
{
    /// <summary>
    /// Collection of nodes. Ensures that the node names are unique.
    /// </summary>
    public class NodeCollection : ICollection<Node>, IEnumerable<Node>
    {
        protected List<Node> x = new List<Node>();

        public Node this[int index] { get { return x[index]; } }
        /// <summary>
        /// Get the node with the given Name. If it doesn't exist, it will be created.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Node this[string Name]
        {
            get
            {
                Node n = x.SingleOrDefault(i => i.Name == Name);
                if (n != null)
                    return n;

                n = new Node() { Name = Name };
                x.Add(n);
                return n;
            }
        }

        // ICollection<Node>
        public int Count { get { return x.Count; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(Node item)
        {
            // Unique the name of item in this collection.
            item.Name = Component.UniqueName(x.Select(i => i.Name), item.Name);
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
