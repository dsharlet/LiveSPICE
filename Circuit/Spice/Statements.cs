using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SyMath;

namespace Circuit.Spice
{
    /// <summary>
    /// SPICE directives interop.
    /// </summary>
    public class Statements : IEnumerable<object>, IEnumerable
    {
        private string title = "";
        /// <summary>
        /// Title found in the statements file.
        /// </summary>
        public string Title { get { return title; } set { title = value; } }

        private ILog log = new TextLog();
        /// <summary>
        /// Messages resulting from parsing the statements in this list.
        /// </summary>
        public ILog Log { get { return log; } }

        // Parsed directive objects.
        private List<object> objects = new List<object>();

        // Circuits constructed.
        private Stack<Circuit> circuits = new Stack<Circuit>(new [] { new Circuit() });
                        
        /// <summary>
        /// Mapping of SPICE model types to component templates.
        /// Properties with a variable named 'x' will be replaced with that key from th emodel.
        /// </summary>
        private static Dictionary<string, Component> ModelTemplates = new Dictionary<string, Component>()
        {
            { "D", new ShockleyDiode() },
            { "NPN", new EbersMollBJT() { Structure = BJTStructure.NPN } },
            { "PNP", new EbersMollBJT() { Structure = BJTStructure.PNP } },
        };

        private static PropertyInfo FindTemplateProperty(Component Template, string Name)
        {
            Name = Name.ToUpper();

            foreach (PropertyInfo i in Template.GetType().GetProperties())
            {
                if (i.Name.ToUpper() == Name)
                    return i;
            }
            return null;
        }

        // Parse a model statement.
        private void ParseModel(TokenList Tokens)
        {
            string name = Tokens[1];

            Component template = null;
            foreach (string i in Tokens.Skip(2))
            {
                if (ModelTemplates.TryGetValue(i, out template))
                    break;
            }

            if (template == null)
            {
                Log.WriteLine(MessageType.Warning, "Warning: Supported model type not found.");
                return;
            }
            
            Component impl = template.Clone();
            impl.PartNumber = name;

            for (int i = 2; i < Tokens.Count; ++i)
            {
                PropertyInfo p = FindTemplateProperty(template, Tokens[i]);
                if (p != null)
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(p.PropertyType);
                    p.SetValue(impl, tc.ConvertFrom(ParseValue(Tokens[i + 1]).ToString()));
                }
            }

            objects.Add(new ModelSpecialization(impl)
            {
                DisplayName = name,
                Category = impl.GetCategory(),
                Description = "Imported from SPICE model '" + Tokens.Text + "'.",
            });
        }

        public void Parse(System.IO.StreamReader Stream)
        {
            Dictionary<string, Action<TokenList>> handlers = new Dictionary<string, Action<TokenList>>();

            handlers[".MODEL"] = ParseModel;

            title = Stream.ReadLine();
            int at = 1;

            while (!Stream.EndOfStream)
            {
                TokenList tokens = TokenList.ReadLine(Stream);
                at += tokens.LineCount;

                if (!tokens.Any())
                    continue;

                try
                {
                    if (tokens[0].StartsWith("."))
                    {
                        // Parse directive.
                        Action<TokenList> handler;
                        if (handlers.TryGetValue(tokens[0], out handler))
                            handler(tokens);
                        else
                            Log.WriteLine(MessageType.Warning, "Warning (line {1}): Ignored unknown spice directive '{0}'.", tokens[0], at);
                    }
                    else
                    {
                        // Parse element.
                        Log.WriteLine(MessageType.Warning, "Warning (line {1}): Ignored element '{0}'.", tokens[0], at);
                    }
                }
                catch (Exception Ex)
                {
                    Log.WriteLine(MessageType.Error, "Error (line {1}): {0}", Ex.Message, at);
                }
            }
        }

        public void Parse(string FileName)
        {
            Log.WriteLine(MessageType.Info, "Reading SPICE directives from '{0}'...", FileName);

            using (FileStream file = new FileStream(FileName, FileMode.Open))
            using (StreamReader reader = new StreamReader(file))
            {
                Parse(reader);
            }
            Log.WriteLine(MessageType.Info, "{0}: {1} directives", FileName, objects.Count);
        }

        public Statements() { }
        public Statements(string FileName) { Parse(FileName); }

        // IEnumerable<object>
        public IEnumerator<object> GetEnumerator() { return objects.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
        
        // Parsing quantities.
        private static Dictionary<string, double> Prefixes = new Dictionary<string, double>()
        {
            { "F", 1e-15 },
            { "P", 1e-12 },
            { "N", 1e-9 },
            { "U", 1e-6 },
            { "M", 1e-3 },
            { "K", 1e+3 },
            { "MEG", 1e+6 },
            { "G", 1e+9 },
            { "T", 1e+12 },
        };

        private static Regex Quantity = new Regex(@"([-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?)(F|P|N|U|M|K|MEG|G|T)?.*", RegexOptions.IgnoreCase);
        private static Expression ParseValue(string s)
        {
            Match m = Quantity.Match(s);
            if (m.Success)
            {
                double v = double.Parse(m.Groups[1].Value);
                double p = 1;
                if (m.Groups[3].Success)
                    p = Prefixes[m.Groups[3].Value.ToUpper()];
                return v * p;
            }
            else
            {
                throw new Exception("Unable to parse quantity '" + s + "'.");
            }
        }
    }
}
