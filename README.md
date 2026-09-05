# Crazy Driver

A car drives itself forward down a desert road. The player aims a turret by dragging left and right;
the turret fires on its own. Enemies stand along the route, wake as the car approaches, run at it and
deal damage. Reach the end of the map to win, run out of hit points to lose.

Unity **6000.3.23f1**, URP, one scene, VContainer + UniTask.

## Running it

Open `Assets/Scenes/Game.unity` and press Play.

- **Tap / click** — start the run, and dismiss the win or lose screen.
- **Drag left / right** — aim the turret. The angle is measured from wherever the drag started.

`CrazyDriver/Run Simulation Smoke Test` plays whole levels headlessly and prints the outcomes.
`CrazyDriver/Rebuild Project` regenerates every prefab, both config assets and the scene from source.

> **The scene is generated, the UI is not.** `Rebuild Project` throws the scene away and rebuilds it,
> so anything hand-placed in `Game.unity` is lost. The UI is deliberately exempt: it lives in
> `Assets/Prefabs/UI/GameUI.prefab`, which the tool creates only if it is missing and otherwise
> re-instantiates untouched. Edit the UI in that prefab and rebuilds will keep your work. Delete the
> prefab to get the generated layout back.

## Architecture

The rule the project is built around: **Unity is the presentation layer.** Gameplay lives in plain C#
classes with no MonoBehaviour, no Transform and no Update. Two assemblies enforce it.

```
CrazyDriver.Core     pure simulation - no MonoBehaviours, no scene, no prefabs
CrazyDriver.Game     views, input, UI, DI wiring; references Core
CrazyDriver.Editor   asset and scene generation, headless test harness
```

`Core` never references `Game`, so the simulation cannot reach into the scene even by accident. The
payoff is concrete: `SimulationSmokeTest` plays a dozen full levels in milliseconds without entering
play mode, which is how the balance table below was produced.

### Flow of a frame

`GameStartupManager` is the single entry point. It is a VContainer `LifetimeScope` rather than a
separate manager next to one — the scope is already a MonoBehaviour with a container and a start-up
hook, so a second class duplicating that would be two entry points pretending to be one.

```
PointerInputService  (ITickable)      raw pointer -> taps and normalized drag offsets
GameLoop             (ITickable)      RunController.Tick -> projectiles -> road streaming
PresentationSystem   (ILateTickable)  copies model state onto transforms, camera last
```

Nothing else has an `Update`. Ordering is four lines of code instead of a script execution order
setting.

`RunController` owns the lifecycle — generate, wait for a tap, drive, resolve — and ticks every other
core system in a fixed order. States are `Ready → Playing → Won | Lost → Ready`.

### Positioning

Everything positional is expressed as a **distance in meters** and resolved through one abstraction:

```csharp
public interface IPathEvaluator
{
    PathSample Evaluate(float distance, float lateralOffset);
}
```

`PathSample` is a value type carrying a position and a rotation. It is deliberately not a `Transform`
— a Transform is a component bound to a live GameObject and cannot be produced for an arbitrary
distance, which is why the road, the car, the enemies and the bonuses all share this instead.

`StraightPath` implements the looped straight corridor: the centerline runs along +Z and only its
height varies. Treating distance as the *horizontal* coordinate rather than true arc length keeps the
class trivial and the car's fixed speed genuinely fixed; with gentle slopes the two differ by well
under a percent, so no arc-length lookup table is needed. A curved path would not have that luxury,
which is exactly why the abstraction exists.

`ActivePath` is a stable indirection to whichever evaluator the current run generated, so a restart
swaps the curve in one place instead of rebuilding the object graph.

`VisualPath` streams pooled road tiles and answers `GetPos(distance, lateralOffset)` by delegating to
the evaluator, so geometry and geometry-as-data can never drift apart.

## Player data

There is a real save, not just an in-run counter.

```
Core/Progression/PlayerProfile.cs          flat serializable record: coins, runs, wins, kills, best distance
Core/Progression/IProfileStorage.cs        the contract the core depends on
Core/Progression/PlayerProfileService.cs   owns the loaded profile and is the only thing that writes it
Game/Data/JsonProfileStorage.cs            JSON under Application.persistentDataPath
```

