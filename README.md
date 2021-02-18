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
git clone https://github.com/dsharlet/LiveSPICE.git LiveSPICE
git clone https://github.com/dsharlet/ComputerAlgebra.git LiveSPICE\ComputerAlgebra
```

The VST plugin depends on https://github.com/mikeoliphant/AudioPlugSharp

To enable building the VST plugin, do the following from a shell in the LiveSPICE root folder:
```bash
curl https://github.com/mikeoliphant/AudioPlugSharp/releases/download/v0.2/AudioPlugSharp.zip -O AudioPlugSharp.zip
powershell -command "Expand-Archive -Force AudioPlugSharp.zip"
```
