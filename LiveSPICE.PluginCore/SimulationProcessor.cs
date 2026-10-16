using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Circuit;
using Util;
using ButtonWrapper = LiveSPICE.PluginCore.ComponentWrapper<Circuit.IButtonControl>;

namespace LiveSPICE.PluginCore;

public class SimulationProcessor
{
    private double sampleRate;
    private int oversample = 2;
    private int iterations = 8;
    private Circuit.Circuit? circuit;
    private Simulation? simulation;
    private bool needUpdate;
    private bool needRebuild;
    private int updateSamplesElapsed;
    private int delayUpdateSamples;
    private Exception? simulationUpdateException;
    private int clock = -1;
    private int update;
    private readonly TaskScheduler scheduler = new RedundantTaskScheduler(1);
    private readonly object sync = new object();

    public SimulationProcessor()
    {
        InteractiveComponents = new ObservableCollection<IComponentWrapper>();
        SampleRate = 44100;
    }

    public ObservableCollection<IComponentWrapper> InteractiveComponents { get; }

    public Schematic? Schematic { get; private set; }

    public string SchematicPath { get; private set; } = string.Empty;

    public string SchematicName => System.IO.Path.GetFileNameWithoutExtension(SchematicPath);

    public double SampleRate
    {
        get => sampleRate;
        set
        {
            if (sampleRate == value)
                return;

            sampleRate = value;
            needRebuild = true;
            delayUpdateSamples = (int)(sampleRate * .1);
        }
    }

    public int Oversample
    {
        get => oversample;
        set
        {
            if (oversample == value)
                return;

            oversample = value;
            needRebuild = true;
        }
    }

    public int Iterations
    {
        get => iterations;
        set
        {
            if (iterations == value)
                return;

            iterations = value;
            needRebuild = true;
        }
    }

    public void LoadSchematic(string path)
    {
        Schematic newSchematic = Circuit.Schematic.Load(path);
        Circuit.Circuit newCircuit = newSchematic.Build();
        SetCircuit(newCircuit);
        Schematic = newSchematic;
        SchematicPath = path;
    }

    public void ClearSchematic()
    {
        Schematic = null;
        SchematicPath = string.Empty;
        circuit = null;
        InteractiveComponents.Clear();
    }

    public void RunSimulation(double[][] audioInputs, double[][] audioOutputs, int numSamples)
    {
        if (simulationUpdateException != null)
        {
            Exception toThrow = simulationUpdateException;
            simulationUpdateException = null;
            throw toThrow;
        }

        if (simulation == null && needRebuild && circuit != null)
        {
            UpdateSimulation(needRebuild);
            needRebuild = false;
        }

        if (circuit == null || simulation == null)
        {
            audioInputs[0].CopyTo(audioOutputs[0], 0);
            return;
        }

        lock (sync)
        {
            foreach (IComponentWrapper component in InteractiveComponents)
            {
                if (component.NeedUpdate)
                {
                    needUpdate = true;
                    component.NeedUpdate = false;
                    updateSamplesElapsed = 0;
                }

                if (component.NeedRebuild)
                {
                    needRebuild = true;
                    component.NeedRebuild = false;
                }
            }

            if (needUpdate || needRebuild)
            {
                if (needRebuild || updateSamplesElapsed > delayUpdateSamples)
                {
                    UpdateSimulation(needRebuild);
                    needRebuild = false;
                    needUpdate = false;
                }
                else
                {
                    updateSamplesElapsed += numSamples;
                }
            }

            simulation.Run(numSamples, audioInputs, audioOutputs);
        }
    }

    private void SetCircuit(Circuit.Circuit newCircuit)
    {
        circuit = newCircuit;
        InteractiveComponents.Clear();

        Dictionary<string, ButtonWrapper> buttonGroups = new Dictionary<string, ButtonWrapper>();
        Dictionary<string, PotWrapper> potGroups = new Dictionary<string, PotWrapper>();

        foreach (Circuit.Component component in circuit.Components)
        {
            if (component is IPotControl pot)
                AddPotControl(component, pot, potGroups);
            else if (component is IButtonControl button)
                AddButtonControl(component, button, buttonGroups);
        }

        needRebuild = true;
    }

    private void AddPotControl(Circuit.Component component, IPotControl pot, Dictionary<string, PotWrapper> potGroups)
    {
        if (string.IsNullOrEmpty(pot.Group))
        {
            InteractiveComponents.Add(new PotWrapper(pot, component.Name));
        }
        else if (potGroups.TryGetValue(pot.Group, out PotWrapper? wrapper))
        {
            wrapper.AddSection(pot);
        }
        else
        {
            wrapper = new PotWrapper(pot, pot.Group);
            potGroups.Add(pot.Group, wrapper);
            InteractiveComponents.Add(wrapper);
        }
    }

    private void AddButtonControl(Circuit.Component component, IButtonControl button, Dictionary<string, ButtonWrapper> buttonGroups)
    {
        ButtonWrapper wrapper;
        if (string.IsNullOrEmpty(button.Group))
        {
            wrapper = button.NumPositions == 2
                ? new DoubleThrowWrapper(button, component.Name)
                : new MultiThrowWrapper(button, component.Name);
            InteractiveComponents.Add(wrapper);
        }
        else if (buttonGroups.ContainsKey(button.Group))
        {
            wrapper = buttonGroups[button.Group];
            wrapper.AddSection(button);
        }
        else
        {
            wrapper = button.NumPositions == 2
                ? new DoubleThrowWrapper(button, button.Group)
                : new MultiThrowWrapper(button, button.Group);
            buttonGroups[button.Group] = wrapper;
            InteractiveComponents.Add(wrapper);
        }
    }

    private void UpdateSimulation(bool rebuild)
    {
        int id = Interlocked.Increment(ref update);
        new Task(() =>
        {
            try
            {
                if (circuit == null)
                    return;

                Simulation next = AudioSimulationFactory.Create(circuit, sampleRate, oversample, iterations);
                next.Compile();
                lock (sync)
                {
                    if (id <= clock)
                        return;

                    if (!rebuild && simulation != null)
                        next.CopyStateFrom(simulation);

                    simulation = next;
                    clock = id;
                }
            }
            catch (Exception ex)
            {
                simulationUpdateException = ex;
            }
        }).Start(scheduler);
    }
}
