# Architecture refactor — stages 0–3

## Current status: stage 3 (2026-10-28)

Supersedes historical status below. Core owns damage HP/module state, damage
distribution, hit chance, shot/resource decision and stable mount reloads.
Unity retains targeting, mount geometry, Physics2D, movement/AI and presentation.
See combat-core.md for call order, compatibility quirks, evidence and limits.
Final Core Release 0 errors/warnings; dotnet 60/60, EditMode 56/56, PlayMode 21/21.
Stage-2 Game View HUD/fitting/overheat review performed; physical mouse smoke not
performed (handler/API-driven setup only). Saves/gameplay preferences unchanged.
No stage 4, checkout, commit or push. All prior local work retained.

## Stage 1 — inventory extraction (2026-10-28)

Supersedes the stage-0 ownership/status statements below. Local main and HEAD
remain unchanged; stage-0 work preserved, no commit/push/checkout. Stage 2 not started.

- Sole authoritative ledger: `Assets/Game.Core/PlayerEquipment.cs`, compiled by
  Unity and dotnet from the same source, netstandard2.0/C#9/noEngineReferences.
  Live Unity assembly references only netstandard. Core owns instance/definition
  identity, shared free inventory, stable per-ship placement, uniqueness,
  category/mount rules, empty-only moves, unequip, auto-equip, preset resolution,
  atomic plans, one-time starter grant and legacy compatibility repair rules.
- `Assets/Scripts/Ships/PlayerEquipment.cs` is the existing public facade, not a
  second ledger. It reads current CombatSession/GameState for each command and
  passes a bool to Core (including EnsureShip). UI never changes model collections.
- `EquipmentDataAdapter` maps ShipDefinition to ID/capacities/stable hardpoint IDs
  and compatibility. Immutable Core items contain only identity/category/mount and
  an opaque DataKey. The adapter retains full serialized FittingItem records by key,
  without placement/ownership collections; reconstructs detached DTOs with original
  saved parameters/colors/description, never the current catalog. Original-reference
  mapping exists only in the short-lived legacy repair adapter, not the ledger.
- Existing legacy parsing/migration and v4 JSON shape remain infrastructure-side.
  Migration results and v4 loads pass Core ownership validation before publication.
  Existing Unity enum declarations/values, GUIDs, preset v2 keys and hole indices
  remain unchanged. Per-ship formatVersion is retained by the adapter.
- Synchronous commands under one lock: prepare detached candidate → validate
  unique ownership and item conservation → callback with detached independent
  snapshot → adapter serializes/writes/flushed/readback/atomic replace → publish.
  Callback exceptions leave state unchanged; reentrant mutation during persistence
  is rejected. No stale prepared result API or asynchronous transactions.
- ShipLoadoutManager remains the lazy persistence facade. EquipmentFileStore is
  the same atomic writer extracted to a path-scoped adapter for isolated tests.
  Optional beforeReplace hook injects disk failure in tests only. `.previous`,
  `.pending` cleanup and pre-v4 backup logic retained. No real player file used as
  a consumable test fixture.
- WeaponMountRules Fits/Validate/Migrate, LoadoutPresets preview and FittingWindow
  auto-equip now delegate to Core. Highlight/drop share CanMove. Drag never removes
  a source item before commit; cancel/close does not need a compensating transaction.

### Stage 1 verification

| Check | Actual result |
|---|---|
| Core Release | 0 errors, 0 warnings |
| Independent NUnit | 26 passed, 0 failed/skipped (12 prior +14 inventory tests) |
| Full EditMode | 39 passed, 0 failed/skipped; job 745f236d6456477e987b5bd4abb3c8b2 |
| Full PlayMode final | 17 passed, 0 failed/skipped; job 0c32a8410e7b40d78d3a34ed3e991384 |
| Loaded CombatSandbox validation | 0 missing scripts, 0 broken prefabs, no repairs |
| Final editor | outside Play Mode, not compiling, scene not dirty; Console 0 errors/warnings at final read |

New adapter tests verify custom saved stats/color/description round-trip, detached
reads, enum mapping, migration repeatability (existing test retained), atomic writer
failure preserving existing bytes and `.previous`, empty indices on file reload.
New PlayMode test drives real FittingWindow/UGUI commands with isolated ledger and
file: green highlight, cancel/close, occupied drop refusal, failed persistence,
unequip, preset load, ship change, auto-equip, UI and current speed update only on
success, then file re-read. Restores singleton pointers, gameplay phase, Random and
time scale; temporary preset key removed. Existing drag-handler test retained.

