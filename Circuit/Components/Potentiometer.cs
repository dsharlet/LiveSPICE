using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Resistor is a linear component with V = R*i.
    /// </summary>
    [CategoryAttribute("Controls")]
    [DisplayName("Potentiometer")]
    public class Potentiometer : Component
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
        public Potentiometer(decimal R) : this() { Resistance.Value = Constant.New(R); }

        protected Quantity resistance = new Quantity(100, Units.Ohm);
        [Description("Total resistance of this potentiometer.")]
        [SchematicPersistent]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged("Resistance"); } }

        protected double wipe = 0.5;
        [Description("Position of the wiper as a ratio from 0 to 1. 1 corresponds to all the resistance between the wiper and the cathode.")]
        [SchematicPersistent]
        [RangedSimulationParameter(0.0, 1.0)]
        public double Wipe { get { return wipe; } set { wipe = Math.Max(Math.Min(value, 1.0), 0.0); NotifyChanged("Wipe"); } }


        public void ConnectTo(Node A, Node C, Node W)
        {
            Anode.ConnectTo(A);
            Cathode.ConnectTo(C);
            Wiper.ConnectTo(W);
        }

        protected override void Analyze(IList<Equal> Kcl)
        {
            Expression R1 = resistance.Value * (1.0 - wipe);
            Expression R2 = resistance.Value * wipe;

            Expression VR1 = Anode.V - Wiper.V;
            Expression VR2 = Wiper.V - Cathode.V;

            Cathode.i = VR1 / R1;
            Anode.i = VR2 / -R2;
            Wiper.i = Cathode.i - Anode.i;
        }
        
        public override sealed void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Anode, new Coord(-10, 20));
            Sym.AddTerminal(Cathode, new Coord(-10, -20));
            Sym.AddTerminal(Wiper, new Coord(10, 0));

            Sym.InBounds(new Coord(-20, -20), new Coord(10, 20));
            
            Sym.AddWire(Anode, new Coord(-10, 16));
            Sym.AddWire(Cathode, new Coord(-10, -16));
            Sym.DrawArrow(ShapeType.Black, new Coord(10, 0), new Coord(-6, 0), 0.2);
            Sym.DrawPositive(ShapeType.Black, new Coord(-6, 16));
            Sym.DrawNegative(ShapeType.Black, new Coord(-6, -16));

            int N = 7;
            Sym.DrawFunction(
                ShapeType.Black,
                (t) => Math.Abs((t + 0.5) % 2 - 1) * 8 - 4 - 10,
                (t) => t * 32 / N - 16,
                0, N, N * 2);

            Sym.DrawText(resistance.ToString(), new Coord(-17, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(wipe.ToString("G2"), new Coord(-4, 4), Alignment.Near, Alignment.Near);
            Sym.DrawText(Name, new Coord(-4, -4), Alignment.Near, Alignment.Far);
        }

        public override string ToString() { return Name + " = " + resistance.ToString(); }
    }
}
