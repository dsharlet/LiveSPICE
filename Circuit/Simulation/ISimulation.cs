using System.Collections.Generic;

namespace Circuit
{
    public interface ISimulation
    {
        public void Run(double[] Input, IEnumerable<double[]> Output);
    }
}