The core defines the contract and the Unity assembly supplies the implementation, so file paths and
JSON never reach the simulation and a test can substitute an in-memory store. Saving happens once,
when a run ends -- a run is the natural unit of progress, and writing a file mid-run on a phone is a
guaranteed frame spike. Writes go to a temporary file that is then moved into place, so a crash
mid-write leaves the previous save intact rather than a half-written one. The record carries a schema
version, and a file from a newer build is ignored rather than misread.

`Wallet` remains per-run and is still reset by `RunController.Prepare()`; the profile is what
survives. The smoke test performs a full round trip -- write, reload from disk with a second service,
compare -- because persistence bugs otherwise only surface on the player's second session.

## Feedback

`FeedbackSystem` turns simulation events into things the player can see, and nothing else subscribes
to it, so deleting it would change nothing about how the game plays.

- **Damage and coin popups** — pooled world-space TextMeshPro labels that rise on a decelerating
  curve, fade late so the number stays readable, and take a small random sideways offset so several
  hits in one spot do not stack into a smear.
- **Particle bursts** on every enemy death and bonus pickup, pooled and tinted per burst. The tint
  goes through a `MaterialPropertyBlock` rather than the particle system's start colour: mesh
  particles carry no colour vertex stream, so a start colour never reaches the shader and every
  burst came out white.
- **Tyre tracks** — a `TrailRenderer` per rear wheel. The car only ever moves forward along a known
  path, so a decal projector would buy nothing that a trail does not.
- **The laser beam** uses `Assets/Shaders/LaserBeam.shader`: an unlit additive pass with separate
  `_Intensity` and `_Alpha` knobs. Additive rather than alpha-blended because a laser adds light to
  what is behind it instead of tinting it, and a view-facing term gives the tube a bright core with
  soft edges instead of reading as a plastic rod. `LaserBeamView` writes both values through a
  `MaterialPropertyBlock`, so nothing clones the material at runtime, and plays the power-up flicker:
  pulses that dim towards the idle level, then an ease onto it. `LaserActivationSystem` is the seam
  that triggers it when a run starts -- the beam knows how to flicker, the run knows when.
- **Health and progress bars** fill by sliding a full-size visual under a mask
  (`HealthBar → Viewport (Mask) → Fill`), driven by `ProgressBarView`. `Image.fillAmount` squeezes
  the sprite as the value drops, distorting any gradient or bevel on it; a mask leaves the fill at
  its true size and simply reveals less of it, and lets the bar take a shape other than a rectangle.

Enemies are destroyed by their own impact and deal their damage exactly once. A sustained
damage-per-second instead left the survivor jogging alongside the car draining it, which reads as a
bug rather than as a hit.

## Decisions worth explaining

**Enemy activation is a distance comparison, not a trigger volume.** Each enemy is generated with a
distance along the path; `SpawnStream<T>` walks the distance-sorted array with a single head index.
Everything before it has fired, everything after is too far, and the per-frame cost is the number of
items actually activated rather than the size of the level.

**Enemies aim where the car *will* be.** The car is twice as fast as an enemy, so chasing its current
position is a tail chase that can never be won and the game would have no fail state. `interceptLead`
turns the same speed budget into an interception. The level generator refuses to place an enemy where
`EnemySettings.CanIntercept` says it could never engage, and `GameConstantsSO.OnValidate` warns if the
speeds are tuned into that corner.

**The turret's aim is stored against the path, not the car.** If the turret were simply a child of the
car, the car's sway would rotate the barrel while the player's finger was still. The angle is held
relative to the path and the view cancels the car's rotation out, so the crosshair stays where the
player put it. Drag is measured in fractions of screen width, not pixels, so sensitivity is identical
on every density.

**Projectiles are sphere casts, not colliders.** At 90 m/s a collider-based bullet passes clean
through a stickman between two fixed-update steps. Each shot sweeps the segment it covers that frame:
it cannot miss, costs one cast per live shot, and needs no Rigidbody. Hits still resolve *by collider*
as required — `DamageReceiver` is the single bridge from a collider back to an `IDamageable`.

