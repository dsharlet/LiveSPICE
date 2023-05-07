using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Circuit;
using CommunityToolkit.Mvvm.ComponentModel;
using ComputerAlgebra;
using Util;
using Util.Cancellation;
using static System.Reactive.Linq.Observable;


namespace LiveSPICE.Common
{
    public class SimulationBuildPipeline<TBuilder, TSettings> : ObservableObject, ISimulationBuildPipeline<TSettings>
        where TBuilder : ISimulationBuilder<TSettings>
        where TSettings : SimulationSettings
    {
        private BehaviorSubject<Expression[]> inputs = new(Array.Empty<Expression>());
        private BehaviorSubject<Expression[]> outputs = new(Array.Empty<Expression>());

        private BehaviorSubject<Analysis> analysis = new(null);

        private BehaviorSubject<int> sampleRate = new BehaviorSubject<int>(0);

        private readonly BehaviorSubject<TSettings> settings;
        public TSettings Settings
        {
            get => settings.Value;
            private set
            {
                settings.OnNext(value);
                OnPropertyChanged();
            }
        }

        private readonly TBuilder builder;
        private readonly ILog log;

        public IObservable<Simulation> Simulation { get; private set; }


        private BehaviorSubject<int> oversample = new(1);

        private SimulationStatus status;
        private object _lock = new();

        /// <summary>
        /// Simulation pipeline status.
        /// </summary>
        public SimulationStatus Status
        {
            get => status;
            private set => SetProperty(ref status, value);
        }

        public SimulationBuildPipeline(TBuilder builder, TSettings settings, ILog log)
        {
            this.settings = new(settings);
            this.builder = builder;
            this.log = log;

            CreatePipeline();
        }

        private void CreatePipeline()
        {
            // Rebuild solution every time analysis, sample rate or oversampling changes.
            var solution = Observable.CombineLatest(
                analysis,
                sampleRate.DistinctUntilChanged(),
                oversample.DistinctUntilChanged(),
                (analysis, sampleRate, oversample) => (analysis, sampleRate, oversample))
                    .Do(_ => Status = SimulationStatus.Solving)
                    .Select(ctx =>
                        FromAsync(token => Task.Run(() => TransientSolution.Solve(ctx.analysis, (Real)1 / ctx.sampleRate / ctx.oversample, log), token))
                        .Catch((Exception e) =>
                        {
                            Status = SimulationStatus.Error;
                            log.Exception(e);
                            return Empty<TransientSolution>();
                        }))
                    .Switch()
                    .Do(_ => Status = SimulationStatus.Ready);

            // compile
            Simulation = Observable.CombineLatest(
               inputs,
               outputs,
               settings,
               solution,
               (input, output, settings, solution) => (input, output, settings, solution))
                   .Where(_ => Status != SimulationStatus.Solving) //Rebuild only if not already solving.
                   .Do(_ => Status = SimulationStatus.Building)
                   .Select(ctx =>
                           RebuildSimulation(ctx.solution, ctx.input, ctx.output, ctx.settings)
                           .Catch((Exception e) =>
                           {
                               Status = SimulationStatus.Error;
                               log.Exception(e);
                               return Empty<Simulation>();
                           }))
                   .Switch()
                   .Do(_ => Status = SimulationStatus.Ready);
        }

        private IObservable<Simulation> RebuildSimulation(
            TransientSolution solution,
            Expression[] inputs,
            Expression[] outputs,
            TSettings settings)
        {
            return FromAsync(token =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        return builder.Build(
                            solution,
                            settings,
                            inputs,
                            outputs,
                            CancellationStrategy.FromToken(token));
                    }
                    catch (OperationCanceledException)
                    {
                        log.WriteLine(MessageType.Info, "Build cancelled");
                        throw;
                    }
                }, token);
            });
        }

        public void UpdateInputs(IEnumerable<Expression> expressions) => inputs.OnNext(expressions.ToArray());

        public void UpdateOutputs(IEnumerable<Expression> expressions) => outputs.OnNext(expressions.ToArray());

        public void UpdateAnalysis(Analysis analysis) => this.analysis.OnNext(analysis);

        public void UpdateSimulationSettings(Func<TSettings, TSettings> update)
        {
            lock (_lock)
            {
                var newSettings = update(Settings);

                oversample.OnNext(newSettings.Oversample);
                sampleRate.OnNext(newSettings.SampleRate);

                Settings = newSettings;
            }
        }
    }

    public static class SimulationBuildPipeline
    {
        public static ISimulationBuildPipeline<TSettings> Create<TBuilder, TSettings>(TBuilder builder, TSettings settings, ILog log)
            where TSettings : SimulationSettings
            where TBuilder : ISimulationBuilder<TSettings> => new SimulationBuildPipeline<TBuilder, TSettings>(builder, settings, log);
    }
}
