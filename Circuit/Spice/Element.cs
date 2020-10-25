using System;
using System.Collections.Generic;
using System.Linq;

namespace Circuit.Spice
{
    public enum ElementType
    {
        Capacitor,
        Diode,
        VoltageControlledVoltageSource,
        CurrentControlledCurrentSource,
        VoltageControlledCurrentSource,
        CurrentControlledVoltageSource,
        CurrentSource,
        JunctionFieldEffectTransistor,
        Inductor,
        Port,
        Resistor,
        VoltageSource,
    }

    /// <summary>
    /// Represents an element statement.
    /// </summary>
    public class Element : Statement
    {
        private ElementType type;
        /// <summary>
        /// Type of this element.
        /// </summary>
        public ElementType Type { get { return type; } }

        private string name;
        /// <summary>
        /// Name of this subcircuit.
        /// </summary>
        public string Name { get { return name; } }

        private IEnumerable<string> parameters;
        /// <summary>
        /// Nodes exposed from this subcircuit.
        /// </summary>
        public IEnumerable<string> Parameters { get { return parameters; } }

        public Element(ElementType Type, string Name, IEnumerable<string> Parameters)
        {
            type = Type;
            name = Name;
            parameters = Parameters.Buffer();
        }

        public static Element Parse(TokenList Tokens)
        {
            // Parse element type.
            ElementType type;
            switch (Tokens[0][0])
            {
                //case "B": // type = ElementType.GaAsMESFET; break;
                case 'C': type = ElementType.Capacitor; break;
                case 'D': type = ElementType.Diode; break;
                case 'E': type = ElementType.VoltageControlledVoltageSource; break;
                case 'F': type = ElementType.CurrentControlledCurrentSource; break;
                case 'G': type = ElementType.VoltageControlledCurrentSource; break;
                case 'H': type = ElementType.CurrentControlledVoltageSource; break;
                case 'I': type = ElementType.CurrentSource; break;
                case 'J': type = ElementType.JunctionFieldEffectTransistor; break;
                //case 'K': type = ElementType.MutualInductor; break;
                case 'L': type = ElementType.Inductor; break;
                //case 'M': type = ElementType.MOSFET; break;
                //case 'N': type = ElementType.DigitalInputInterface; break;
                //case 'O': type = ElementType.DigitalOutputInterface; break;
                case 'P': type = ElementType.Port; break;
                case 'R': type = ElementType.Resistor; break;
                //case 'S': type = ElementType.VoltageControlledSwitch; break;
                //case 'T': type = ElementType.TransmissionLine; break;
                //case 'U': type = ; break;
                case 'V': type = ElementType.VoltageSource; break;
                default: throw new NotSupportedException("Unsupported SPICE element '" + Tokens[0][0] + "'.");
            }

            return new Element(type, Tokens[0], Tokens.Skip(1));
        }
    }
}
