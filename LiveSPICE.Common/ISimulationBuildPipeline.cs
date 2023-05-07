using System;
using Circuit;

namespace LiveSPICE.Common
{
    public interface ISimulationBuildPipeline
    {
        SimulationStatus Status { get; }

        IObservable<Simulation> Simulation { get; }

        SimulationSettings Settings { get; }
    }

    public interface ISimulationBuildPipeline<TSettings> : ISimulationBuildPipeline
        where TSettings : SimulationSettings
    {
        new TSettings Settings { get; }

        SimulationSettings ISimulationBuildPipeline.Settings => Settings;

        void UpdateSimulationSettings(Func<TSettings, TSettings> update);

    }
}