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
    [DisplayName("Triode (Child-Langmuir)")]
    [Description("Triode implemented using the Child-Langmuir law.")]
    public class ChildLangmuirTriode : Triode
    {
        protected double mu, k;
        [Description("Voltage gain.")]
        [Serialize]
        public double Mu { get { return mu; } set { mu = value; NotifyChanged("Mu"); } }

        [Description("Generalized perveance.")]
        [Serialize]
        public double K { get { return k; } set { k = value; NotifyChanged("K"); } }

        protected override void Analyze(ModifiedNodalAnalysis Mna, Expression Vgk, Expression Vpk, out Expression Ip, out Expression Ig)
        {
            Expression Ed = Mu * Vgk + Vpk;
            Ip = Call.If(Ed > 0, K * (Ed ^ 1.5), 0);
            Ig = 0;
        }
    }
}
