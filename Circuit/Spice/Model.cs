using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Util;

namespace Circuit.Spice
{
    public class ParameterAlias : Attribute
    {
        private string alias;
        public string Alias { get { return alias; } }

        public ParameterAlias(string Alias) { alias = Alias.ToUpper(); }
    }

    /// <summary>
    /// Represents the .MODEL SPICE statement.
    /// </summary>
    public class Model : Statement
    {
        private Component component;
        /// <summary>
        /// Component representing the model.
        /// </summary>
        public Component Component { get { return component; } }

        private string desc;
        /// <summary>
        /// Log of information resulting from the import of this model.
        /// </summary>
        public string Description { get { return desc; } }

        public Model(Component Component, string Description) { component = new Specialization(Component); desc = Description; }
        public Model(Component Component) : this(Component, "") { }

        /// <summary>
        /// Mapping of SPICE model types to component templates.
        /// </summary>
        private static Dictionary<string, Component> ModelTemplates = new Dictionary<string, Component>()
        {
            ["D"] = new Diode(),
            ["NPN"] = new BipolarJunctionTransistor() { Type = BjtType.NPN },
            ["PNP"] = new BipolarJunctionTransistor() { Type = BjtType.PNP },
            ["NJF"] = new JunctionFieldEffectTransistor() { Type = JfetType.N },
            ["PJF"] = new JunctionFieldEffectTransistor() { Type = JfetType.P },
        };

        public static Model Parse(TokenList Tokens)
        {
            string name = Tokens[1];
            string type = Tokens[2];

            if (!ModelTemplates.TryGetValue(type, out Component template))
                throw new NotSupportedException("Model type '" + type + "' not supported.");

            Component impl = template.Clone();
            impl.PartNumber = name;

            for (int i = 3; i < Tokens.Count; ++i)
            {
                PropertyInfo p = FindTemplateProperty(template, Tokens[i]);
                if (p != null)
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(p.PropertyType);
                    p.SetValue(impl, tc.ConvertFrom(ParseValue(Tokens[i + 1]).ToString()), null);
                }
            }

            return new Model(impl, "Imported from SPICE model '" + Tokens.Text + "'.");
        }

        private static PropertyInfo FindTemplateProperty(Component Template, string Name)
        {
            Name = Name.ToUpper();

            foreach (PropertyInfo i in Template.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                // Check all the parameter aliases for this parameter.
                foreach (ParameterAlias j in i.CustomAttributes<ParameterAlias>())
                    if (Name == j.Alias)
                        return i;

                // Check the name itself.
                if (Name == i.Name.ToUpper())
                    return i;
            }
            return null;
        }
    }
}
