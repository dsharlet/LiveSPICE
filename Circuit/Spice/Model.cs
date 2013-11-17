using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Circuit.Spice
{
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

        public Model(Component Component, string Description) { component = Component; desc = Description; }
        public Model(Component Component) : this(Component, "") { }

        /// <summary>
        /// Mapping of SPICE model types to component templates.
        /// </summary>
        private static Dictionary<string, Component> ModelTemplates = new Dictionary<string, Component>()
        {
            { "D", new Diode() },
            { "NPN", new BipolarJunctionTransistor() { Type = BjtType.NPN } },
            { "PNP", new BipolarJunctionTransistor() { Type = BjtType.PNP } },
            { "NJF", new JunctionFieldEffectTransistor() { Type = JfetType.N } },
            { "PJF", new JunctionFieldEffectTransistor() { Type = JfetType.P } },
        };

        public static Model Parse(TokenList Tokens)
        {
            string name = Tokens[1];

            Component template = ModelTemplates[Tokens[2]];

            Component impl = template.Clone();
            impl.PartNumber = name;

            for (int i = 3; i < Tokens.Count; ++i)
            {
                PropertyInfo p = FindTemplateProperty(template, Tokens[i]);
                if (p != null)
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(p.PropertyType);
                    p.SetValue(impl, tc.ConvertFrom(ParseValue(Tokens[i + 1]).ToString()));
                }
            }

            return new Model(impl, "Imported from SPICE model '" + Tokens.Text + "'.");
        }

        private static PropertyInfo FindTemplateProperty(Component Template, string Name)
        {
            Name = Name.ToUpper();

            foreach (PropertyInfo i in Template.GetType().GetProperties())
                if (i.Name.ToUpper() == Name)
                    return i;
            return null;
        }
    }
}
