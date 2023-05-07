using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;

namespace Circuit
{
    /// <summary>
    /// Exception thrown when a Specialization does not have an implementation.
    /// </summary>
    public class SpecializationNotImplemented : Exception
    {
        public SpecializationNotImplemented() : base("Specialization not implemented.") { }
    }

    /// <summary>
    /// This component represents a specialization of another component type.
    /// </summary>
    public class Specialization : Component
    {
        private Component impl;

        public Specialization() : this(null) { }
        public Specialization(Component Impl) { impl = Impl; }

        // Forward the interesting work to the implementation component.
        public override string Name { get { AssertImpl(); return impl.Name; } set { AssertImpl(); impl.Name = value; NotifyChanged(nameof(Name)); } }
        [Browsable(false)]
        public override string PartNumber { get { AssertImpl(); return impl.PartNumber; } set { AssertImpl(); impl.PartNumber = value; NotifyChanged(nameof(PartNumber)); } }
        public override IEnumerable<Terminal> Terminals { get { AssertImpl(); return impl.Terminals; } }
        public override void Analyze(Analysis Mna) { AssertImpl(); impl.Analyze(Mna); }
        protected internal override void LayoutSymbol(SymbolLayout Sym) { AssertImpl(); impl.LayoutSymbol(Sym); }
        public override object Tag { get => impl.Tag; set => impl.Tag = value; }
        public override string TypeName { get { return PartNumber; } }

        public override XElement Serialize()
        {
            AssertImpl();
            XElement E = base.Serialize();
            E.Add(impl.Serialize());
            return E;
        }

        protected override void DeserializeImpl(XElement X)
        {
            impl = Deserialize(X.Element("Component"));
            base.DeserializeImpl(X);
        }

        private void AssertImpl() { if (impl == null) throw new SpecializationNotImplemented(); }
    }
}
