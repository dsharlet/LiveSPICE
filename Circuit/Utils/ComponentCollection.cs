using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuit
{
    /// <summary>
    /// Collection of circuit components. Ensures that the names of the components are unique.
    /// </summary>
    public class ComponentCollection : ICollection<Component>, IEnumerable<Component>
    {
        protected List<Component> x = new List<Component>();
        
        public Component this[int index] { get { return x[index]; } }

        // ICollection<Component>
        public int Count { get { return x.Count; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(Component item)
        {
            if (x.Any(i => i.Name == item.Name))
                item.Name = Component.UniqueName(x, GetPrefix(item.Name));
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

        private static string GetPrefix(string Name)
        {
            return new string(Name.TakeWhile(i => !Char.IsDigit(i)).ToArray());
        }
    }
}
