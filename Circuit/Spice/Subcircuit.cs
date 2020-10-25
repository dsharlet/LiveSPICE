using System.Collections.Generic;

namespace Circuit.Spice
{
    /// <summary>
    /// Represents the .SUBCKT SPICE statement.
    /// </summary>
    public class Subcircuit : Statement
    {
        private string name;
        /// <summary>
        /// Name of this subcircuit.
        /// </summary>
        public string Name { get { return name; } }

        private IEnumerable<string> ports;
        /// <summary>
        /// Nodes exposed from this subcircuit.
        /// </summary>
        public IEnumerable<string> Ports { get { return ports; } }

        private List<Statement> elements;
        /// <summary>
        /// Component representing the model.
        /// </summary>
        public List<Statement> Elements { get { return elements; } }

        public Subcircuit(string Name, IEnumerable<string> Ports)
        {
            name = Name;
            ports = Ports.Buffer();
            elements = new List<Statement>();
        }
    }
}
