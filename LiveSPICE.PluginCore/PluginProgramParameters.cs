using System.Collections.Generic;
using System.Linq;

namespace LiveSPICE.PluginCore;

public class PluginProgramParameters
{
    public string? SchematicPath { get; set; }

    public int OverSample { get; set; } = 2;

    public int Iterations { get; set; } = 8;

    public List<PluginProgramControlParameter> ControlParameters { get; set; } = new List<PluginProgramControlParameter>();

    public static PluginProgramParameters FromProcessor(SimulationProcessor processor)
    {
        PluginProgramParameters parameters = new PluginProgramParameters
        {
            SchematicPath = processor.SchematicPath,
            OverSample = processor.Oversample,
            Iterations = processor.Iterations,
        };

        foreach (IComponentWrapper wrapper in processor.InteractiveComponents)
        {
            switch (wrapper)
            {
                case PotWrapper potWrapper:
                    parameters.ControlParameters.Add(new PluginProgramControlParameter { Name = wrapper.Name, Value = potWrapper.PotValue });
                    break;
                case DoubleThrowWrapper doubleThrowWrapper:
                    parameters.ControlParameters.Add(new PluginProgramControlParameter { Name = wrapper.Name, Value = doubleThrowWrapper.Engaged ? 1 : 0 });
                    break;
                case MultiThrowWrapper multiThrowWrapper:
                    parameters.ControlParameters.Add(new PluginProgramControlParameter { Name = wrapper.Name, Value = multiThrowWrapper.Position });
                    break;
            }
        }

        return parameters;
    }

    public void ApplyTo(SimulationProcessor processor)
    {
        processor.Oversample = OverSample;
        processor.Iterations = Iterations;

        foreach (PluginProgramControlParameter controlParameter in ControlParameters)
        {
            IComponentWrapper? wrapper = processor.InteractiveComponents.SingleOrDefault(i => i.Name == controlParameter.Name);
            if (wrapper == null)
                continue;

            switch (wrapper)
            {
                case PotWrapper potWrapper:
                    potWrapper.PotValue = controlParameter.Value;
                    break;
                case DoubleThrowWrapper doubleThrowWrapper:
                    doubleThrowWrapper.Engaged = controlParameter.Value == 1;
                    break;
                case MultiThrowWrapper multiThrowWrapper:
                    multiThrowWrapper.Position = (int)controlParameter.Value;
                    break;
            }
        }
    }
}

public class PluginProgramControlParameter
{
    public string? Name { get; set; }

    public double Value { get; set; }
}
