About
-----

LiveSPICE is a circuit simulator that attempts to run in real time with minimal latency for audio signals.
It processes signals from audio input devices attached to your computer, and plays the results to the speakers.

For more information, see http://www.livespice.org.

Building
--------

The LiveSPICE solution requires the ComputerAlgebra project: https://github.com/dsharlet/ComputerAlgebra
The VST plugin depends on https://github.com/ValdemarOrn/SharpSoundDevice

To clone the LiveSPICE repo, run the following commands from the folder to contain the LiveSPICE solution:

```bash
git clone https://github.com/dsharlet/LiveSPICE.git .
git clone https://github.com/dsharlet/ComputerAlgebra.git ComputerAlgebra
```

To enable building the VST plugin:
```bash
git clone https://github.com/ValdemarOrn/SharpSoundDevice SharpSoundDevice
powershell -command "Expand-Archive -Force SharpSoundDevice\Builds\SharpSoundDevice-1.5.2.0-2019-08-11.zip SharpSoundDevice"
```