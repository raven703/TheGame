# Shared inventory — continuation completed for review

Latest section supersedes the partial-stage notes below. Local main, existing
ahead1 commit preserved, no branch/commit/push. Scene saved outside Play, validated
0 missing scripts / broken prefabs. Final intended state CombatSandbox preparation
Play with Cobra fitting open at1280x720. Check final tool result if continuing.

Implemented detached PlayerEquipment ledger, manager read snapshots, staged
Move/Unequip/ApplyPlan/EnsureShip, exact ownership+data conservation and duplicate
instance checks. Commit writes before publishing. Disk writer validates, flushes,
reads back, File.Replace+previous, cleans pending. Failure returns error with no
UI refresh or state changes. Removed unsafe mutable per-ship shared alias writes.
No recurring demo/starter grants. Legacy migration stages and backs up before v4.
V4 missing/duplicate instance IDs rejected. JsonUtility empty objects normalize to
null slot placeholders without compacting. Do not reset user saves.

New UI contract: drag only EMPTY compatible targets, no replacement/swap. Source
stays reserved. CanMove drives highlights/drop/auto. Green border and background;
occupied neutral. Compact one drag card, blocked raycasts. Labels use dedicated
icon and number space; invalid empty serialized entries no longer hide names.
Short tooltip measured/clamped/flipped in root Canvas coordinates, no raycast,
hidden on drag start/close. Close/switch/combat cancellation restores visuals.
Removed obsolete hover-only compatibility implementations. Presets remain atomic
definition plans using free stock + selected ship only (never other ships).

Verified backups: previous 111723 preserved, continuation backup
C:\Personal\Unity\TheGame\My project\Temp\SaveBackups\shared-inventory-continuation-20261028-113723
of already-v4 current state + editor registry export; matching source/copy hashes.
Audit original v3 to current:17->17, definition counts identical. Both old ships
had a demo turret, so TWO physical demo copies retained (not deduplicated).
Current7free+10installed. Full instanceId+placement map matches continuation
backup17/17 exactly. Cobra weapons empty was user's newer persisted state; do NOT
restore older w3/w2 placement. No actual user fitting operation persisted during
UI tests: category drops ran with isolated in-memory ledger, then real ledger and
UI restored. Ship switching only selected ship PlayerPrefs changed/back to Cobra.

Final full tests after functional changes:35/35 EditMode job
f41b6a646e98449e92cf92e9578804fa;15/15 PlayMode job
27df476f59bf46139bb2c5768f66668e. Includes7new ledger tests +1new real UGUI test.
Earlier failures were fixture initialization issues, corrected; one runner stalled
because fixture disabled its runner root, fixed exclusion. Later changes only
whitespace/docs. Console read zero errors/warnings, not cleared.

MCP live: engine drag only empty slots2/3 green, occupied Forsage/source neutral,
occupied drop file unchanged; cancel and close restore, drag layer empty next
frame; icons raycast false. All three categories successfully dropped via real
handlers against isolated ledger. Sidewinder/Cobra selection stock unchanged.
Tooltip corners inside screen for all slots at720 and1080; edge clamp also in
PlayMode test. Screenshots captured AND read: Temp/Diagnostics/shared-tooltip-720-1,
shared-drag-1080,shared-sidewinder-{720,1080}.png. Sidewinder weapon names visible.

Limits: synthetic PointerEventData through actual handlers, not exhaustive physical
mouse automation. No OS power-loss simulation (injected writer failure tested).
Final git diff --check reports trailing spaces in empty YAML fields of the scene
after Unity saved it; code whitespace checks were clean. Not stripped during Play.
Reproducible icon generator from prior stage is still not retained in repository;
the five imported PNGs and metadata are present. These are remaining cleanup items.
Definition data remains legacy item snapshots; separate id/instanceId/placement,
no new catalog/economy. Actual combat aiming constraints untouched. See
docs/shared-inventory.md for format, presets, migration policy and audit.

# Previous partial-stage notes (superseded)

# Shared inventory — IN PROGRESS, NOT ACCEPTANCE READY

User requests full shared inventory + polished fitting. Continue on local main;
no commit/push. Preserve previous mount changes. This is a partial implementation.

