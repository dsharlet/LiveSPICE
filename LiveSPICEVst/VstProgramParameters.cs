using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSPICEVst
{
    /// <summary>
    /// Class to hold VST parameters to allow the host to restore the plugin state
    /// </summary>
    public class VstProgramParameters
    {
        public string SchematicPath { get; set; }
        public int OverSample { get; set; }
        public int Iterations { get; set; }

        public VstProgramParameters()
        {
            OverSample = 2;
            Iterations = 8;
        }
    }
}