Non-successful attempts: first new PlayMode expectation incorrectly expected `b`
after preset re-resolution (historical free-first application picks `b` into the
preset, leaving `a` first for subsequent auto-equip); corrected with explicit checks
of both identities. A subsequent complete run had the existing stochastic auto-fire
test fail; isolated repeat passed 1/1. New integration test now restores Random/time;
final full suite passed without changing combat code or weakening old assertions.
MCP refresh timed out, two test-start requests disconnected during domain reload,
one PlayMode init timed out at 0 tests; retries above are the actual final results.
Two earlier EditMode runs were 36/36 before explicit synchronous asset refresh
picked up the new tests; these are not claimed as final 39-test verification.

Rebuild exposed the 9 known runtime CS0618 warnings plus 4 CS0618 in unchanged
WeaponRangeTests and 1 CS0252 in unchanged CombatFeatureTests. No new-code warnings;
these existing test-source warnings were not fixed. Empty final console is not a
claim of warning-free Unity recompilation.

Verified ignored backup:
`C:\Personal\Unity\TheGame\My project\Temp\SaveBackups\architecture-stage1-20261028-144405`.
Copied JSON/.previous/.pre-v4 and exported Editor PlayerPrefs twice with matching
hashes before checks. After full suites all three actual files retain SHA256:

- JSON: `159C3D70929A860123AA5224A4D194D94F3971A5B93FE6D9D08CE022B604F3E9`
- previous: `743A9CC08470ACD28E500F1BD63602B1E796D2D9F6EA17DFD87DFC774F896199`
- pre-v4: `3F3CB9C51D0D3B96F43208D12AC9158AD7FF38F86111C193C142E780267814D5`

Registry comparison after joining multiline values: only PT_Run and three Unity
session keys changed, no gameplay/preset/current-ship changes. No blanket restore.
No scenes/prefabs/settings intentionally edited; no save/backup/diagnostic artifacts
added to Git. git diff --check passed (Git reports only existing line-ending notices).

Limits: no standalone/IL2CPP build, exhaustive asset audit, physical mouse/visual
acceptance or screenshot comparison. Automated real UI/component checks above are
not a claim of manual visual acceptance. Reactor/stats/damage/firing/aim/AI remain
Unity-side. Stop here; wait for explicit stage-2 instructions.

Date: 2026-10-28. Baseline: clean local `main`,
`df8d21c2c05844b34e08cf4d1ba887718f28c0ed`. No checkout, commit or push.

## Rules for every stage

1. Preserve behavior, balance, controls and appearance.
2. Do not introduce gameplay mechanics while moving code.
3. Preserve GUIDs, scene references, stable IDs and empty slots.
4. Never reset saves; change their format only when necessary and justified.
5. Every state has one owner.
6. Never leave parallel implementations of the same rule.
7. UI sends commands and displays results; it does not mutate model collections.
8. Unity physics, scenes, input, UI, visuals and audio stay in Unity.
9. No ECS, global event bus or DI framework without concrete necessity.
10. No commit, push or branch switch without instruction.
11. Save scenes/prefabs only outside Play Mode. Never persist runtime objects.
12. Never call an unperformed check successful.

Game.Core must not reference Unity. Unity adapters may reference Core. This is
incremental extraction, not an engine migration or rewrite. Stop after each stage.

## Dependency and state ownership map

Paths below are relative to `C:\Personal\Unity\TheGame\My project`.

