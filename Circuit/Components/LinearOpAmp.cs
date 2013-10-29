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
        [Serialize]
        public Quantity InputResistance { get { return rin; } set { if (rin.Set(value)) NotifyChanged("InputResistance"); } }

        protected Quantity rout = new Quantity(1e2m, Units.Ohm);
        [Description("Output resistance.")]
        [Serialize]
        public Quantity OutputResistance { get { return rout; } set { if (rout.Set(value)) NotifyChanged("OutputResistance"); } }

        protected decimal gain = 1e6m;
        [Description("Gain.")]
        [Serialize]
        public decimal Gain { get { return gain; } set { gain = value; NotifyChanged("Gain"); } }
        
        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // The input terminals are connected by a resistor Rin.
            Resistor.Analyze(Mna, Positive, Negative, InputResistance);
            // Vo = (G*Vin - Out.V) / Rout
            Mna.AddTerminal(Out, (Gain * (Positive.V - Negative.V) - Out.V) / (Expression)OutputResistance);
        }
    }
}
