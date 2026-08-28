# PathOptimiser

A Tecnomatix Process Simulate plugin that tunes robot path parameters to remove collisions and near misses while giving up as little cycle time as possible.

Anyone reading this will be unlikely to rebuild this environment themselves. This is mostly here for my own reference

Loads as a .NET command accessible through the UI

## Outline -

Given a robot path, the optimiser:

- Plays the operation through the simulation player and records a 'collision envelope' for every point where clearance drops below the configured near-miss distance.
- Generates candidate adjustments to the CNT (smoothing) and max speed of each active via.
- Re-runs the simulation for each candidate and scores the result by collision penalty against time cost.
- Applies the best adjustment and repeats until no collisions remain, or no adjustment improves the score.

If the first pass finds nothing better, a second pass is tried with double-length steps. When every candidate makes things worse (a local maximum) the least-bad option is applied, hoping to escape the local maximum.

Existing near misses can be recorded as *accepted clearances* so the optimiser doesn't chase geometry that was already tight before it started.

## Requirements -

- Tecnomatix Process Simulate 16.1.1 (Tecnomatix.Engineering, Tecnomatix.Olp, Tecnomatix.Ui)
- .NET Framework 4.7.2
- `SMC_Form_Library` (Private in-house SMC Design library)

## Build -

Open `PathOptimiser.sln` in Visual Studio and build. Output is a .dll (class library). Assembly is dropped into eMPower's `DotNetCommands` folder for Process Simulate to register it.

## Layout -

```
OptimisationSystem/
  Models/            Envelopes, adjustments, solver parameters
  Services/
    EnvelopeRecorders/   Simulation playback and clearance recording
    PathSolver/          Adjustment search and application
    Util/                Pre-run checks, playback helpers
DisplayGrid/         WPF operation and envelope views
```

The UI runs on its own STA thread with its own dispatcher, calls into the Tecnomatix API are marshalled back to the Process Simulate thread via `MyDispatchers`.

## Notes -

- The loaded cell (document) must be free of collisions before a run starts — `ReadyToRunChecks` will block otherwise.
- Weld locations are excluded from adjustment.
