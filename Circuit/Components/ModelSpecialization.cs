using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Xml.Linq;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// This component wraps another component with predefined property values.
    /// </summary>
    public class ModelSpecialization : Component
    {
        private Component impl;

        public ModelSpecialization() { }

        public ModelSpecialization(Component Impl) 
        { 
            impl = Impl;
            category = impl.GetCategory();
            displayName = impl.PartNumber != "" ? impl.PartNumber : impl.GetDisplayName();
            description = impl.GetDescription();
        }

        // Forward the interesting work to the implementation component.
        public override string Name { get { return impl.Name; } set { impl.Name = value; NotifyChanged("Name"); } }
        [Browsable(false)]
        public override string PartNumber { get { return impl.PartNumber; } set { impl.PartNumber = value; NotifyChanged("PartNumber"); } }
        public override IEnumerable<Terminal> Terminals { get { return impl.Terminals; } }
        public override void Analyze(ModifiedNodalAnalysis Mna) { impl.Analyze(Mna); }
        public override void LayoutSymbol(SymbolLayout Sym) { impl.LayoutSymbol(Sym); }
        
        private string category = "";
        [Serialize]
        [Browsable(false)]
        public string Category { get { return category; } set { category = value; NotifyChanged("Categories"); } }

        private string displayName = "";
        [Serialize]
        [Browsable(false)]
        public string DisplayName { get { return displayName; } set { displayName = value; NotifyChanged("DisplayName"); } }

        private string description = "";
        [Serialize]
        [Browsable(false)]
        public string Description { get { return description; } set { description = value; NotifyChanged("Description"); } }

        public override string GetCategory() { return category; }
        public override string GetDisplayName() { return displayName; }
        public override string GetDescription() { return description; }

        public override XElement Serialize()
        {
            XElement E = base.Serialize();
            E.Add(impl.Serialize());
            return E;
        }

        protected override void DeserializeImpl(XElement X)
        {
            impl = Deserialize(X.Element("Component"));
            base.DeserializeImpl(X);
        }
    }
}
