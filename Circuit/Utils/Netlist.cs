using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuit
{
    class Netlist
    {
        // Remove the first word from s and return it.
        private static string Word(ref string s)
        {
            char[] whitespace = new char[] { ' ', '\t' };
            int end = s.IndexOfAny(whitespace);
            string w = s.Substring(0, end);
            s = s.Substring(end).TrimStart();
            return w;
        }

        private static string[] Words(ref string s, int N)
        {
            string[] w = new string[N];
            for (int i = 0; i < N; ++i)
                w[i] = Word(ref s);
            return w;
        }

        private static Node[] Nodes(Circuit C, ref string s, int N)
        {
            Node[] nodes = new Node[N];
            for (int i = 0; i < N; ++i)
            {
                string name = Word(ref s);
                nodes[i] = C.Nodes.SingleOrDefault(j => j.Name == name);
                if (nodes[i] == null)
                {
                    nodes[i] = new Node() { Name = name };
                    C.Nodes.Add(nodes[i]);
                }
            }
            return nodes;
        }
              

        private static Dictionary<string, decimal> prefixes = new Dictionary<string, decimal>()
        {
            { "F", 10e-15m },
            { "P", 10e-12m },
            { "N", 10e-9m },
            { "U", 10e-6m },
            { "M", 10e-3m },
            { "K", 10e+3m },
            { "MEG", 10e+6m },
            { "G", 10e+9m },
            { "T", 10e+12m },
        };

        private static Quantity ParseValue(string Word)
        {
            string digits = new string(Word.TakeWhile(i => Char.IsDigit(i)).ToArray());
            decimal value = decimal.Parse(digits);
            Word = Word.Substring(digits.Length);

            foreach (KeyValuePair<string, decimal> i in prefixes)
            {
                if (Word.StartsWith(i.Key))
                    return new Quantity(value * i.Value, Units.None);
            }
            return new Quantity(value, Units.None);
        }

        private static Quantity ParseFunction(string Word)
        {
            return ParseValue(Word);
        }

        public static Circuit Parse(string Filename)
        {
            Circuit circuit = new Circuit();

            System.IO.StreamReader file = new System.IO.StreamReader(Filename);

            // First line is the title.
            circuit.Name = file.ReadLine();
            
            for (string line = file.ReadLine(); line != null; line = file.ReadLine())
            {
                // Skip comments.
                if (line.StartsWith("*")) continue;

                // We don't read any simulation commands.
                if (line.StartsWith(".")) continue;

                // Get the name of the element.
                string name = Word(ref line);

                Node[] nodes = null;
                Component C = null;
                if (name.StartsWith("R"))
                {
                    nodes = Nodes(circuit, ref line, 2);
                    C = new Resistor() { Resistance = ParseValue(Word(ref line)) };
                }
                else if (name.StartsWith("C"))
                {
                    nodes = Nodes(circuit, ref line, 2);
                    C = new Capacitor() { Capacitance = ParseValue(Word(ref line)) };
                }
                else if (name.StartsWith("L"))
                {
                    nodes = Nodes(circuit, ref line, 2);
                    C = new Inductor() { Inductance = ParseValue(Word(ref line)) };
                }
                else if (name.StartsWith("D"))
                {
                    nodes = Nodes(circuit, ref line, 2);
                    C = new Diode();
                }
                else if (name.StartsWith("I"))
                {
                    nodes = Nodes(circuit, ref line, 2);
                    C = new CurrentSource() { Current = ParseFunction(line) };
                }
                else if (name.StartsWith("V"))
                {
                    nodes = Nodes(circuit, ref line, 2);
                    C = new VoltageSource() { Voltage = ParseFunction(line) };
                }
                if (C == null)
                    throw new InvalidOperationException("Unknown SPICE netlist component '" + name + "'");

                // Connect nodes to terminals.
                foreach (var i in C.Terminals.Zip(nodes, (a, b) => new { a, b }))
                    i.a.ConnectTo(i.b);

                circuit.Components.Add(C);
            }
            return circuit;
        }
    }
}
