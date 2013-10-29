using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Reflection;
using SyMath;

namespace Circuit
{
    public class Serialize : Attribute { };
    
    /// <summary>
    /// Components are capable of performing MNA to produce a set of equations and unknowns describing their behavior.
    /// </summary>
    public abstract class Component : INotifyPropertyChanged
    {
        /// <summary>
        /// Time of the simulation.
        /// </summary>
        public static readonly Variable t = Variable.New("t");
        /// <summary>
        /// Previous timestep of the simulation.
        /// </summary>
        public static readonly Variable t0 = Variable.New("t0");

        /// <summary>
        /// Sampling period of the simulation.
        /// </summary>
        public static readonly Variable T = Variable.New("T");

        /// <summary>
        /// Thermal voltage.
        /// </summary>
        public const double VT = 25.35e-3;

        private string name = "X1";
        [Description("Unique name of this component.")]
        [Serialize]
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
        /// Add any extra MNA equations required by this component.
        /// </summary>
        /// <param name="Mna"></param>
        /// <param name="Unknowns"></param>
        public virtual void Analyze(ModifiedNodalAnalysis Mna) { }
        
        /// <summary>
        /// Define the schematic layout of this component.
        /// </summary>
        /// <param name="S"></param>
        public abstract void LayoutSymbol(SymbolLayout Sym);

        public virtual XElement Serialize()
        {
            XElement X = new XElement("Component");
            Type T = GetType();
            X.SetAttributeValue("_Type", T.AssemblyQualifiedName);

            while (T != null)
            {
                foreach (PropertyInfo i in T.GetProperties().Where(i => i.GetCustomAttribute<Serialize>() != null))
                {
                    System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
                    X.SetAttributeValue(i.Name, tc.ConvertToString(i.GetValue(this)));
                }
                T = T.BaseType;
            }
            return X;
        }

        protected virtual void DeserializeImpl(XElement X)
        {
            Type T = GetType();
            while (T != null)
            {
                foreach (PropertyInfo i in GetType().GetProperties().Where(i => i.GetCustomAttribute<Serialize>() != null))
                {
                    XAttribute attr = X.Attribute(i.Name);
                    if (attr != null)
                    {
                        System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
                        i.SetValue(this, tc.ConvertFromString(attr.Value));
                    }
                }
                T = T.BaseType;
            }
        }

        public static Component Deserialize(XElement X)
        {
            try
            {
                XAttribute type = X.Attribute("_Type");
                if (type == null)
                    throw new Exception("Component type not defined.");
                Type T = Type.GetType(type.Value);
                if (T == null)
                    throw new Exception("Type '" + type.Value + "' not found.");
                Component c = (Component)T.GetConstructor(new Type[0]).Invoke(new object[0]);
                c.DeserializeImpl(X);
                return c;
            }
            catch (TargetInvocationException Ex)
            {
                throw Ex.InnerException;
            }
        }

        // This is too useful not to have.
        protected static Expression D(Expression f, Expression x) { return Call.D(f, x); }
        // Make a variable Name dependent on On.
        public static Expression DependentVariable(string Name, params Expression[] Args) 
        { 
            return Call.New(ExprFunction.New(Name, Args.Select((i, j) => Variable.New(j.ToString()))), Args); 
        }

        // object interface.
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
