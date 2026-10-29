# Architecture refactor — stages 0–6

## Stage 6 final dependency audit

This section supersedes historical ownership/next-stage statements below. The
existing extraction was audited, not redesigned. The game is NOT Unity-independent:
scenes, physics, rendering, audio, UI, input and lifecycle adapters remain Unity.
Baseline and final branch/HEAD: main/cbb4478. Prior stage-4/5 local work retained;
no checkout, commit or push. No scene/prefab edits or runtime scene saves retained.

### Assemblies and dependency direction

Unity Assembly-CSharp -> Game.Core -> netstandard. Editor/test assemblies consume
the adapters; Core never consumes them. Assets/Game.Core is the only source tree;
DotNet/Game.Core/Game.Core.csproj links that same tree, netstandard2.0, C# 9.
Game.Core.asmdef keeps empty references and noEngineReferences=true. Live Unity
reflection reports only netstandard 2.1 (Unity's host reference), not an engine
reference; the independent artifact remains netstandard2.0. No imported Core DLL.
CoreBoundaryTests checks assembly references/public signatures, shared source,
forbidden engine/storage/clock/random access, project settings and imported DLLs.
This is deliberately a small guard, not a general-purpose semantic analyzer.

Root My project.slnx regenerated through SyncVS: 55 projects, zero DotNet entries,
zero duplicate project names. UnitySolutionBoundary keeps external projects out.
DotNet/Game.Core.sln remains independent; generated csproj files not hand-edited.
No global VS Code settings changed. C# Dev Kit GUI loading remains manual.

### Final state ownership

| State | Authoritative owner | Unity facade | Consumers |
|---|---|---|---|
| Shared stock, installed instances, starter flag | Core PlayerEquipment | PlayerEquipment / ShipLoadoutManager | FittingWindow, presets, LoadoutApplier |
| Hull/shield/module HP, destruction latch | Core DamageState | ShipData, ShipModule, ShipHealth | weapon resolution, abilities, HUD, registry |
| Energy/heat/overheat | Core Reactor | ShipReactor | weapons, abilities, HUD |
| Weapon ready-at deadlines by mount ID | Core WeaponCombat | ShipWeapon | shot admission |
| Equipment stat calculation | Core StatMath | ShipStatsCalculator / adapters | physics tuning, HUD/fitting, reactor/HP maxima |
| Aim angle by stable mount ID | Core MountGeometry | ShipWeapon | firing gate, barrel/sector rendering |
| Order/queue/target ID, orientation, hold/braking, Flank phase | Core ShipOrders | ShipOrderController | ShipMovement, tactical UI, route visuals |
| Personality decisions | Core PilotDecisions (stateless) | EnemyAI | same ShipOrders owner |
| Physical detour/progress/retries | Unity ShipLocalNavigator | ShipOrderController | ShipMovement |
| Lock progress/EMP and ability cooldowns | Unity ShipWeapon / ShipAbilities | same components | HUD, target selection, ability input |

Detached inventory DTOs are mutable copies; immutable items and read-only route/HP
snapshots cannot mutate owner collections. ShipData setters/ShipModule methods are
compatible forwards to the same owner, not parallel damage rules. UI commands go
through the ledger; renderers read angles/results and do not spend/apply damage.
Save adapters parse/migrate/serialize; compatibility and ownership remain Core rules.

### Lifecycle, clocks, randomness and physics

ShipHealth.Awake initializes damage once. Reactor lazily initializes, so Configure
before Awake does not reset it. LoadoutApplier.Start schedules existing preparation
application after Awake; equipment refresh configures maxima/tuning without replacing
damage/reactor/aim/reload owners. Ship change retains distinct mount IDs; removal
and replacement preserve same-mount aim/reload as characterized. Disable/re-enable
does not silently reset models; target availability rejects disabled/destroyed ships.
Destruction latch reports once before deactivation; restart constructs fresh scene
owners and explicitly clears EnemyRegistry. ConditionalWeakTable identities and
weak target references do not transfer orders to recreated ShipHealth instances.

FixedUpdate: observe AI -> Core decision -> order command/evaluation -> physical
projection/avoidance -> unchanged forces/torques. Only Unity executes movement.
ShipWeapon.Update advances lock then each occupied mount's aim before Core shot
admission/spend/reload -> external nearest-cover Physics2D query -> damage -> visuals.
Reactor.Update supplies scaled Time.deltaTime once. Deadlines receive Time.time;
Core has no clock or singleton. UnityCombatRandom supplies hit/module rolls in
existing order. Stage 6 removes Core Guid.NewGuid: starter identity factory is now
explicit from the adapter (same GUID format); independent tests supply deterministic
IDs. Constructor without a factory is usable for non-grant ledgers, but attempting
actual starter issuance requires a supplied identity source.
Queries/colliders, scene target search, transforms and asteroid generation stay Unity.

### Replaced paths and compatibility

No additional executable duplicate was proven safe to delete during this audit.
Previously replaced aim/order/shot formulas remain replaced by Core delegates.
Do not delete compatibility APIs just for file count: SelectMount is still called
by fitting; orbitClockwise/manualMoveOrder remain serialized in both ship prefabs,
but no longer own order state. ShipHealth/ShipData damage APIs forward to Core.
No mass rename, asset reserialization, balance or presentation change was made.

### Persistence and isolation

v4 JSON, GUIDs, holes, legacy migration, atomic .pending/.previous replacement and
pre-v4 backups stay unchanged; see shared-inventory.md. Presets keep v2 keys.
Fitting fixture setup now installs a production-access failure guard, clears the
cached ledger before components and uses memory preset delegates. Existing isolated
file scenario writes only its unique OS-temp directory. Fixture teardown restores
repository/profile/session/game flags, random/time, delegates, temporary definitions
and directories; UnityTearDown unloads isolated scenes even after assertion failure.
The new shortened battle uses the same isolation. Other physics tests instantiate
ships without LoadoutApplier/profile/UI persistence; editor file tests use temp paths.
The guard is fixture-scoped, not a claim that arbitrary future tests cannot bypass it.

Baseline copies and registry exports: Temp/SaveBackups/stage6 (ignored). After full
tests AND visual/restart preview all three real JSON files match byte-for-byte/SHA256:
current A9B2264D9B97C730D9C26B79984D000CE00C0B644A25E630685F222FE20A6175;
previous 987CE9BADEA4C154BF000A51C47D7AE9EDE6236BC66B9BB7806843ED7E3C242F;
pre-v4 3F3CB9C51D0D3B96F43208D12AC9158AD7FF38F86111C193C142E780267814D5.
Registry comparison changed only unity.player_session_count, unity.player_sessionid
and unity_connect.session_id; no gameplay/preset/selection changes or temporary keys.
No production-file restore was needed in stage 6. Historical stage-4/5 incidents
remain documented and must not be described as unchanged-throughout runs.

### Verification

| Check | Actual result |
|---|---|
| Core Release | 0 warnings / 0 errors |
| dotnet full | 89 passed / 0 failed / 0 skipped; DotNet/TestResults/stage6.trx |
| EditMode full | 61/61, 0 failed/skipped; 0eb48e4d3cef4a7fa644684d81237c9e |
| Initial PlayMode full | 22/22; 3c04b52685f04312a33bb16e2763f69d |
| Final PlayMode full | 23/23, 0 failed/skipped; 54aa9f0e161e48d7ac4588acfdf0c227 |
| All discovered scene/prefab assets | 5 scenes + 8 prefabs; missing scripts/broken prefabs 0/0 |
| git diff --check | exit 0 |

Production changes were covered by all suites. Subsequent executable changes were
PlayMode fixture/new battle test only; final full PlayMode rerun covers those, so
unchanged Core/EditMode suites were not pointlessly repeated. Final doc-only edits
do not require another full test run. Existing obsolete Unity API warnings are not
a warning-free Unity rebuild claim.

The new shortened battle initially failed twice (adcb1402c6574dd08a05485753f84548,
3076bcad2d4a40a1a93d57f1253846e4): fixture lacked GameState then facing command.
Fixed fixture only; targeted 890ed86724f74fb1986e853c5437cd19 passed 1/1 before final
23/23. No assertions weakened. No interrupted Unity test jobs in this stage.
Tool-only attempts: internal SyncVS access and obsolete GetInstanceID failed to
compile; corrected reflection/GetEntityId calls succeeded. One shell command had
unobservable completion; a later persisted diff-check exit file explicitly gives 0.

Integration suites cover two-ship stock, occupied refusal, atomic presets/failure,
reload, stats/resources/abilities, Fixed/Turret fire gates, four personalities,
asteroid cover/avoidance, route/Stop, destruction and recreation. Added real
Update shots -> destruction callbacks -> registry removal -> two wave completions
-> CombatManager victory. Intentionally shortened: two single stationary 1-HP
enemies, 10000 fallback damage, API target/order commands; NOT unchanged normal combat.
Preview used a memory copy of the real profile installed before components, temporary
DisableDomainReload, then restored editor settings and explicitly reloaded scripts
to clear substitutions. Actual RestartCombat returned fresh player identity,
HoldPosition, empty queue/registry, no target, angle 0, energy 100 and heat 0.

1280x720 and 1920x1080 route/HUD/tactical/sectors AND fitting screenshots captured
and viewed: Temp/Stage6Acceptance/stage6-{720,1080}-{route,fitting}.png. No new overlap
seen; inventory is intentionally clipped within its scroll viewport. Handlers/API,
not physical clicks. Final normal Play preparation is for user manual acceptance.

### Build and extension guide

From C:\Personal\Unity\TheGame\My project\DotNet:
`dotnet build Game.Core/Game.Core.csproj -c Release`
and `dotnet test Game.Core.sln -c Release`. Unity Test Runner: run all EditMode,
then all PlayMode, after refresh/compilation. Protect real storage before tests.

New item: add serialized definition/snapshot data through existing FittingItem and
adapter, preserve instance versus definition identity; never inject stock on UI open.
New weapon: add existing data/enum mapping and Core WeaponStatMath/shot rule tests,
retain stable hardpoint ID and Core resource/damage path; visual consumes ShotResult.
New rule: implement in the owning Core model with plain values/explicit observations,
add independent tests and Unity adapter characterization; do not query scene in Core.
Storage-format change: version/migrate in adapter with verified backup and ownership
validation before publication, not in Core and not by silently resetting progress.

### Known issues / remaining manual acceptance

- Existing standalone CombatSession.Started fallback is not set by ShipSpawner when
  GameState is absent; normal scene contains GameState. Recorded, not refactor-fixed.
- Existing direct Shoot pause behavior, railgun two notifications and frozen idle
  barrels are retained compatibility (see combat-core.md / mount-geometry-core.md).
- Lock/EMP/ability timers remain Unity gameplay adapters; this is not extraction of
  every game rule. No standalone/IL2CPP build or exhaustive unchanged full battle.
- Physical V toggle; mouse fitting open/close, drag/right-click; queued movement,
  Stop, tactical/orientation and ability buttons remain manual checks.
- VS Code: Reload Window -> C# Dev Kit Solution Explorer -> select root
  C:\Personal\Unity\TheGame\My project\My project.slnx; confirm one Game.Core and
  no duplicate-name/load error. Open DotNet/Game.Core.sln separately for SDK work.
  IDE GUI unavailable to agent; not marked passed.

## Stage 5 current status

Orders and four AI personality decisions now use Core ShipOrders/PilotDecisions.
Unity retains observations, target resolution, local physical navigator and forces.
See orders-ai-core.md for ownership, update order, compatibility and verification.
This supersedes historical stage-4/stage-3 ownership below. Stage 6 not started.

## Stage 4 current status

Mount geometry and stable-ID aiming state now live in Core MountGeometry.
Root Unity / independent DotNet solutions separated; generator callback prevents
external project reinsertion. See mount-geometry-core.md for exact ownership,
coordinates, lifecycle, verification and acceptance limitations. Stage 5 not started.
This supersedes historical ownership and git-state statements below.

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
