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
using LiveSPICE.Utils;
using Util;
using Util.Cancellation;
using static System.Reactive.Linq.Observable;


namespace LiveSPICE
{
    public class SimulationPipeline : ObservableObject
    {
        private BehaviorSubject<Expression[]> inputs = new(Array.Empty<Expression>());
        private BehaviorSubject<Expression[]> outputs = new(Array.Empty<Expression>());

        private BehaviorSubject<Analysis> analysis = new(null);

        private BehaviorSubject<int> sampleRate = new BehaviorSubject<int>(0);

        private readonly ILog log;

        public IObservable<Simulation> Simulation { get; private set; }


        private BehaviorSubject<int> oversample;

        /// <summary>
        /// Simulation oversampling rate.
        /// </summary>
        public int Oversample
        {
            get { return oversample.Value; }
            set { oversample.OnNext(Math.Max(1, value)); OnPropertyChanged(); }
        }

        private BehaviorSubject<int> iterations = new(8);

        /// <summary>
        /// Max iterations for numerical algorithms.
        /// </summary>
        public int Iterations
        {
            get { return iterations.Value; }
            set { iterations.OnNext(Math.Max(1, value)); OnPropertyChanged(); }
        }

        private SimulationStatus status;

        /// <summary>
        /// Simulation pipeline status.
        /// </summary>
        public SimulationStatus Status
        {
            get => status;
            private set => SetProperty(ref status, value);
        }

        public SimulationPipeline(int oversample, ILog log)
        {
            this.oversample = new(oversample);
            this.log = log;

            CreatePipeline();
        }

        private void CreatePipeline()
        {
            // Rebuild solution every time analysis, sample rate or oversampling changes.
            var solution = Observable.CombineLatest(
                analysis,
                sampleRate,
                oversample,
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
                    .Switch();

            // compile
            Simulation = Observable.CombineLatest(
               inputs,
               outputs,
               iterations,
               solution,
               (input, output, iterations, solution) => (input, output, iterations, solution))
                   .WithLatestFrom(sampleRate, (x, sampleRate) => (x.input, x.output, x.iterations, x.solution, sampleRate))
                   .Do(_ => Status = SimulationStatus.Building)
                   .Select(ctx =>
                           RebuildSimulation(ctx.solution, ctx.input, ctx.output, ctx.sampleRate, ctx.iterations)
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
            int sampleRate,
            int iterations)
        {
            return FromAsync(token =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var simulationTimeStep = (Real)1 / sampleRate;

                        var oversample = simulationTimeStep / solution.TimeStep;

                        var builder = new NewtonSimulationBuilder(log);

                        var settings = new NewtonSimulationSettings(sampleRate, (int)oversample, iterations, true);

                        var simulation = builder.Build(
                            solution,
                            settings,
                            inputs,
                            outputs,
                            CancellationStrategy.FromToken(token));

                        return simulation;

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

        public void UpdateSampleRate(int sampleRate) => this.sampleRate.OnNext(sampleRate);

    }
}
