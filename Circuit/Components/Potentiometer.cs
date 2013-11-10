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
    [Category("Standard")]
    [DisplayName("Potentiometer")]
    [DefaultProperty("Resistance")]
    [Description("Represents a potentiometer. When Wipe is 0, the wiper is at the cathode.")] 
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

        protected Quantity resistance = new Quantity(100, Units.Ohm);
        [Description("Resistance of this potentiometer.")]
        [Serialize]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged("Resistance"); } }

        protected Expression wipe = 0.5m; // RangeParameter.New(Name, 0.5m, false);
        [Description("Position of the wiper, between 0 and 1.")]
        [Serialize]
        public Expression Wipe { get { return wipe; } set { wipe = value; NotifyChanged("Wipe"); } }
        
        public void ConnectTo(Node A, Node C, Node W)
        {
            Anode.ConnectTo(A);
            Cathode.ConnectTo(C);
            Wiper.ConnectTo(W);
        }

        public override void Analyze(ModifiedNodalAnalysis Mna)
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

            Sym.DrawText(() => resistance.ToString(), new Coord(-17, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(() => wipe.ToString(), new Coord(-4, 4), Alignment.Near, Alignment.Near);
            Sym.DrawText(() => Name, new Coord(-4, -4), Alignment.Near, Alignment.Far);
        }
    }
}