| Area | Current owner and dependencies | Intended boundary / risk |
|---|---|---|
| Shared inventory | `Assets/Scripts/Ships/PlayerEquipment.cs`: private ledger state; detached snapshots, Move/Unequip/ApplyPlan/EnsureShip; persist before publish | Remains the only ledger. Still depends on JsonUtility, ShipDefinition, CombatSession and GameState. Do not add a second inventory. |
| Persistence v4 | `Ships/ShipLoadoutManager.cs`: static ledger, persistentDataPath, JSON, pending/flush/readback/Replace/previous, pre-v4 backup | File/serialization adapter stays Unity-side. Top-level v4 differs from per-loadout/preset v3; preserve both and the v2 PlayerPrefs key. |
| Compatibility | `Ships/WeaponMountRules.cs`: hardpoints, capacities, slot repair/validation, ShipDefinition | Only Accepts delegates to Core in stage 0. Repair and ownership operations are not extracted yet. |
| Stats / reactor | `Ships/ShipStatsCalculator.cs`, `WeaponStatsCalculator.cs`; `ShipReactor.cs` owns Energy/Heat/Overheated | Calculate uses definitions/items/Mathf. Apply also mutates components and can migrate/save. Reactor combines rules, phase, time and logs. Preserve thresholds and multipliers. |
| Damage / firing | `Ships/ShipData.cs` owns hull/shield/modules; `ShipHealth.cs` initializes/applies damage; `ShipWeapon.cs` owns lock, cooldowns and equipped runtime view | Damage math mixes Unity Random/events/lifecycle. Weapon combines input, Time, resource gates, physics and visuals. Runtime equipped view is not an inventory owner. |
| Mounts / aiming | ShipDefinition hardpoints + ShipWeapon mountAngles/weaponReadyAt; existing firePoints | Stable hardpoint IDs and null slot indices matter. CanAim also advances angles and updates LineRenderer. Preserve sector intersection, wrap and origin. |
| Orders / AI / physics | ShipOrderController owns route/order/OrderTarget; EnemyAI chooses orders; ShipMovement applies AddForce/AddTorque | Weapon target is separate. ShipLocalNavigator performs Physics2D casts. Keep physical queries and actuation in Unity; extract decisions later, not now. |
| UI / range | UI/FittingWindow sends ledger commands and refreshes snapshots; UI/FiringArcVisualizer owns runtime geometry/toggle | Existing CanDrop calls CanMove. Range uses ShipWeapon.EffectiveSector/MountAngle, not a second combat rule. UI, meshes, input and lifetime stay in Unity. |

Other important adapters: `Ships/LoadoutPresets.cs` resolves definition plans from
owned equipment and free stock; `Ships/LoadoutApplier.cs` applies saved stats at
startup. `FittingItem` currently mixes legacy stats, identity, Color and visual
metadata. Its ID fallbacks must be preserved during later separation.

## Shared compilation structure

- `Assets/Game.Core/Game.Core.asmdef`: auto-referenced, `noEngineReferences:true`.
- `Assets/Game.Core/MountCompatibilityRules.cs`: sole compatibility expression;
  no engine types, state, serialization or I/O.
- `DotNet/Game.Core/Game.Core.csproj`: explicit linked glob of those same `.cs`
  sources; default compile discovery disabled; target `netstandard2.0`, C# 9.
- `DotNet/Game.Core.Tests`: NUnit tests, `net8.0`, test SDK + adapter.
- `DotNet/global.json`: SDK 8.0.422, latestPatch. Run commands from DotNet so
  SDK selection applies. SDK 10 is also locally installed but not needed here.

Unity 6000.5 documents C# 9 (with unsupported features); Core uses only enums and
ordinary methods. Official reference: Unity Manual 6000.5, “C# compiler and
language version reference”. The conservative netstandard2.0 target needs no new
API or PlayerSettings change. Live Unity compilation verified this exact source.
Do not assume all C# 9 features or arbitrary .NET 8 APIs work in Unity.

The existing global Unity enums stay in Assembly-CSharp, retaining serialized
names/values. The wrapper casts to explicitly numbered Core enums. There is no
second compatibility expression. Adapter tests check all named numeric values and
known/unknown mappings; Core tests preserve even Universal + unknown weapon=true.
These duplicate *representations*, not duplicated state/rules, avoid a serialized
type move in this introductory stage.

The asmdef excludes Core sources from Assembly-CSharp automatically. The dotnet
DLL is outside Assets, never imported. Do not copy it into Assets. Generated
Unity csproj files are not hand-maintained. Unity regenerated the tracked root
`My project.slnx` to add its Game.Core project; this is IDE output, not the dotnet
build configuration. New Unity metadata is retained; old metadata is untouched.

## Commands (PowerShell)

```powershell
Set-Location 'C:\Personal\Unity\TheGame\My project\DotNet'
dotnet build 'C:\Personal\Unity\TheGame\My project\DotNet\Game.Core\Game.Core.csproj' --configuration Release
dotnet test 'C:\Personal\Unity\TheGame\My project\DotNet\Game.Core.Tests\Game.Core.Tests.csproj' --configuration Release --logger 'trx;LogFileName=stage0.trx' --results-directory 'C:\Personal\Unity\TheGame\My project\DotNet\TestResults'
```

