using System;
using System.Collections;
using System.Collections.Generic;

namespace Circuit
{
    public class ElementEventArgs : EventArgs
    {
        private Element e;
        public Element Element { get { return e; } }

        public ElementEventArgs(Element E) { e = E; }
    }

    /// <summary>
    /// Collection of circuit elements.
    /// </summary>
    public class ElementCollection : ICollection<Element>, IEnumerable<Element>
    {
        protected List<Element> x = new List<Element>();

        public Element this[int index] { get { return x[index]; } }

        public delegate void ElementEventHandler(object sender, ElementEventArgs e);

        private List<ElementEventHandler> itemAdded = new List<ElementEventHandler>();
        protected void OnItemAdded(ElementEventArgs e) { foreach (ElementEventHandler i in itemAdded) i(this, e); }
        public event ElementEventHandler ItemAdded
        {
            add { itemAdded.Add(value); }
            remove { itemAdded.Remove(value); }
        }

        private List<ElementEventHandler> itemRemoved = new List<ElementEventHandler>();
        protected void OnItemRemoved(ElementEventArgs e) { foreach (ElementEventHandler i in itemRemoved) i(this, e); }
        public event ElementEventHandler ItemRemoved
        {
            add { itemRemoved.Add(value); }
            remove { itemRemoved.Remove(value); }
        }

        // ICollection<Node>
        public int Count { get { return x.Count; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(Element item)
        {
            x.Add(item);
            OnItemAdded(new ElementEventArgs(item));
        }
        public void AddRange(IEnumerable<Element> items)
        {
            foreach (Element i in items)
                Add(i);
        }
        public void Clear()
        {
            Element[] removed = x.ToArray();
            x.Clear();

            foreach (Element i in removed)
                OnItemRemoved(new ElementEventArgs(i));
        }
        public bool Contains(Element item) { return x.Contains(item); }
        public void CopyTo(Element[] array, int arrayIndex) { x.CopyTo(array, arrayIndex); }
        public bool Remove(Element item)
        {
            bool ret = x.Remove(item);
            if (ret)
                OnItemRemoved(new ElementEventArgs(item));
            return ret;
        }
        public void RemoveRange(IEnumerable<Element> items)
        {
            foreach (Element i in items)
                Remove(i);
        }

        // IEnumerable<Node>
        public IEnumerator<Element> GetEnumerator() { return x.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
