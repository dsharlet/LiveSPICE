using System.Collections.ObjectModel;
using System.Linq;

namespace Circuit
{
    /// <summary>
    /// Collection of circuit components. Ensures that the names of the components are unique.
    /// </summary>
    public class ComponentCollection : Collection<Component>
    {
        /// <summary>
        /// Get the component with the given Name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Component this[string Name] { get { return Items.Single(i => i.Name == Name); } }

        protected override void InsertItem(int index, Component item)
        {
            // Unique the name of item in this collection.
            item.Name = Component.UniqueName(Items.Select(i => i.Name), item.Name);
            base.InsertItem(index, item);
        }
    }
}