Restore needs NuGet access (or populated package cache). NUnit 3.14.0,
NUnit3TestAdapter 4.6.0, Microsoft.NET.Test.Sdk 17.11.1. Tests run outside Unity.
Unity suites: Test Runner → all EditMode, then all PlayMode (or MCP run_tests,
init_timeout=120000). Do not run both editor jobs concurrently.

## Actual checks

| Run | Passed | Failed | Skipped | Evidence |
|---|---:|---:|---:|---|
| Baseline EditMode | 35 | 0 | 0 | bd7b13c347054e399462c11bf7477f80 |
| Baseline PlayMode | 16 | 0 | 0 | fbd227726ba4411994a61ec65c70db99 |
| dotnet Release tests | 12 | 0 | 0 | DotNet/TestResults/stage0.trx (ignored) |
| Final EditMode | 36 | 0 | 0 | b0744d67079a42be86ac963e8252261e |
| Final PlayMode | 16 | 0 | 0 | 9163b6d5384b4178b34de7d0173907f1 |

Core Release build: 0 errors, 0 warnings. Live Unity loaded Game.Core with only
`netstandard` referenced; CompilationPipeline assigns its source to Game.Core.dll;
the real WeaponMountRules wrapper returned the Core result. New Unity test checks
the boundary and enum mapping; existing full suites cover real fitting, mixed
weapon firing, sector behavior, navigation and UGUI. No old test weakened.

MCP real reads confirmed Unity 6000.5.7f1 and CombatSandbox, initially and after
tests outside Play Mode. Scene validation before/after: 0 Missing Scripts,
0 broken prefabs, no repairs. No scene/prefab edits or saves performed.

Non-successful tooling attempts: refresh/compile MCP call timed out at 60s;
subsequent real reads confirmed compilation completed and Core loaded. EditMode
job d584e0cd1a5549a1a7dcafafb12a7006 timed out initializing (0 tests executed),
not counted as a passing run; retry with 120s initialization passed 36/36.

## Save protection

Verified pre-test backup (ignored by Git):
`C:\Personal\Unity\TheGame\My project\Temp\SaveBackups\architecture-stage0-20261028-140445`.
Contains current JSON, .previous, .pre-v4.bak, Editor PlayerPrefs export and
SHA256 manifest. Copies match source hashes; registry export repeated and matched.
Standalone PlayerPrefs registry key absent. Current JSON SHA256:
`159C3D70929A860123AA5224A4D194D94F3971A5B93FE6D9D08CE022B604F3E9`.
After both baseline/final suites all three source save files still match backup.
Registry value comparison: only PT_Run and three Unity session keys changed;
gameplay/preset/selection keys unchanged. No blanket registry restore or reset.

Git ignores dotnet bin/obj/TestResults, local save filenames and registry exports;
only the two intentional manual csproj files are exceptions to the Unity csproj
ignore. Never add backup exports or secrets to Git.

## Existing issues and limits (not fixed here)

- Recompilation exposes 9 CS0618 warnings in CombatSession, PlayerInputHandler,
  AsteroidField, FiringArcVisualizer and PreCombatPanel (old find-object APIs).
  Initial console read was empty; do not confuse that with a warning-free rebuild.
- Older directional-mounts notes describe v3/demo grants; newer shared-inventory
  documentation and actual code take precedence. Historical handoff sections remain.
- No player build/IL2CPP run, exhaustive physical mouse session or visual comparison
  was performed. Full Unity automated suites are not proof of those checks.
- Scene validation covers the loaded CombatSandbox; not a full audit of all assets.
- Core currently contains only compatibility. Ledger, stats, damage, aiming, orders
  and AI are still Unity-coupled; no claim that extraction of them is complete.

Next: wait for stage 1 instructions; sequentially detach the existing ledger and
equipment operations, retain one owner and preserve format/identity/transactions.
## Stage 2 implementation status � 2026-10-28
Implemented Core calculation snapshots and authoritative reactor resources with Unity adapters; details and remaining visual acceptance limitation in stats-reactor.md. Verified Core 40/40, EditMode 47/47, PlayMode 19/19. No stage 3 work, commit or push.
