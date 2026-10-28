# Stage 3 - damage and shot resolution (2026-10-28)

## Ownership and boundary

`Assets/Game.Core/DamageState.cs` is the sole HP owner for hull, shield and three
logical modules. `ShipData` keeps non-damage tuning and forwards its legacy HP
properties; `ShipModule` is a view, not a second HP store. Module collections are
read-only and Core snapshots are detached values. Legacy setup property setters
remain available for existing consumers/tests but write the same Core owner.
Equipment maximum changes use Core ConfigureMaximums: add the maximum difference,
then clamp. Ship selection retains its separate historical full-reset behavior.
Shield boost and its hull payment use Core BoostShield; repair uses Core too.

`WeaponCombat` owns per-hardpoint ready-at deadlines, keyed by the existing stable
MountId. SetEquippedWeapons changes neither those deadlines nor this owner.
`Reactor` remains the only resource owner; no duplicate resource calculation.
`CoverState` holds only destructible-cover HP; Asteroid retains the serialized
120 HP default with FormerlySerializedAs compatibility and all physical queries.

## Shot sequence

Unity checks target availability, pending lock, EMP and combat preparation. Existing
Update additionally checks pause and weapon-module state. Empty installed slots
are skipped, including when fallback is enabled; fallback is only the existing
explicit legacy no-fitting option. Existing per-mount CanAim checks compatibility,
sector, tracking and range, unchanged. A blocked barrel does not stop other mounts.

Core checks weapon operation, stable-ID reload deadline and shared reactor
availability. A successful spend occurs once and starts reload. Unity's deferred
Physics2D cover query then returns a Core CoverState, never a collider/GameObject.
Nearest-asteroid choice and impact coordinates remain in Asteroid.FindCover.
Cover takes the original nominal weapon damage, without hit/module random rolls.
Otherwise Core compares the hit roll strictly `< clamp01(accuracy + kind bonus -
evasion)`. Misses still spend resources and reload. Hits run the Core damage path.
ShotResult contains refusal reason, miss/ship/cover, damage/module outcome,
cover-destruction flag and ready-at time. Unity draws only actual shots and uses
notifications for logs/deactivation, never to apply damage a second time.

Time is explicit scaled game time (`Time.time`) for shot deadlines, matching the
old implementation. Reactor Update still receives scaled deltaSeconds once.
No second timeScale multiplication. UnityCombatRandom uses the same global
UnityEngine.Random.value and integer Range source in the same order: hit roll,
then (only if shield did not absorb and no explicit target module) module roll,
then integer module selection if roll < .4. Refusals and cover consume no rolls.
Core has no UnityEngine, Time, Mathf or singleton dependency.

## Preserved non-obvious behavior

- First module destruction discards excess damage; subsequent hits to that module
  send half their damage to hull. Destroyed shield module retains shield HP but
  disables absorption. Destroyed engine makes evasion zero.
- Plasma removes an extra half-damage shield amount before normal processing.
- Railgun bypasses 40% to hull after the primary damage notification. Existing
  OnDamageTaken subscribers can therefore receive two calls, in their old order.
  This is intentional compatibility, not accidental duplicate damage.
- Hull reaches zero immediately, but destruction is reported by ShipHealth.Update.
  Core latches reporting once before OnDestroyed; subscribers run before object
  deactivation. EnemyRegistry subscribers and wave code remain Unity-side.
- Direct Shoot historically has no additional pause gate; normal Update does.
- Evasion itself is not clamped; the final hit chance is. Strict comparisons keep
  a Unity roll of exactly 1 from hitting even at probability 1.
- Repeated cover destruction notifications are now suppressed by a Core latch,
  as required for single destruction; old Destroy-deferred Asteroid could log twice.
  Nominal cover damage/nearest selection are unchanged.

## Evidence and limitations

Before production replacement: full EditMode 56/56, job
69a6a8dcd6e646ab9da776ac239b1a61 (47 previous +9 damage characterization cases).
The first 47-test run had not imported new tests. Two initialization attempts
timed out with zero tests. A discovered 56-test run exposed fixture counters not
reset between tests; fixed fixture only, then the above baseline passed.

Core Release: 0 errors/warnings. Independent tests: 60/60, stage3.trx under ignored
DotNet/TestResults. Final EditMode 56/56, fccaf039dec641939ea0f8f039c6883d.
PlayMode intermediate 21/21, e061b5d4e90c49379bbf67d619d717ce.
Final PlayMode 21/21, job cbe92d5c52294bda8545bec224beaf4f, zero failures/skips.
An earlier 19-case PlayMode run failed the old stochastic AutoFire assertion
(target shield unchanged); no test weakened, full repeat passed. Domain reload
also disconnected two test-start attempts; those are not passing runs.

New tests cover scripted random boundaries, shields, all three kinds, modules,
discard/half-spill, maximum differences, one-time destruction, ordered notification,
stable independent reloads, resource atomicity, refusal/no random, empty slots,
nearest physical cover, actual beam count, loadout refresh, shield-boost hull
payment and registry removal. Existing mixed Fixed/Turret, lock/EMP, overload,
resource and fitting tests remain; reflection HP reads now accept Core-backed
properties rather than expecting removed mutable Unity fields.

Game View reviewed at 1280x720: HUD 100/100 energy and 0 heat, fitting base/equipped
totals, and live 60/100 energy, 100% overheat/red bar/disabled abilities. No new
overlaps observed in these views; no restyling. Captures in ignored
Temp/Stage3Captures. Fitting opened by Open handler; combat state/heat arranged via
runtime API. This is NOT physical mouse validation or a full interactive smoke.
No OS-level click automation was used. No exhaustive wave progression test or
all-prefab asset audit; loaded scene validation reported 0 missing scripts/broken
prefabs. These limits must remain explicit.

Backup: Temp/SaveBackups/architecture-stage3-20261028-164455, verified copies and
two matching registry exports before tests. After final tests all three real save
hashes match; selected ship and saved loadout PlayerPrefs match. Unity session and
test-run metadata are excluded from gameplay comparison. Final Console read has
zero errors/warnings; compilation still has pre-existing CS0618 warnings. The new
test's obsolete FindObjects overload was corrected without changing old warnings.
Generated ProjectAuditor append was removed; no scene/prefab edits retained.
No save format changes, commit, push or stage 4.