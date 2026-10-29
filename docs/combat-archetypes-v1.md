## Controls and flight update — October 29, 2026

LMB enemy selects weapon lock without changing order; repeated selection preserves lock. LMB empty does nothing. RMB space replaces route, RMB selected enemy engages, RMB other enemy moves to position snapshot without selecting. Shift+RMB appends; Stop/S brakes without clearing weapon target. UI blocks world clicks. Explicit Tab retained; implicit nearest-enemy acquisition removed. Pending weapon selection now visible in frame/HUD independently of tactical target. No actual movement-triggered reset reproduced; competing autoselection/UI ambiguity fixed, not a claimed proven movement reset.

Standard hull FlightVersion2: Sidewinder speed10, force19; Cobra speed8, force28.5. Shared regulator compensates desired damping through normal force budget. Before naked-hull straight speeds6.154/3.889 (90% unreachable in10s), after10/approximately8; measured t90=1.62/2.04s. Sidewinder Stop1.20s/3.714units versus old.98s/1.850units. Cobra final braking distance not separately recorded. No weapons/modules retuned; no grants or refit. Existing CameraFollow owns speed zoom: .2 threshold, cruise1.35/max1.6, smoothing .15/.5/1s, freezes pause/fitting, supports external user base size. Parallax quads cover expanded viewport. See newest handoff for test XML, final state and incomplete manual zoom/physical-click checks; do not report full acceptance.

## Ordinary CombatSandbox integration — October 29, 2026 (supersedes demo priority)

User authorized production profile upgrade; main branch retained, no commit/push/switch. CombatSandbox scene saved outside Play Mode with runtime ShipSpawner role configuration and hull references: waves Interceptor; Artillery; Escort; Interceptor+Escort. NPC use existing CombatDemoLoadouts definitions (runtime despite historical name), shared ShipStatsCalculator.ApplyNpc, EnemyAI role pilot and ordinary paid ShipAbilities. No player inventory access for NPC, no new demo/profile/screen. Editor-only ApplyExperiment is not used by ordinary combat. Runtime Windows paths are compiled into Assembly-CSharp; standalone Windows executable build/launch was NOT performed.

Production backup (outside Temp, excluded from Git): C:\Personal\Unity\TheGame\My project\Tools\CombatEvidence\ProductionUpgrade-20261029. JSON copies verified SHA256; PlayerPrefs registry exported. Original current JSON hash 9871051137244C9782F4246FF34F85693DEDD30626AB8B935FB44DB50C7B96A2. This is the current authorized checkpoint, not the lost historical baseline.

One-shot combatStarterVersion=1 persisted through Unity/Core adapter and Core snapshot copy. Grant checks category-compatible stable IDs across free inventory and every ship. Existing records untouched, no auto-equip, no replenish after version stamped. Migration + grant use one atomic EquipmentFileStore replacement before publishing ledger. Failure-injection test verifies original bytes and pending cleanup. Known-standard migration remains CombatStandardDefinitions.Upgrade balanceVersion=1; unknown IDs untouched.

Actually added: lightweight-engine, maneuver-block, radiator, capacitor, w4 pulse laser, laser-turret. Already owned: s2 heavy shield, w2 plasma, w3 railgun. All 17 previous items retain every field and instanceId; placements/empty slots unchanged; total23. Existing profile had already received versioned standard parameter migration: no previous item fields changed in this authorized upgrade. New records use unchanged catalog values. After hash3755316138E82B78476077C6F554ABBC876B9118EC1F1492D2B2AAACDC899631 remained identical through restart/reentry/fitting. No presets rewritten.

Fitting standard descriptions now derive from actual catalog effects; 1280x720 HUD/fitting screenshots inspected. Long statistics moved into masked scroll at readable14pt rather than clipped/shrunk text; reverse/lateral stats use calculated values. Existing unrelated UI/selection settings preserved.

