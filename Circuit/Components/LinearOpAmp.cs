using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Implements a linear model operational amplifier (op-amp). This model will not saturate.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Linear Op-Amp")]
    public class LinearOpAmp : OpAmp
    {
        protected Quantity rin = new Quantity(1e6m, Units.Ohm);
        [Description("Input resistance.")]
        [SchematicPersistent]
        public Quantity InputResistance { get { return rin; } set { if (rin.Set(value)) NotifyChanged("InputResistance"); } }

        protected Quantity rout = new Quantity(1e2m, Units.Ohm);
        [Description("Output resistance.")]
        [SchematicPersistent]
        public Quantity OutputResistance { get { return rout; } set { if (rout.Set(value)) NotifyChanged("OutputResistance"); } }

        protected decimal gain = 1e6m;
        [Description("Gain.")]
        [SchematicPersistent]
        public decimal Gain { get { return gain; } set { gain = value; NotifyChanged("Gain"); } }
        
        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // The input terminals are connected by a resistor Rin.
            Expression Vin = Positive.V - Negative.V;
            Expression iin = Mna.AddNewUnknown("in" + Name);

            Mna.AddEquation(Vin / (Expression)InputResistance, iin);
            Positive.i = iin;
            Negative.i = -iin;

            // Vo = (G*Vin - Out.V) / Rout
            Out.i = Mna.AddNewUnknown("out" + Name);

            Mna.AddEquation(Gain * Vin - Out.V, Out.i * (Expression)OutputResistance);
        }
    }
}