Backup BEFORE changes: absolute directory
C:\Personal\Unity\TheGame\My project\Temp\SaveBackups\shared-inventory-20261028-111723
contains original ship_loadouts.json and editor-playerprefs.reg (29388 bytes).
Original JSON SHA256 3F3CB9C51D0D3B96F43208D12AC9158AD7FF38F86111C193C142E780267814D5.
Standalone registry key absent. PlayerPrefs keys: player_current_ship_id and
TheGame.Loadout.v2.{shipId}.{slot}. Persistent JSON path:
C:\Users\user\AppData\LocalLow\DefaultCompany\My project\ship_loadouts.json.
Before migration: Cobra free4 installed4, Sidewinder free3 installed6 =17.
At last disk inspection original format still present; migration NOT verified/run.

Partial changes: FittingItem.instanceId; save wrapper v4 shared inventory;
manager attaches shared list to runtime loadouts, omits per-ship inventories on
disk, migrates legacy records individually with GUIDs, duplicate-ID guard,
pre-v4 backup, pending + File.Replace write; corrupt loads throw instead of reset.
New ships empty; new profile starter inventory issued once. Removed recurring
demo turret injection. Presets already resolve definition IDs against current
ship plus its inventory alias, excluding other installed ships.
UI CanDrop shared between drop and all-slot highlighting (green/yellow), compact
five icons generated once with System.Drawing into Assets/Resources/FittingIcons;
imported as sprites via MCP. Tooltip above slot; generic rejection reason.
Highlight clears on cancel/switch/close; Update cancels when combat locks.

CRITICAL remaining: transactions are NOT rollback-safe on write failure; UI still
mutates shared objects before saving. Need proper staged operation service and
tests for all user requirements. Manager SaveLoadout can accept unowned items;
must enforce conservation. Explicit instance/definition separation currently only
instanceId + snapshot, no definition catalog. ClearAll/DeleteLoadout semantics
need review. Missing v4 instance IDs currently assigned rather than rejected.
Tooltip drag raycast/rejection hover and cursor layout not validated; Resources.Load
called during refresh, not texture generation. Remove unused old hover-highlighting
methods. Need generator retained in repo (only generated PNGs currently saved).
Need new operation/migration/UI tests; none added in this stage yet.

Actual suites: EditMode28/28 job82fdb8306ff04d668ce75e529d10c8de;
PlayMode14/14 jobec76d4d4bfc8409db3cc41fd34dc9ef5. Some later small UI changes
not rerun. Console zero errors before latest changes. git diff --check passed.
No Game View checks, Missing Scripts checks, physical/synthetic drags or final
fresh Play fitting performed yet. Tests may briefly create InitTestScene files;
check runner cleanup before deleting anything. ProjectAuditorSettings was briefly
modified by test tool but disappeared from later git status.
Next: stop Play if needed, inspect diff, finish service + tests, run migration with
count checks17, verify two resolutions, docs, final CombatSandbox Play fitting open.

# Weapon mount fix — review handoff

Latest work supersedes old mount notes below. Local main started clean, ahead 1;
no branch/commit/push made. Root cause reproduced MCP: Fixed on previous mount2
360 returned true side/rear, because no own Fixed cone. Fixed now has own 60
degree cone intersected with hardpoint cone; compatibility independent of sector.
ShipDefinition hardpoints explicit: Cobra FixedOnly60, FixedOnly60, TurretOnly360,
Universal180; Sidewinder two FixedOnly60. Stable IDs shipId:weapon:index.
firePoints preserved. Empty slots retained through UI, manager, stats and presets.
Compatibility checked on drops/auto/symmetric swaps, active migration, ship change,
startup and preset load. Active migration moves to inventory, presets reject atomically.
Player save migrated: w3 slot1, w2 slot2; w1 moved from slot3 to inventory;
demo turret remains exactly one, not auto equipped. Saved layout never replaced
by temporary MCP mixed setup. formatVersion3, legacy preset key retained.

Actual final logic runs: 28/28 EditMode job 16f04da0646344c397e8fa2770a3b09d;
14/14 PlayMode job 97ab50be539f44e3b6c1c08775351420, no failures/skips.
Only subsequent code change: added formatVersion=3 field to preset DTO.
MCP mixed live fire: only cobra_mk3:weapon:2 fired, angle -90 side then 180 rear.
Fixed after fix side/rear=false front=true. Fitting screenshots viewed at 720p
and 1080p: Temp/Diagnostics/mounts-fitting-{720,1080}.png.
Console last read zero errors, not cleared. Scene validate zero missing/broken.
Scene saved outside Play; temporary combat target and loadout discarded on exit.
Final state intended fresh Play preparation, turret in inventory (verify MCP).