Checks: Core Release build0 warnings/errors, Core tests102/102; affected EditMode21/21 XML20261029-124818-265 (MCP initialization timeout was false negative, authoritative XML Passed); affected FittingInteraction PlayMode3/3, latest1.4544937s. Earlier new fixture initially failed because raw JsonUtility null slot becomes empty FittingItem: test now uses production Parse normalization, assertion retained. No full regression or72-batch. Real CombatSandbox smoke confirmed roles/weapons: Interceptor w4/w4 Approach/Exit; Artillery w3/w2 Recover energy83.67/155; Escort w4/turret Orbit and resource spending; W4 both roles. Defeat panel observed in unmodified battle; victory and all wave transitions verified with diagnostic lethal damage, temporary runtime-only player HP and shorter pauses. Restart reload clears all diagnostic changes. This is not an unassisted four-wave victory/balance test. Ability code audited to call ordinary paid methods; detailed per-ability NPC telemetry not captured.

Final: ordinary CombatSandbox Play Mode, preparation=true, fitting=false, enemies0, role configuration=true, scene dirty=false, Console errors0. Windows player build/run remains unverified. Backup and evidence available outside Temp. Existing historical full-suite limitations remain; no claim full regression clean.

# Combat archetypes v1 — implementation in progress

## Verified update — October 29, 2026

### Playable demonstration update

Interactive Editor demo implemented in the separate CombatArchetypesV1 scene, excluded from build. Choose builds A/B, AI-versus-AI or player A, fixed asteroids, start/restart, see elapsed time/winner and applied ship values. Banner explicitly states demonstration progress is not saved. Player uses right-click movement and three paid ability buttons; automatic fire targets B. Detached catalog loadouts use real ShipMovement, ShipWeapon, reactor, damage and role AI; no production inventory grants.
Three completed smoke fights and eight passing targeted EditMode tests recorded under ignored Tools/CombatEvidence (outside Temp). See current handoff for exact report paths. Production save unchanged across this demonstration session. Batch72 deferred, no balance conclusions/tuning. Manual player-versus-each-role and two-resolution visual acceptance remain limitations, not completed checks.

### Latest investigation (supersedes storage/report availability below)

Latest test localization: added Editor TestRunnerApi callbacks in CombatTestEvidence.cs, saving authoritative framework XML plus Editor log after every run under ignored `Tools/CombatEvidence/TestRuns` (outside Temp). Historical MCP initialization timeout is not a framework test timeout; the old framework result could not be recovered retrospectively.
Storage audit warning: latest main save SHA256 is9871051137244C9782F4246FF34F85693DEDD30626AB8B935FB44DB50C7B96A2, not original B0386A. A line-by-line comparison against the outside-Temp checkpoint-20261029 copy showed no differences (both format4, inventory3, loadouts3), so change predates the latest fixture runs. Current file copied to Tools/CombatEvidence/save-after-tests.json. Do not blindly restore; locate exact earlier write/migration before claiming original storage preservation. Further combat runs paused for this audit.
Unmodified victory-chain fixture reproduced Passed in XML 20261029-113853-132.xml (1.190761s). Fixture depended on random spawn direction/hit rolls and erroneously assumed fallback firingArcAngle=360 affected AdvanceAim (fallback uses a 60-degree fixed cone). Exact original failure causality remains unproven. Replaced fallback with explicit test-only equipped 360-degree gun, damage10000/interval0.1/energy1/heat1, stationary shooter and guaranteed accuracy; real shot/damage/destruction/registry/waves paths and 15s deadline/assertions retained. Three consecutive XML results Passed: 0.299036, 0.244387, 0.255382s.
One subsequent full PlayMode run completed: XML 20261029-114951-278.xml, 23/24 Passed, 1 Failed, duration65.9116043s. Victory-chain test passed. Failure was AutoFire_RequiresLock_RespectsEmp_AndDoesNotOverrideRoute (shield unchanged1000); removed random miss dependency with fixture-only accuracy1/evasion0. Targeted XML 20261029-115206-894.xml Passed in2.080163s. Full suite after this last fixture change remains pending; do not report24/24.
External manual Editor closure is the working hypothesis for shutdown, not a confirmed crash. One main Editor confirmed; Asset Import Workers are not duplicate editors. Evidence directory excluded through .git/info/exclude. No balance changes.

