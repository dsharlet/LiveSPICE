using Circuit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Util;

namespace Tests
{
    internal class Docs
    {
        public static void WriteDocs()
        {
            FileStream file = new FileStream("docs.html", FileMode.Create);
            StreamWriter docs = new StreamWriter(file);

            void WriteTag(StreamWriter S, string Tab, string Tag, string P) => S.WriteLine(Tab + "<" + Tag + ">" + P + "</" + Tag + ">");

            docs.WriteLine("<section id=\"components\">");
            docs.WriteLine("\t<h3>Components</h3>");
            docs.WriteLine("\t<p>This section describes the operation of the basic component types provided in LiveSPICE. All of the components in the library are directly or indirectly (via subcircuits) implemented using these component types.</p>");

            Type root = typeof(Circuit.Component);
            foreach (Assembly i in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type j in i.GetTypes().Where(j => j.IsPublic && !j.IsAbstract && root.IsAssignableFrom(j)))
                {
                    try
                    {
                        System.ComponentModel.DisplayNameAttribute name = j.CustomAttribute<System.ComponentModel.DisplayNameAttribute>();
                        System.ComponentModel.DescriptionAttribute desc = j.CustomAttribute<System.ComponentModel.DescriptionAttribute>();

                        docs.WriteLine("\t<section id=\"" + j.Name + "\">");
                        docs.WriteLine("\t<h4>" + (name != null ? name.DisplayName : j.Name) + "</h4>");
                        if (desc != null)
                            WriteTag(docs, "\t\t", "p", desc.Description);

                        docs.WriteLine("\t\t<h5>Properties</h5>");
                        docs.WriteLine("\t\t<ul>");
                        foreach (PropertyInfo p in j.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(k => k.CustomAttribute<Serialize>() != null))
                        {
                            desc = p.CustomAttribute<System.ComponentModel.DescriptionAttribute>();
                            StringBuilder prop = new StringBuilder();
                            prop.Append("<span class=\"property\">" + p.Name + "</span>");
                            if (desc != null)
                                prop.Append(": " + desc.Description);

                            WriteTag(docs, "\t\t\t", "li", prop.ToString());
                        }
                        docs.WriteLine("\t\t</ul>");
                    }
                    catch (Exception) { }
                }
            }

            docs.WriteLine("</section> <!-- components -->");
        }
    }
}
