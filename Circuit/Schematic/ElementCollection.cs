using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuit
{
    /// <summary>
    /// Collection of circuit components.
    /// </summary>
    public class ElementCollection : ICollection<Element>, IEnumerable<Element>
    {
        protected List<Element> x = new List<Element>();
        protected Schematic owner;

        public ElementCollection(Schematic Owner) { owner = Owner; }

        public Element this[int index] { get { return x[index]; } }

        // ICollection<Node>
        public int Count { get { return x.Count; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(Element item)
        {
            x.Add(item);
            //owner.Update();
        }
        public void AddRange(IEnumerable<Element> items)
        {
            x.AddRange(items);
            //owner.Update();
        }
        public void Clear() 
        {
            foreach (Element i in x)
                foreach (Terminal j in i.Terminals)
                    j.ConnectTo(null);
            x.Clear(); 
            //owner.Update(); 
        }
        public bool Contains(Element item) { return x.Contains(item); }
        public void CopyTo(Element[] array, int arrayIndex) { x.CopyTo(array, arrayIndex); }
        public bool Remove(Element item) 
        { 
            bool ret = x.Remove(item);
            if (ret)
                foreach (Terminal i in item.Terminals)
                    i.ConnectTo(null);
            return ret;
        }

        // IEnumerable<Node>
        public IEnumerator<Element> GetEnumerator() { return x.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
