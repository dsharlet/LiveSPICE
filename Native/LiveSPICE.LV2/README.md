# LiveSPICE Native LV2

This is the Linux-native plugin target for hosts such as Carla, Ardour, and REAPER that support LV2.

The bundle currently contains two native LV2 plugins:

- `LiveSPICE Generic`: a generic LiveSPICE LV2 shell with mono input/output, `schematicPath` state support, and a GTK3 `Load Schematic` UI styled after the Windows VST metal panel. Loading a `.schx` updates the UI with discovered potentiometer and switch controls, and `Clear` resets the loaded schematic and control panel. It currently passes audio through until the managed LiveSPICE simulation engine is bridged into the native LV2 runtime.
- `LiveSPICE MXR Phase 90`: a mono MXR Phase 90-style phaser with `Speed` and `Trimmer` controls.

Both build to real Linux ELF shared objects, unlike the AudioPlugSharp `.vst3` bridge that is Windows-only.

It also exposes an LV2 state interface with a `https://livespice.org/ns/plugin#schematicPath` property. That is the Linux-native state hook needed for the generic LiveSPICE model where hosts save/restore the selected `.schx` path, matching the Windows plugin's program-state design. The current DSP is still the built-in phaser; wiring arbitrary `.schx` simulation into the native plugin is the next engine bridge step.

## Build

```bash
make -C Native/LiveSPICE.LV2 clean all
```

## Test Discovery

```bash
make -C Native/LiveSPICE.LV2 test
make -C Native/LiveSPICE.LV2 ui-smoke
```

Expected URIs:

```text
https://livespice.org/plugins/generic
https://livespice.org/plugins/mxr-phase90
```

## Install

```bash
make -C Native/LiveSPICE.LV2 install
```

This copies the bundle to:

```text
~/.lv2/LiveSPICE.lv2
```

## Test With Carla

```bash
carla-single lv2 https://livespice.org/plugins/mxr-phase90
carla-single lv2 https://livespice.org/plugins/generic
```

If the plugin loads successfully, Carla opens/runs the plugin host instead of immediately failing with a plugin description error.

The `ui-smoke` target renders the generic GTK UI offscreen with the MXR Phase 90 example and writes `out/lv2-generic-ui-smoke.png` for layout review.
