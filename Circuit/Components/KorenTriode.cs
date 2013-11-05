using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
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

        protected override void Analyze(ModifiedNodalAnalysis Mna, Expression Vpk, Expression Vgk, out Expression Ip, out Expression Ig)
        {
            Expression ex = Kp * (1.0 / Mu + Vgk * (Kvb + Vpk * Vpk) ^ (-0.5));

            // ln(1+e^x) = x for large x, and large x causes numerical issues.
            Expression E1 = Call.If(ex > 5, ex, Call.Ln(1 + Call.Exp(ex))) * Vpk / Kp;

            Ip = Call.If(E1 > 0, (E1 ^ Ex) / Kg, Constant.Zero);
            Ig = Call.If(Vgk > Vg, (Vgk - (Expression)Vg) / (Expression)Rgk, Constant.Zero);
        }

        public static IEnumerable<Component> Parts = new Component[]
        {
            new ModelSpecialization(new KorenTriode() { PartName = "12AX7", Mu = 100.0, Ex = 1.4,     Kg = 1060.0,    Kp = 600.0,  Kvb = 300.0, Rgk = 1e6m, Vg = 0.33m }) { DisplayName = "12AX7" },
	        new ModelSpecialization(new KorenTriode() { PartName = "12AZ7", Mu = 74.08, Ex = 1.371,   Kg = 382.0,     Kp = 190.11, Kvb = 300.0, Rgk = 1e6m, Vg = 0.33m }) { DisplayName = "12AZ7" },
	        new ModelSpecialization(new KorenTriode() { PartName = "12AT7", Mu = 67.49, Ex = 1.234,   Kg = 419.1,     Kp = 213.96, Kvb = 300.0, Rgk = 1e6m, Vg = 0.33m }) { DisplayName = "12AT7" },
	        new ModelSpecialization(new KorenTriode() { PartName = "12AY7", Mu = 44.16, Ex = 1.113,   Kg = 1192.4,    Kp = 409.96, Kvb = 300.0, Rgk = 1e6m, Vg = 0.33m }) { DisplayName = "12AY7" },
	        new ModelSpecialization(new KorenTriode() { PartName = "12AU7", Mu = 21.5,  Ex = 1.3,     Kg = 1180.0,    Kp = 84.0,   Kvb = 300.0, Rgk = 1e6m, Vg = 0.33m }) { DisplayName = "12AU7" },
        };

        private static KeyValuePair<string, object> KV(string Key, object Value) { return new KeyValuePair<string, object>(Key, Value); }
    }
}