Remaining limits: not exhaustive physical mouse matrix of swaps/preset failures;
new integration test groups A-I assertions rather than nine separate cases.
Turret barrel updates only during targeting path; no art overhaul. Sector preview
is selected by hover, one at a time. Read docs/directional-mounts.md for rules.
If continuing, verify git diff/check, final preparation and Console. Do not clear
PlayerPrefs or saves. Existing movement/AI limitations from previous stage remain.

## Previous-stage history (superseded where conflicting)

2026-10-27 continuation, local main, no commit/push.

Implemented: local elliptic thrust budget (forward 100%, reverse 50%, side 35%),
orientation-aware braking/avoidance force, ShipDefinition optional reverse/lateral
force with legacy defaults, Course/OrderTarget orientation, Flank .65/1.4 hysteresis,
orientation UI, explicit Fixed/Turret item type, per-hardpoint sector/direction,
per-slot aiming/range gates, bounded turret speed and wrap handling. Legacy
firePoints preserved; missing mount data uses original firingArcAngle.
CombatSandbox player hardpoint 2 explicitly configured 360 degrees; others 60.
Demo laser ID w_laser_turret_demo: 11 damage vs 15, 90 deg/s, 3 degree tolerance.
Added once to inventory when fitting opens, checks inventory + equipped, preserves
existing items/presets. Fitting lists mount type, sector and tracking; simple barrel.

ACTUAL runs: 26/26 EditMode passed (job 4a535ef79c954ca789023dbe2346a81c);
13/13 PlayMode passed (job b4818c32d9114dd4b4653bdb37f491b8).
First PlayMode job aborted in Unity Test Framework; do not count it as passing.
Last UI placement-only change after those runs needs final rerun.

NOT COMPLETE: full requested new combat integration coverage, MCP scenarios A-E,
1080p review and final 720p recheck. Initial 720 screenshot revealed clipped
orientation row; moved from y302 to y290 and panel bottom to -714, not rechecked.
Runtime isolated aiming demo inconclusive: temporary loadout was not saved; target
at x5 outside actual mount range (6.9 > 6). Moved x3; later inspection hit null
owner state during live combat. Do not claim verified side-fire scenario.
Turret visuals currently update only during valid target Shoot path; should be
decoupled for persistent display when no lock/EMP/module disabled and cleaned on
equipment change. CanAim currently combines pure aiming and visual update; refactor.
Cooldowns keyed by slot preserved across SetEquippedWeapons, but existing fitting
compacts empty weapon slots; audit stable physical-slot mapping before acceptance.
Safety-gate all Shoot entry paths and add integration tests for lock/EMP/heat/cover.
Review hull-alignment speed multiplier: currently retained .15..1 may overly slow
explicit target-facing orbit in addition to lateral force. No new AI sophistication.

Scene saved outside Play after hardpoint change. No runtime objects saved.
Current requested final state should be fresh Play preparation (verify via MCP).
Console contains test-runner errors and obsolete API warnings; not cleared.
Next: stop Play before edits, inspect git diff, resolve above issues, add missing
tests (mass/orientation/boost, resource gates, multi-slot firing, migration), rerun
all, MCP A-E, both resolutions, scene validation, update this handoff with facts.

## Tactical weapon range continuation

See weapon-range-display.md for implementation, passing suites and remaining
acceptance limits. Existing FiringArcVisualizer replaced in place; fitting hover
sector removed from ShipWeapon. No commit/push. Scene saved outside Play.
Mixed preview is runtime-only and does not mutate player equipment save.

## Previous-stage history

Implementation and final test results: see tactical-validation.md.
Controls and local avoidance limits: see tactical-controls.md.

Final verified tests: 22/22 EditMode and 13/13 PlayMode, no failures/skips.
Scene saved outside play; 0 missing scripts/broken prefabs. Fresh Play preparation
left for review, HoldPosition, speed cap 100%, empty route, popup hidden.
No branch, commit or push. All changes remain local main.

Manual physical mouse/keyboard end-to-end session and long dense-field stress test
remain unverified; automatic command, physics and real UGUI raycast tests passed.