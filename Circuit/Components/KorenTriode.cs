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
            Expression E1 = Ln1Exp(Kp * (1.0 / Mu + Vgk * (Kvb + Vpk ^ 2) ^ (-0.5))) * Vpk / Kp;

            Ip = (Call.Max(E1, 0) ^ Ex) / Kg;
            Ig = Call.Max(Vgk - Vg, 0) / Rgk;
        }

        // ln(1+e^x) = x for large x, and large x causes numerical issues.
        private static Expression Ln1Exp(Expression x)
        {
            return Call.If(x > 50, x, Call.Ln(1 + Call.Exp(x)));
        }
    }
}
