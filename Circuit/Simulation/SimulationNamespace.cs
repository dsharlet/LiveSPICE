using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerAlgebra;

namespace Circuit
{
    /// <summary>
    /// Namespace for simulation related symbols.
    /// </summary>
    public class SimulationNamespace : DynamicNamespace
    {
        public SimulationNamespace(Expression h)
        {
            // Define T = step size.
            Add("T", h);

            Variable t = Variable.New("t");

            // Define d[t] = delta function.
            Add(ExprFunction.New("d", Call.If((0 <= t) & (t < h), 1, 0), t));
            // Define u[t] = step function.
            Add(ExprFunction.New("u", Call.If(t >= 0, 1, 0), t));
        }
    }
}
