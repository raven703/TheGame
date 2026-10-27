# Directional thrust / mounts — partial implementation, NOT final acceptance

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

## Previous-stage history

Implementation and final test results: see tactical-validation.md.
Controls and local avoidance limits: see tactical-controls.md.

Final verified tests: 22/22 EditMode and 13/13 PlayMode, no failures/skips.
Scene saved outside play; 0 missing scripts/broken prefabs. Fresh Play preparation
left for review, HoldPosition, speed cap 100%, empty route, popup hidden.
No branch, commit or push. All changes remain local main.

Manual physical mouse/keyboard end-to-end session and long dense-field stress test
remain unverified; automatic command, physics and real UGUI raycast tests passed.