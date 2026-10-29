# Stage 5: orders and AI boundary

## Ownership and contract

`Assets/Game.Core/ShipOrders.cs` is the sole owner of mode, route, target ID,
speed limit, preferred distance, orientation override, braking/hold point,
Flank phase and navigation error. `Waypoints` returns a detached read-only
snapshot. Commands are MoveTo, Tactical, Stop, SetSpeedLimit, SetOrientation;
the adapter submits explicit waypoint corrections and navigation failure.
Global Unity enums retain their original numeric/serialized representation.
The Unity facade maps them to Core enums; it has no second queue or mode.
Weapon selection and lock remain independent of the order target.

Coordinates reuse stage-4 AimPoint: XY floats, counterclockwise degree rotation.
No Unity types, scene queries, time API or persistence in Core.

Target IDs are GUIDs associated with individual ShipHealth managed instances
through ConditionalWeakTable. A facade resolves the currently registered ID
through WeakReference and checks Unity lifetime, enabled state and destruction.
A recreated component cannot inherit an old object's ID, even if Unity reuses an
instance ID. IDs are runtime identities, not saved hull-definition IDs. Loss of
availability brakes on the next evaluation; re-enabling does not resurrect orders.

`PilotDecisions` owns the four personality decisions: Brawler Orbit/optimalDistance,
Sniper KeepDistance/range*.85, Flanker Flank/range*.5, Cautious Orbit/optimalDistance
unless hull strictly below 35%, then KeepDistance/range*1.4. Missing health does
not trigger retreat. Observations are supplied by EnemyAI, not read from scene.
PilotDecision.Apply uses the same ShipOrders owner and does not reissue an unchanged
mode/target; preferred distance still refreshes without resetting navigation.

## Fixed-step order

ShipMovement.FixedUpdate retains its preparation gate and invokes EnemyAI once.
EnemyAI gathers target/health/weapon observations, obtains and applies a Core
decision; weapon lock maintenance stays Unity-side. Then the facade advances at
most one queued leg, projects the current point using Unity physics, gathers
order observations, evaluates Core once and synchronizes navigator revision.
The navigator performs casts and returns steering/detour/failure. Failure is
submitted explicitly to Core; detours require final-leg braking as before.
ShipMovement applies its unchanged forces/torque, and LateUpdate draws snapshots.
No Update-driven Core tick or second timeScale multiplication was introduced.
Pause therefore keeps fixed-step semantics; preparation still accepts commands
but does not execute movement/AI. Engine-disabled behavior remains in Unity.

## Physics boundary and preserved behavior

ShipLocalNavigator remains the owner of obstacle reference, side, progress,
8-second stall clock, 32-second detour clock and three retries. Core owns none of
those counters. Radius, CircleCast/OverlapCircle, moving-asteroid observation and
AccessiblePoint projection remain Unity code. This is NOT independent physical
avoidance and NOT pathfinding. Failure retains failed and subsequent waypoints.
No ShipMovement force, mass, braking, boost, speed or turn formula changed.

Arrival thresholds remain .65 intermediate, .18 final position/.12 velocity;
Stop follows position until speed < .08, then holds. Flank hysteresis is strict
<.65 and >1.4; Orbit still rotates radial direction by 20 degrees. Speed clamps
to .25..1. Range fallback without a weapon remains 4 units.
Existing quirks retained, not silently fixed: same explicit Tactical command resets
navigation even for unchanged target/mode; commands reset orientation overrides;
navigation failure retains the previous Destination until braking updates it;
local projection uses conservative bounds rather than a closest polygon point.

## Verification and isolation

Baseline main/cbb4478 and all stage-4 local work preserved. No commit/push/checkout.
Editor was already stopped at first MCP check; no runtime objects were saved.
Pre-extraction characterization job 8cf5d8c809d5409dac5b42c1dae13c28 passed 2/2
(a third supplied name did not exist and is not counted).
Core Release build: 0 warnings/errors. Full dotnet: 86/86, stage5.trx.
Full EditMode: 61/61, 3494931a076c491390ad604f0cbf3b33.
Initial full PlayMode: 22/22, af5216acbe1f4cfeaa2a70e05585bfec.
Final PlayMode after forced refresh/isolation correction: 22/22,
22c1f5f20f144d0faab9b933ee2b22c0. An intervening orphaned job was cleared,
not counted as a success. See handoff for intermediate runs/tooling caveats.

New independent tests exercise command sequences, queue order, append after
autotactics, Stop, limits/orientation, target loss, Flank hysteresis, failure,
snapshot mutation refusal, all personalities/retreat and repeated decisions.
Existing Unity suites retain physical arrival/mass/boost/thrust/engine/avoidance,
blocked queue, AI, UI raycast, lock/EMP, mounts and sector coverage. API calls and
raycasts are NOT claimed as physical mouse/keyboard input.

Save protection: Temp/SaveBackups/stage5 contains baseline SHA256 manifest, copies
and registry export. Fitting tests inject ledger before creating fitting/ship
components; profile singleton is detached and restored. Presets now have a narrow
Read/Write storage seam, overridden in test setup and restored in teardown. No
new global test framework. Core tests do not access persistence.
An initial PlayMode run revealed one temporary isolated-* preset registry value;
it was removed specifically, not by global registry restoration. Both fitting
tests were then moved to in-memory preset storage before the final rerun.
Final all three real JSON hashes match backups; registry differs only in three
Unity session metadata keys. No gameplay preference changes or added test keys.
Final CombatSandbox validation: 0 missing scripts/broken prefabs, Console 0 errors,
outside Play, sceneDirty=false. Generated runner scenes removed; no scene saved.

Game View 1280x720 and 1920x1080 captured AND viewed in preparation using original
Cobra equipment and API-issued two-point route. Temp/Stage5Acceptance/stage5-720.png
and stage5-1080.png show matching route markers and readable tactical panel.
No fitting opened with a preview definition. Editor stopped after captures.

Physical V, mouse fitting open/close, physical tactical input and C# Dev Kit UI
loading remain user checks. No all-project prefab audit/exhaustive waves or stage 6.