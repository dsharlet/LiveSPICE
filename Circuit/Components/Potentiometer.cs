using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    public enum SweepType
    {
        Linear,
        Logarithmic,
        ReverseLogarithmic,
    }

    /// <summary>
    /// Resistor is a linear component with V = R*i.
    /// </summary>
    [Category("Standard")]
    [DisplayName("Potentiometer")]
    [DefaultProperty("Resistance")]
    [Description("Represents a potentiometer. When Wipe is 0, the wiper is at the cathode.")] 
    public class Potentiometer : Component, IControl
    {
        protected Terminal anode, cathode, wiper;
        [Browsable(false)]
        public Terminal Anode { get { return anode; } }
        [Browsable(false)]
        public Terminal Cathode { get { return cathode; } }
        [Browsable(false)]
        public Terminal Wiper { get { return wiper; } }
        public override IEnumerable<Terminal> Terminals 
        { 
            get 
            {
                yield return anode;
                yield return cathode;
                yield return wiper;
            } 
        }

        public Potentiometer()
        {
            anode = new Terminal(this, "Anode");
            cathode = new Terminal(this, "Cathode");
            wiper = new Terminal(this, "Wiper");
            Name = "R1"; 
        }

        protected Quantity resistance = new Quantity(100, Units.Ohm);
        [Serialize, Description("Resistance of this potentiometer.")]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged("Resistance"); } }

        protected double wipe = 0.5;
        [Serialize, Description("Position of the wiper, between 0 and 1.")]
        public double Wipe { get { return wipe; } set { wipe = value; NotifyChanged("Wipe"); } }

        //protected SweepType sweep = SweepType.Linear;
        //[Serialize, Description("Sweep mapping of the wiper.")]
        //public SweepType Sweep { get { return sweep; } set { sweep = value; NotifyChanged("Sweep"); } }

        // IControl implementation.
        [Browsable(false)]
        public double Value { get { return Wipe; } set { Wipe = value; } }

        public void ConnectTo(Node A, Node C, Node W)
        {
            Anode.ConnectTo(A);
            Cathode.ConnectTo(C);
            Wiper.ConnectTo(W);
        }

        public override void Analyze(Analysis Mna)
        {
            Expression P = Wipe;

            Expression R1 = resistance.Value * P;
            Expression R2 = resistance.Value * (1 - P);

            Resistor.Analyze(Mna, Cathode, Wiper, R1);
            Resistor.Analyze(Mna, Anode, Wiper, R2);
        }
        
        public override sealed void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.InBounds(new Coord(-20, -20), new Coord(10, 20));

            Sym.AddTerminal(Anode, new Coord(-10, 20), new Coord(-10, 16));
            Sym.DrawPositive(EdgeType.Black, new Coord(-16, 16));

            Sym.AddTerminal(Cathode, new Coord(-10, -20), new Coord(-10, -16));
            Sym.DrawNegative(EdgeType.Black, new Coord(-16, -16));
                        
            Sym.AddTerminal(Wiper, new Coord(10, 0));
            Sym.DrawArrow(EdgeType.Black, new Coord(10, 0), new Coord(-6, 0), 0.2);

            Resistor.Draw(Sym, -10, -16, 16, 7);

            Sym.DrawText(() => Resistance.ToString(), new Coord(-17, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(() => Wipe.ToString("G3"), new Coord(-4, 4), Alignment.Near, Alignment.Near);
            Sym.DrawText(() => Name, new Coord(-4, -4), Alignment.Near, Alignment.Far);
        }
    }
}
