using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Util;

namespace Circuit.Spice
{
    /// <summary>
    /// SPICE directives interop.
    /// </summary>
    public class Statements : IEnumerable<Statement>, IEnumerable
    {
        private string title = "";
        /// <summary>
        /// Title found in the statements file.
        /// </summary>
        public string Title { get { return title; } set { title = value; } }

        private ILog log = new StringLog();
        /// <summary>
        /// Messages resulting from parsing the statements in this list.
        /// </summary>
        public ILog Log { get { return log; } set { log = value; } }

        // Parsed statements.
        private List<Statement> statements = new List<Statement>();

        // Circuits constructed.
        //private Stack<Subcircuit> subcircuits = new Stack<Subcircuit>();

        public void Parse(System.IO.StreamReader Stream)
        {
            Dictionary<string, Action<TokenList>> handlers = new Dictionary<string, Action<TokenList>>
            {
                [".MODEL"] = x => statements.Add(Model.Parse(x)),
                //[".SUBCKT"] = x => subcircuits.Push(new Subcircuit(x[1], x.Skip(2))),
                //[".ENDS"] = x => statements.Add(subcircuits.Pop()),
            };

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
                        if (handlers.TryGetValue(tokens[0], out Action<TokenList> handler))
                            handler(tokens);
                        else
                            Log.WriteLine(MessageType.Warning, "Warning (line {1}): Ignored unknown spice directive '{0}'.", tokens[0], at);
                    }
                    else
                    {
                        //Element element = Element.Parse(tokens);
                        //if (subcircuits.Any())
                        //    subcircuits.Peek().Elements.Add(element);
                        //else
                        //    statements.Add(element);
                    }
                }
                catch (Exception Ex)
                {
                    Log.WriteLine(MessageType.Error, "Error (line {1}): {0}", Ex.Message, at);
                }
            }

            // Build circuits from statements.

        }

        public void Parse(string FileName)
        {
            Log.WriteLine(MessageType.Info, "Reading SPICE directives from '{0}'...", FileName);

            using (FileStream file = new FileStream(FileName, FileMode.Open))
            using (StreamReader reader = new StreamReader(file))
            {
                Parse(reader);
            }
            Log.WriteLine(MessageType.Info, "{0}: {1} directives", FileName, statements.Count);
        }

        public Statements() { }
        public Statements(string FileName) { Parse(FileName); }

        // IEnumerable<object>
        public IEnumerator<Statement> GetEnumerator() { return statements.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
