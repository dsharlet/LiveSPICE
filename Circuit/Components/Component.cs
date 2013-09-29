using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    public class SchematicPersistent : Attribute { };
    public class SimulationParameter : Attribute
    {
        public SimulationParameter() { }
    }

    public class RangedSimulationParameter : SimulationParameter
    {
        private double min, max;
        public double Min { get { return min; } }
        public double Max { get { return max; } }

        public RangedSimulationParameter(double Min, double Max) { min = Min; max = Max; }
    }

    /// <summary>
    /// Components are capable of performing a KCL analysis to produce a set of expressions.
    /// </summary>
    public abstract class Component : INotifyPropertyChanged
    {
        /// <summary>
        /// Time of the simulation.
        /// </summary>
        public static readonly Variable t = Variable.New("t");

        /// <summary>
        /// Sampling period of the simulation.
        /// </summary>
        public static readonly Variable T = Variable.New("T");

        private string name = "X1";
        [Description("Unique name of this component.")]
        [SchematicPersistent]
        public virtual string Name { get { return name; } set { name = value; NotifyChanged("Name"); } }

        private object tag = null;
        [Browsable(false)]
        public object Tag { get { return tag; } set { tag = value; NotifyChanged("Tag"); } }

        /// <summary>
        /// Find a unique name for a component in a set of components.
        /// </summary>
        /// <param name="Components"></param>
        /// <returns></returns>
        public static string UniqueName(IEnumerable<Component> Components, string Prefix)
        {
            for (int i = 1; ; ++i)
            {
                string name = Prefix + i;
                if (!Components.Any(j => j.Name == name))
                    return name;
            }
        }

        /// <summary>
        /// Access the terminals of this component.
        /// </summary>
        [Browsable(false)]
        public abstract IEnumerable<Terminal> Terminals { get; }

        /// <summary>
        /// Compute a system of equations representing a KCL node analysis.
        /// </summary>
        /// <returns>Set expression containing the KCL equations.</returns>
        public List<Equal> Analyze()
        {
            List<Equal> Kcl = new List<Equal>();
            Analyze(Kcl);
            return Kcl;
        }

        protected virtual void Analyze(IList<Equal> Kcl) { }
        
        /// <summary>
        /// Define the schematic layout of this component.
        /// </summary>
        /// <param name="S"></param>
        public abstract void LayoutSymbol(SymbolLayout Sym);

        public override string ToString() { return Name; }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