Separate verified results: `dotnet build` Game.Core Release succeeded with 0 warnings and 0 errors; `dotnet test` Game.Core.Tests Release passed 102/102, 0 skipped.
Previous interrupted PlayMode log contains script compilation/domain reload during the run followed by a null controller at PlayModeRunTask.cs:52. Files must not be edited while tests are executing. A separate TimeController.OnDisable null-controls exception was also found; enable/disable now guard uninitialized controls.
The fresh full PlayMode retry was interrupted by complete Editor shutdown (not counted as passing). Shutdown log preserved at `C:\Personal\Unity\TheGame\My project\Tools\CombatEvidence\playmode-shutdown.log`. Editor was relaunched; shutdown trigger is not established.
CRITICAL: Editor shutdown cleared Temp, including original save/prefs backups and previous smoke reports. Those historical report paths below are no longer available. No original playerprefs.reg or save-hashes.csv was found under the parent game directory after shutdown. Do not claim final gameplay-pref equality against the original baseline. Do not replace that baseline with current state.
Main production save still hashes to B0386A75009C9CF35C368D4FE1DFB86D1475C9E199D28BF31F3F93ED55D9A5D1, matching the documented original hash.
The requested 72-fight matrix has NOT been run. Existing experiment loop is not that matrix (includes mirrors and lacks independent seed coverage), and must be replaced with a durable manifest/resume implementation before batch execution. Lifecycle/error coverage, production integration assertions and two-resolution review remain incomplete. No balance changes made during this continuation.

This section supersedes the historical partial-A status below. Work remains incomplete.
Catalog/versioned item migration, directional thrust, detached demo builds and three role pilots are implemented.
Latest verified Core Release run: 102/102. Unity EditMode: 69/69 after the latest experiment-only stat-application refactor.
Full PlayMode did not finish: runner failed during ExitPlayModeTask after 21/24 with no reported assertion failures at that point. Do not count as passed.
Editor experiment scene: `C:\Personal\Unity\TheGame\My project\Assets\Scenes\CombatArchetypesV1.unity`; deliberately excluded from build settings.
Menu: TheGame/Combat v1/Create isolated experiment scene. Editor stopped after smoke.
Initial smoke was invalid (zero equipped weapons). Fixed through an Editor-only detached application path that skips migration/persistence; production Apply remains preparation-only.
Valid three-fight smoke reports: `C:\Personal\Unity\TheGame\My project\Temp\CombatArchetypesV1\Reports\20261029-105644`.
Interceptor mirror: A wins in 29.76s; versus artillery: B in 20.50s; versus escort: B in 23.84s. These are smoke results, not balance conclusions.
All three production ship_loadouts files match backup SHA256 after smoke. Full PlayerPrefs export differs, including Unity session IDs/count and a serialized timestamp; game-specific key equality still requires structured verification. No registry restoration performed.
Remaining: robust exception outcomes/cleanup tests, telemetry destruction safety, independent seed coverage, corrected per-weapon metric geometry, full 72-fight batch, visual checks at both requested resolutions, final Unity regression tests and PlayerPrefs audit.
Physics measurements: Sidewinder force 14, t90 actual terminal 1.64s; Cobra force 14, 1.96s. Damping means terminal velocity is about 76.9% of configured max, so configured-speed t90 is unattainable under unchanged controller physics.
No commit, push or branch switch. Existing user settings/TMP/build-profile changes retained.

## Historical status (superseded)

