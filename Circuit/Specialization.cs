using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Xml.Linq;
using ComputerAlgebra;

namespace Circuit
{
    /// <summary>
    /// This component represents a specialization of another component type.
    /// </summary>
    public class Specialization : Component
    {
        private Component impl;

        public Specialization() : this(null) { }
        public Specialization(Component Impl) { impl = Impl; }

        // Forward the interesting work to the implementation component.
        public override string Name { get { return impl.Name; } set { impl.Name = value; NotifyChanged("Name"); } }
        [Browsable(false)]
        public override string PartNumber { get { return impl.PartNumber; } set { impl.PartNumber = value; NotifyChanged("PartNumber"); } }
        public override IEnumerable<Terminal> Terminals { get { return impl.Terminals; } }
        public override void Analyze(Analysis Mna) { impl.Analyze(Mna); }
        public override void LayoutSymbol(SymbolLayout Sym) { impl.LayoutSymbol(Sym); }
        
        public override string TypeName { get { return PartNumber; } }

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