**Sway and terrain height are sums of sine octaves evaluated against distance, not time.** A single
sine reads as mechanical and a per-frame random offset is not continuous. Because the input is
distance, the result is identical for a given seed regardless of frame rate or speed. The car also
yaws into its own drift — without that term it looks like it is sliding on ice.

**Road tiles are stretched onto their chord.** A tile is a rigid plane exactly one tile-length long,
but the chord it must cover is the hypotenuse of that length and the height change across it, so an
unscaled tile leaves a hairline gap at every boundary — small, but visible when it catches the light.
Scaling the mesh along its own forward axis by the chord ratio closes it exactly rather than hiding it
under an overlap fudge.

**Wavelengths are constrained by the road geometry.** `ground.fbx` is a rigid 75 m plane, and tiles are
laid on the chord between boundaries, so a height wave shorter than a tile leaves the car hovering
over the middle of every piece. `GameConstantsSO.GetRoadChordError` measures that deviation and
`GameStartupManager.OnValidate` warns when it exceeds 25 cm. This is the one cross-asset constraint in
the project and it is invisible in either asset's inspector alone, which is why the check lives where
both meet.

**Pooled agents return values instead of raising events.** A pooled object that owned events would
need every subscriber wired on rent and torn down on release, and one missed unsubscribe resurrects a
dead enemy's damage. `EnemyAgent.Tick` returns the damage it dealt; the director polls state.

**Damage is accumulated across the enemy loop and reported once.** Reporting it inside the loop let a
killing blow run the whole lose sequence — which clears the very list being iterated — while the loop
still held an index into it. The smoke test caught this.

**Generation is seeded end to end.** Map choice, enemy and bonus placement, sway phases and terrain
phases all derive from one seed, which is logged at the start of every run. Setting
`GameStartupManager._fixedSeed` to a non-zero value replays a run exactly.

## Balance

Produced by `CrazyDriver/Run Simulation Smoke Test`. *Marksmanship* is the fraction of enemies the
stand-in player destroys, substituting for aiming — the only part of the loop the harness cannot
exercise. It kills them the instant they wake, which is harsher than real play, so live play sits a
little easier than this table.

| Marksmanship | seed 12345 | seed 999 |
|---|---|---|
| 0 %  | Lost at 138 m | Lost at 139 m |
| 50 % | Lost at 240 m | Lost at 164 m |
| 70 % | Lost at 301 m | Lost at 332 m |
| 80 % | **Won**, 125 hp left | **Won**, 25 hp left |
| 100 % | **Won**, full hp | **Won**, full hp |

The same command also round-trips the player profile through disk and reports OK or FAILED.

The threshold sits between 70 % and 80 %: both outcomes are reachable and neither is degenerate.

Level is 500 m at 12 m/s — about 42 seconds — with 43 enemies and 17 bonuses. Every number lives in
`Assets/Data/GameConstants.asset` and `Assets/Data/Level_Desert.asset`.

## Assets

`ground.fbx`, `car.fbx`, `turret.fbx` and `stickman.fbx` are used as supplied, with `map.png` on the
world and `car.png` on the vehicle, as the brief requires. Prefabs are assembled by
`Assets/Scripts/Editor/PrefabBuilder.cs` rather than by hand, so the numbers that matter — where the
turret sits, how long a tile is, which layer a hit box is on — are written down and reviewable in a
diff instead of buried in a `.prefab`.

Projectiles, the gate and the bonus pickup are primitives: no models for them ship with the brief.

## Known gaps

- **The stickman has no rig**, so enemies are static meshes. `EnemyView` already writes an `IsRunning`
  parameter and the prefab carries an `Animator`; dropping in a Mixamo idle/run controller is the only
  step left.
- **The road is a straight corridor**, per the agreed scope. Curves are the reason `IPathEvaluator`
  exists — a spline implementation would need an arc-length lookup table and would slot in behind that
  interface without touching the car, the enemies or the streamer.
- **No audio.**
- **The bonus pickup and the gate are primitives** — no models for them ship with the brief.