Only part of A implemented. This is NOT the requested vertical slice.
No commits, pushes, branch changes, production inventory grants or scene saves.
Baseline HEAD: e0991faba0dd192dcd06cf9d576b372b407a8b5c, main.
Pre-existing local settings/build-profile changes retained.

## Storage baseline

Verified file copies, SHA256 manifest and registry export:
`C:\Personal\Unity\TheGame\My project\Temp\SaveBackups\combat-v1-20261029-121549`.
Machine-generated directory timestamp is not an assertion of task date.
Production ship_loadouts.json SHA256:
B0386A75009C9CF35C368D4FE1DFB86D1475C9E199D28BF31F3F93ED55D9A5D1.
No production ledger instantiated by this work. Final independent comparison
command timed out; do not claim post-work save/prefs equality is verified yet.

## Baseline and commands

From project root, independent build/test:
`dotnet build DotNet/Game.Core/Game.Core.csproj -c Release`
`dotnet test DotNet/Game.Core.sln -c Release`.
Fresh baseline 89/89; after Core changes 97/97, zero failed/skipped.
Release build zero warnings/errors. Evidence in ignored
`C:\Personal\Unity\TheGame\My project\Temp\CombatArchetypesV1`:
combat-v1-baseline.trx, substage-a.trx, core-a.log, tests-a.log.
Unity refresh timed out and MCP subsequently reported ping unanswered.
EditMode/PlayMode and Unity compile verification remain pending.

## Current changes, not yet connected to gameplay

Core CombatArchetypeCatalog version 1 has four independent weapon definitions:
w4 pulse laser 10/.7/6/7, w2 plasma 22/1.4/14/20,
w3 railgun 26/2/20/25 and combat-v1:laser-turret 7/.8/7/8
(damage/interval/energy/base heat). w1 and w5 remain legacy, not aliases.
Five module data entries: new combat-v1:lightweight-engine, maneuver-block,
radiator, capacitor; existing s2 reserved for heavy shield, not duplicated.
These definitions currently do NOT update saved snapshots or Unity items.

StatMath adds additive capacity/cooling and percentage sums with requested caps
30% speed, 20% forward, 60% reverse/lateral; boost penalty sums independently.
Legacy additive thrust retains its old .5/.35 axis effect. New forward percentage
does not boost reverse/lateral. DirectedThrust exposes normalized elliptic budget.
Unity still uses its old limiter; integration is pending. Tests verify catalog,
sum/caps, axis separation, module tradeoffs, boost heat before resistance and
normalized diagonal effort. Existing 89 tests also pass.

## Actual baseline parameters / constraints

Sidewinder: hull80/shield40/mass1.5/speed8/thrust18/turn420,
energy100/regen12/cooling15/lock.35/resistance0; two Fixed, shields2, engines3.
Cobra: hull120/shield60/mass3/speed6/thrust14/turn300,
energy100/regen12/cooling15/lock.5/resistance0; Fixed0/1, TurretOnly2,
Universal3; shields4/engines4. Requested builds fit these capacities.
Movement sets linear damping1.2 and angular damping3; velocity controller gain4.
No physics measurement or thrust tuning performed. Existing FittingItem default
turret aim tolerance is 3 degrees; serialized zero values also exist and need
explicit migration policy. Shared weapon range unchanged.

## Remaining required work

Finish A Unity compilation, fresh Unity tests/storage guards and source-of-truth
integration design. B: Unity definitions, narrow versioned snapshot migration,
stats/physics/abilities/UI integration and real acceleration/braking measurement.
C: three persistent Core role state machines, explicit opponent IDs, paid abilities,
demo loadouts/profile. D: isolated real-physics stand, shot/outcome metrics and
cleanup, smoke then bounded batch, complete tests, two-resolution Game View review,
final production hashes/prefs comparison and isolated manual preview.
No battles run, no win rates, no role-strength or balance conclusions available.
No demo left running because it does not exist yet. Do not describe this as done.