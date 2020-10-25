using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Circuit
{
    /// <summary>
    /// Collection of circuit components. Ensures that the names of the components are unique.
    /// </summary>
    public class ComponentCollection : ICollection<Component>, IEnumerable<Component>
    {
        protected List<Component> x = new List<Component>();

        public Component this[int index] { get { return x[index]; } }
        /// <summary>
        /// Get the component with the given Name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Component this[string Name] { get { return x.Single(i => i.Name == Name); } }

        // ICollection<Component>
        public int Count { get { return x.Count; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(Component item)
        {
            // Unique the name of item in this collection.
            item.Name = Component.UniqueName(x.Select(i => i.Name), item.Name);
            x.Add(item);
        }
        public void AddRange(IEnumerable<Component> items)
        {
            foreach (Component i in items)
                Add(i);
        }
        public void AddRange(params Component[] items) { AddRange(items.AsEnumerable()); }
        public void Clear() { x.Clear(); }
        public bool Contains(Component item) { return x.Contains(item); }
        public void CopyTo(Component[] array, int arrayIndex) { x.CopyTo(array, arrayIndex); }
        public bool Remove(Component item) { return x.Remove(item); }

        // IEnumerable<Component>
        public IEnumerator<Component> GetEnumerator() { return x.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
