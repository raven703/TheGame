# Stats and reactor — stage 2 semantic baseline (2026-10-28)

Recorded from the pre-extraction code; no balance fixes intended.

- Null-definition base: hull100/shield50/mass2/speed5/thrust12/turn300/lock.5,
  energy100/regen12/cooling15/resistance0. ShipDefinition supplies its saved asset
  values otherwise (default asset hull120/shield60/mass3/speed6/thrust14).
- Sum installed weapon, shield, engine slots in that order, preserving list order.
  Skip null/empty definition IDs; inventory never counts. Add shield/mass/speed/
  accel/lock/resistance, not damage to ship stats. Then mass>=.5, lock>=.05,
  shield/energy/regen/cooling>=0; resistance clamp0..75%. No speed/thrust/hull clamp.
- Legacy e2 effective resistance is always .1 regardless of saved resistance;
  other saved item parameters are used directly, not current catalog values.
- Apply current hull/shield = clamp(current + newMaximum - oldMaximum,0,newMaximum).
  Repeated application does not accumulate. Directional ratios remain Unity-side:
  positive baseReverseThrust/baseAcceleration (denominator >=.01), else .5;
  lateral analogous, fallback .35. No change to physical thrust rules.
- Weapon kind: explicit non-Laser enum wins; else w3=Railgun, w2 or Plasma visual
  =Plasma, otherwise Laser. Damage=item.damageBonus, null item=fallbackDamage .5.
  Cooldown=max(.1, kind base + saved cooldownBonus): Laser caller base (.6 default),
  Plasma1.2/Railgun1.8. Energy7/16/22; heat9/22/30. DPS=damage/cooldown.
- Reactor initial energy=serialized maxEnergy (default100), heat0, overheated false.
  Configure computes delta=raw capacity - old maximum BEFORE max(0,capacity),
  currentEnergy=clamp(currentEnergy+delta,0,newMaximum); regen/cooling>=0,
  resistance clamp0..75%. Heat and overheat latch survive configuration.
- Tick is skipped in preparation. delta=max(0,delta); energy=min(max,energy+regen*dt),
  then heat=max(0,heat-cooling*dt). Exit overheat exactly at heat<=35.
- Spend skipped in preparation/overheat/insufficient energy. On success subtract
  requested energy, then heat=min(100,heat+max(0,requestedHeat)*(1-clamp(resistance))).
  Reaching exactly100 succeeds and latches overheat. Failed spend changes nothing.
  Negative costs were not rejected; preserve existing arithmetic, do not broaden
  validation as a balance change. Cooling/regen independent of resistance.
- Update passes Time.deltaTime once (already scaled). Pause naturally gives zero
  dt, 2x scales once. Direct Tick explicitly takes game seconds. Direct TrySpend
  historically has no pause check; do not introduce one. Weapon Update does check
  pause. No imposed order between different MonoBehaviour Updates: Core ticks and
  spends in the order the adapter invokes them.
- Afterburner costs25/heat20, temporary SpeedMultiplier2 for2 game seconds, CD10;
  multiplier affects desired speed and available force, not fitting base stats or
  order speed limit. Shield boost30/15 CD8; EMP35/25 CD12. Failed spend starts no
  effect/cooldown. Effects/timers remain Unity-side.
- Weapon spends after valid aim/sector/readiness, before cover/hit/miss resolution;
  all mounts and abilities retrieve one reactor on the owning ship.

Initialization edge found: old Awake unconditionally refills Energy=maxEnergy,
potentially resetting a prior Configure if ShipHealth.Awake executes first.
Stage 2 must initialize lazily once and never reset an already configured model.
This lifecycle correction is required for safe initialization, not a balance change.

Characterization tests added before replacement: additive/clamped totals ignoring
inventory; legacy weapon kinds/costs/cooldowns; capacity deficit/heat preservation,
zero-step/exact recovery/exact energy spend; hull/shield maximum-difference apply.
## Stage 2 implementation and validation � 2026-10-28

Core now owns ship/weapon calculations (`CombatStats.cs`) and the sole current
energy/heat/overheat state (`Reactor.cs`). `StatsDataAdapter` maps Unity definitions
and installed saved item values only. Existing calculator and ShipReactor entry
points remain facades; serialized tuning fields remain compatible. SetTuning
preserves legacy direct-field semantics without storing a second resource state.
Unity retains component application, permission/time input, effects, cooldowns,
aiming, damage and transition logging. No stage-3 systems were extracted.
Lazy initialization fixes Configure-before-Awake resetting already spent energy.

Validation: baseline characterization 45/45 EditMode on original production
formulas (initial 39-test run did not include the new tests; compiler errors in
reflection argument arrays were corrected before the valid baseline). Final Core
Release build: 0 warnings/errors; dotnet 40/40, EditMode 47/47, PlayMode 19/19.
PlayMode covers shared weapon/ability resource spending, resistance, failed-spend
cooldown atomicity, scaled Update, pause and preparation. Editor covers lazy
initialization and enum compatibility. Core covers thresholds, deficits, clamping,
ordered resource sequences and all weapon kinds. Existing fitting tests also pass.
The first PlayMode poll timed out; retrieving the same job confirmed 19/19 passed.

All three real ship_loadouts files match backup SHA256 values. Gameplay PlayerPrefs
(loadout and selected ship) match the backup; Unity session/test-run metadata changed
normally. Backup and post-run registry export remain ignored under Temp/SaveBackups.
Editor left outside Play Mode, not compiling; CombatSandbox scene clean. Generated
ProjectAuditor settings append was removed; prior work retained. No commit/push.

Remaining acceptance limitation: no new manual Game View HUD/fitting visual review
or physical-input smoke session was performed in this continuation. Automated
fitting and resource integration checks passed; do not report those as visual QA.
