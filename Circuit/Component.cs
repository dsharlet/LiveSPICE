using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Reflection;
using ComputerAlgebra;

namespace Circuit
{
    /// <summary>
    /// Interface for components that expose a pot controlled value.
    /// </summary>
    public interface IPotControl
    {
        /// <summary>
        /// The value of the pot.
        /// </summary>
        double PotValue { get; set; }
    }

    /// <summary>
    /// Indicates the property should be serialized.
    /// </summary>
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
        [Serialize, Description("Unique name of this component.")]
        public virtual string Name { get { return name; } set { name = value; NotifyChanged("Name"); } }

        private string partNumber = "";
        [Serialize, DefaultValue(""), Description("Part name/number.")]
        public virtual string PartNumber { get { return partNumber; } set { partNumber = value; NotifyChanged("PartNumber"); } }

        private object tag = null;
        [Browsable(false)]
        public object Tag { get { return tag; } set { tag = value; NotifyChanged("Tag"); } }
        
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
        public virtual void Analyze(Analysis Mna) { }
        
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
            foreach (PropertyInfo i in T.GetProperties().Where(i => i.GetCustomAttribute<Serialize>() != null))
            {
                object value = i.GetValue(this);
                DefaultValueAttribute def = i.GetCustomAttribute<DefaultValueAttribute>();
                if (def == null || !Equals(def.Value, value))
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(i.PropertyType);
                    X.SetAttributeValue(i.Name, tc.ConvertToString(value));
                }
            }
            return X;
        }

        protected virtual void DeserializeImpl(XElement X)
        {
            foreach (PropertyInfo i in GetType().GetProperties().Where(i => i.GetCustomAttribute<Serialize>() != null))
            {
                XAttribute attr = X.Attribute(i.Name);
                if (attr != null)
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(i.PropertyType);
                    i.SetValue(this, tc.ConvertFromString(attr.Value));
                }
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

        /// <summary>
        /// Create a deep copy of this component via serialization.
        /// </summary>
        /// <returns></returns>
        public virtual Component Clone() { return Deserialize(Serialize()); }

        /// <summary>
        /// The name of the type of this component.
        /// </summary>
        public virtual string TypeName
        {
            get
            {
                DisplayNameAttribute attr = GetType().GetCustomAttribute<DisplayNameAttribute>(false);
                return attr != null ? attr.DisplayName : GetType().Name;
            }
        }
                
        // object interface.
        public override string ToString() { return TypeName + " " + Name; }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        // This is too useful not to have.
        protected static Expression D(Expression f, Expression x) { return Call.D(f, x); }
        
        /// <summary>
        /// Make a dependent variable.
        /// </summary>
        /// <param name="Name">Name of the new dependent variable.</param>
        /// <param name="On">Dependent expressions.</param>
        /// <returns></returns>
        public static Expression DependentVariable(string Name, params Expression[] On)
        {
            return Call.New(Name, On);
        }
        /// <summary>
        /// Test if x is a dependent variable.
        /// </summary>
        /// <param name="x">Expression to test.</param>
        /// <param name="On">Dependent expressions.</param>
        /// <returns></returns>
        public static bool IsDependentVariable(Expression x, params Expression[] On)
        {
            Call d = x as Call;
            return
                !ReferenceEquals(d, null) &&
                d.Target is UnknownFunction &&
                On.SequenceEqual(d.Arguments);
        }

        private const double LinExpKnee = 50.0;
        /// <summary>
        /// Similar to e^x, but uses a linear extension of e^x for large x. Useful for p-n junction
        /// i-V relationships to avoid numerical problems for large x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression LinExp(Expression x) { return Call.If(x < LinExpKnee, Call.Exp(x), Math.Exp(LinExpKnee) * (1.0 + x - LinExpKnee)); }
        
        /// <summary>
        /// Find a unique name among a set of names.
        /// </summary>
        /// <param name="Components"></param>
        /// <returns></returns>
        public static string UniqueName(IEnumerable<string> Names, string Name)
        {
            if (!Names.Contains(Name))
                return Name;

            string prefix = new string(Name.TakeWhile(i => !Char.IsDigit(i)).ToArray());
            for (int i = 1; ; ++i)
            {
                string name = prefix + i;
                if (!Names.Contains(name))
                    return name;
            }
        }
    }
}
