using System.Collections.Generic;

namespace LiveSPICEVst
{
    /// <summary>
    /// Class to hold VST parameters to allow the host to restore the plugin state
    /// </summary>
    public class VstProgramParameters
    {
        public string SchematicPath { get; set; }
        public List<VSTProgramControlParameter> ControlParameters { get; set; }

        // Deprecated, but kept to avoid breaking serialization of state...?
        public int OverSample { get; set; }
        public int Iterations { get; set; }

        public VstProgramParameters()
        {
            ControlParameters = new List<VSTProgramControlParameter>();
        }
    }

    /// <summary>
    /// Name/Value pair for storing schematic control state
    /// </summary>
    public class VSTProgramControlParameter
    {
        public string Name { get; set; }
        public double Value { get; set; }
    }
}
