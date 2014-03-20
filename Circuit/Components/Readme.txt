LiveSPICE Components Library
----------------------------

LiveSPICE attempts to load SPICE libraries from this folder. You can download 
SPICE component libraries from electronics manufacturers and other SPICE simulation 
packages. 

Warnings:
- Only a small subset of SPICE component models are currently supported. If you expect 
  a component to be visible but it is not, this is the most likely reason why.
- Of the SPICE models currently supported, not all model parameters are supported by 
  LiveSPICE. LiveSPICE was designed to ignore parameters likely to be insignificant for 
  circuits processing audio signals, but this may not always be true.