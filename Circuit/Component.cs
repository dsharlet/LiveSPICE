using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Util;

namespace Circuit
{
    /// <summary>
    /// Interface for components with grouping ability.
    /// </summary>
    public interface IGroupableComponent
    {
        string Group { get; }
    }

    /// <summary>
    /// Interface for components that expose a pot controlled value.
    /// </summary>
    public interface IPotControl: IGroupableComponent
    {
        /// <summary>
        /// The value of the pot.
        /// </summary>
        double PotValue { get; set; }
    }

    /// <summary>
    /// Interface for components that expose a button controlled value.
    /// </summary>
    public interface IButtonControl: IGroupableComponent
    {
        void Click();
        int Position { get; set; }
        int NumPositions { get; }
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
        /// Sampling period of the simulation.
        /// </summary>
        public static readonly Expression T = Variable.New("T");

        /// <summary>
        /// Thermal voltage. We assume ~room temperature.
        /// </summary>
        public static readonly Quantity VT = new Quantity(25.35e-3m, Units.V);

        private string name = "X1";
        [Serialize, Description("Unique name of this component.")]
        public virtual string Name { get { return name; } set { name = value; NotifyChanged(nameof(Name)); } }

        private string partNumber = "";
        [Serialize, DefaultValue(""), Description("Part name/number.")]
        public virtual string PartNumber { get { return partNumber; } set { partNumber = value; NotifyChanged(nameof(PartNumber)); } }

        private string description = "";
        [Serialize, DefaultValue(""), Description("Description of this component.")]
        public virtual string Description
        {
            get
            {
                if (!string.IsNullOrEmpty(description))
                    return description;
                else
                    return GetType().CustomAttribute<DescriptionAttribute>()?.Description;
            }
            set
            {
                description = value;
                NotifyChanged(nameof(Description));
            }
        }

        private object tag = null;
        [Browsable(false)]
        public virtual object Tag { get { return tag; } set { tag = value; NotifyChanged(nameof(Tag)); } }

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
        protected internal abstract void LayoutSymbol(SymbolLayout Sym);

        public SymbolLayout LayoutSymbol()
        {
            SymbolLayout layout = new SymbolLayout();
            LayoutSymbol(layout);
            return layout;
        }

        public virtual XElement Serialize()
        {
            XElement X = new XElement("Component");
            Type T = GetType();
            X.SetAttributeValue("_Type", T.AssemblyQualifiedName);
            foreach (PropertyInfo i in T.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(i => i.CustomAttribute<Serialize>() != null))
            {
                object value = i.GetValue(this, null);
                DefaultValueAttribute def = i.CustomAttribute<DefaultValueAttribute>();
                if (def == null || !Equals(def.Value, value))
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(i.PropertyType);
                    X.SetAttributeValue(i.Name, tc.ConvertToString(null, CultureInfo.InvariantCulture, value));
                }
            }
            return X;
        }

        protected virtual void DeserializeImpl(XElement X)
        {
            foreach (PropertyInfo i in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(i => i.CustomAttribute<Serialize>() != null))
            {
                XAttribute attr = X.Attribute(i.Name);
                if (attr != null)
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(i.PropertyType);
                    i.SetValue(this, tc.ConvertFromString(null, CultureInfo.InvariantCulture, attr.Value), null);
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
                DisplayNameAttribute attr = GetType().CustomAttribute<DisplayNameAttribute>(false);
                return attr != null ? attr.DisplayName : GetType().Name;
            }
        }

        // object interface.
        public override string ToString() { return TypeName + " " + Name; }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
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
            return
                x is Call d
                && d.Target is UnknownFunction
                && On.SequenceEqual(d.Arguments);
        }

        /// <summary>
        /// Similar to e^x - 1, but uses a linear extension of e^x for large x. Useful for p-n junction
        /// i-V relationships to avoid numerical problems for large x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression LinExpm1(Expression x)
        {
            const double LinExpKnee = 50.0;
            // TODO: Do a proper e^x - 1. Right now this still helps with stability, just
            // because the computer algebra simplifications don't cross Call.If, which is
            // lame.
            double expKnee = Math.Exp(LinExpKnee);
            double kneeIntercept = expKnee - expKnee * LinExpKnee - 1.0;
            return Call.If(x < LinExpKnee, Call.Exp(x) - 1, expKnee * x + kneeIntercept);
        }

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
