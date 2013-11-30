using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Base class for a triode.
    /// </summary>
    [Category("Vacuum Tubes")]
    [DisplayName("Triode (Koren)")]
    [Description("Triode implemented using Norman Koren's model.")]
    public class KorenTriode : Triode
    {
        private double mu = 100.0;
        private double ex = 1.4;
        private double kg = 1060.0;
        private double kp = 600.0;
        private double kvb = 300;
        private Quantity rgk = new Quantity(1e6, Units.Ohm);
        private Quantity vg = new Quantity(0.33, Units.V);

        [Serialize]
        public double Mu { get { return mu; } set { mu = value; NotifyChanged("Mu"); } }
        [Serialize]
        public double Ex { get { return ex; } set { ex = value; NotifyChanged("Ex"); } }
        [Serialize]
        public double Kg { get { return kg; } set { kg = value; NotifyChanged("Kg"); } }
        [Serialize]
        public double Kp { get { return kp; } set { kp = value; NotifyChanged("Kp"); } }
        [Serialize]
        public double Kvb { get { return kvb; } set { kvb = value; NotifyChanged("Kvb"); } }
        [Serialize]
        public Quantity Rgk { get { return rgk; } set { if (rgk.Set(value)) NotifyChanged("Rgk"); } }
        [Serialize]
        public Quantity Vg { get { return vg; } set { if (vg.Set(value)) NotifyChanged("Vg"); } }

        protected override void Analyze(Analysis Mna, Expression Vpk, Expression Vgk, out Expression Ip, out Expression Ig)
        {
            Expression ex = Kp * (1.0 / Mu + Vgk * (Kvb + Vpk * Vpk) ^ (-0.5));

            // ln(1+e^x) = x for large x, and large x causes numerical issues.
            Expression E1 = Call.If(ex > 5, ex, Call.Ln(1 + LinExp(ex))) * Vpk / Kp;

            Ip = Call.If(E1 > 0, (E1 ^ Ex) / Kg, 0);
            Ig = Call.If(Vgk > Vg, (Vgk - Vg) / Rgk, 0);
        }
    }
}
