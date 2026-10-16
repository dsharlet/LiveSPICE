About
-----

LiveSPICE is a circuit simulator that attempts to run in real time with minimal latency for audio signals.
It processes signals from audio input devices attached to your computer, and plays the results to the speakers.

For more information, see http://www.livespice.org.

Building
--------

The LiveSPICE solution requires the ComputerAlgebra project: https://github.com/dsharlet/ComputerAlgebra

To clone the LiveSPICE repo, run the following commands:

```bash
git clone --recursive https://github.com/dsharlet/LiveSPICE.git LiveSPICE
```

If you cloned without `--recursive`, initialize the submodules before building:

```bash
git submodule update --init --recursive
```

Headless Linux
--------------

The `LiveSPICE.Headless` project provides a cross-platform console host for the simulation core. It reuses the same input/output wiring logic as the VST path, while the WPF UI and VST projects remain Windows-only.

To build the headless runner:

```bash
dotnet build LiveSPICE.Headless/LiveSPICE.Headless.csproj
```

To run a simple Linux smoke test:

```bash
dotnet run --project LiveSPICE.Headless/LiveSPICE.Headless.csproj "Tests/Circuits/Passive 1stOrder Highpass RC.schx"
```

To process a real WAV input through an example pedal schematic:

```bash
dotnet run --project LiveSPICE.Headless/LiveSPICE.Headless.csproj "Tests/Examples/MXR Distortion +.schx" \
	--input-wav "/path/to/input.wav" \
	--output-wav "/path/to/output.wav"
```

The runner accepts optional overrides for `--input-wav`, `--output-wav`, `--sample-rate`, `--oversample`, `--iterations`, `--samples`, `--batch-size`, `--frequency`, and `--amplitude`.
