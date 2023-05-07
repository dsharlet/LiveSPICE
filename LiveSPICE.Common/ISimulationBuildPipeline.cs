using System;
using System.Collections.Generic;
using Circuit;
using ComputerAlgebra;

namespace LiveSPICE.Common
{
    public interface ISimulationBuildPipeline
    {
        SimulationStatus Status { get; }

        IObservable<Simulation> Simulation { get; }

        SimulationSettings Settings { get; }

        void UpdateAnalysis(Analysis analysis);
        void UpdateInputs(IEnumerable<Expression> expressions);
        void UpdateOutputs(IEnumerable<Expression> expressions);
    }

    public interface ISimulationBuildPipeline<TSettings> : ISimulationBuildPipeline
        where TSettings : SimulationSettings
    {
        new TSettings Settings { get; }

        SimulationSettings ISimulationBuildPipeline.Settings => Settings;

        void UpdateSimulationSettings(Func<TSettings, TSettings> update);

    }
}