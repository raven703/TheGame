## October 30 — individual weapon range / compact HUD (battle acceptance PARTIAL)

Started clean main at f9b3830; previous radar/support work is already in HEAD. No commit/push/checkout. Stopped Play before every edit. Added Core Range, closed catalog v3 range-only migration (starter version unchanged), deterministic equipment-distance policy and independent EMP8. Plasma10, pulse12, standard turret10, rail18; ordinary w1 laser12 without changing other parameters. Zero/missing custom range uses owning ShipWeapon.range. w5 rapid laser and w_laser_turret_demo are distinct legacy definitions: preserved at fallback (current player demo turret6), not silently renamed/rebalanced. Approved ranges serialized in standard hull inventory assets; damage/resources/sectors unchanged.

Player policy: fixed weapon with greatest nominal DPS, otherwise greatest-DPS turret; ordinal definition/mount tie-break. Existing explicit PreferredDistance wins, otherwise existing .7 maneuver factor. AI observation uses the same Core selector: interceptor prefers w4, artillery railgun (.8 working factor, plasma does not pull it inward), escort turret (.65 factor retained); missing role weapon falls back to primary real installed weapon. Persistent pass/safety/avoidance untouched. EMP previously had no independent actual-activation distance; now Core AbilityRanges.Emp8 gates both actual player/NPC activation and AI decision, preserving35 energy/25 heat/3s/12s.

Legacy FittingButton controller remains enabled for I and PreCombatPanel; only its Button/Graphics are disabled with raycasts off. Loadout tested via actual button event. V compact existing sprite in free right-panel slot, tooltip uses TacticalTooltip upper layer. Shield arrows/reset beside player shield, transparent parent non-raycast; W/S unchanged, desired percentage displayed paused. No bottom strip. Weapon card shows range; rings deduplicate actual per-mount ranges; geometry fingerprint includes range, no unconditional rebuild.

Backup verified byte-for-byte before first profile migration: C:\Users\user\OneDrive\Documents\TheGameProfileBackups\20261030-143106\ship_loadouts.json (64755 bytes). Existing atomic migration additionally creates its normal verified backup. Profile not manually equipped/rewritten; runtime-only diagnostic fitting discarded by RestartCombat.

Validation: new Core WeaponRangeTests8/8; targeted EditMode migration4/4 (identity/idempotence/custom preservation, prior placement/atomic failure regressions). Runtime CombatSandbox diagnostic: mixed plasma/rail/turret at target14 => false/true/false, actual Shoot resolved exactly1 shot; at7 all3 aim true; EMP14 refused without cooldown. Rings2 for10/18/10; role ranges18 artillery/10 escort. Paused shield arrow desired.75 and HUD75%. Loadout opened, hidden controller active,0 enabled/raycast Graphics. Screenshots weapon-fitting-720/1080 inspected:18-unit card fully readable; HUD720 and paused rings inspected. HUD1080 captured. Physical keyboard/mouse and several ordinary role battles NOT completed; interceptor/escort prolonged navigation and balance NOT accepted. No72-battle package. No guarantees about obstacle routes or simultaneous mixed fire. Final verification/status below reported by task response.

## October 30 — radar root cause and support-zone completion

Supersedes partial arena notes below. Local main retained; no commit/push/checkout, no scene/prefab edits or diagnostic NPC persistence. Authoritative BattleArena.operationRadius=90 (was36); independent of equipment range. Fifteen asteroids retained over larger circle. Waves retain configured spawnRadius14 capped by arena rather than spawning86 units away. Existing max zoom37.5 unchanged. Background follows camera and scales to viewport; edge/max-zoom screenshot inspected.

Root cause confirmed live: DotsContainer260x260 inside Radar Mask208x208 after responsive layout. Previous projection used child dimensions, so its edge108 exceeded mask104. Registry had live diagnostic NPC; overlay Canvas, scale1, alpha1, active Image, no distance filter. EnemyRadar now limits by ancestor Mask transformed into container coordinates, subtracts circumscribed icon radius plus outline reserve; both30x30 point and12x14 triangle accounted for. Range stays25; mask retained; no camera coordinates or distance size scaling. Removed stale distance-state dictionary. Exact inside/outside classification.

Runtime evidence: dedicated prefab NPC with AI/movement/weapon disabled and Rigidbody simulation off, manually registered.80 sequential Game View1280x720 captures:0.9/1.1/2/5/10 radar ranges ×8 directions ×selected/unselected. All active/alpha1/corners inside; all80 captured images contain red/yellow radar pixels. Logs/captures under ignored Tools/CombatEvidence/radar-*; representative radar-32 and79 visually inspected. Destroyed diagnostic object: pooled icon hidden. Selected target preserved by Return action. Exterior player MoveTo accepted110,10. NPC at100 steered83.29 inward through ordinary navigator, player timer0/combat unfinished.

Support: player only,10 scaled seconds outside90, reset inside88; preparation/combat-over resets; tactical pause freezes. Warning and Return button below pause strip at top center, text non-raycast, smooth alpha .6–1 and fully readable paused. Existing CombatManager guarded EndCombat handles defeat with exact reason, no artificial damage; result text avoids button labels. Return uses bounded48 obstacle-clear candidates; on failure keeps original point/no unsafe center. Player manual/queued/follow destinations no arena clamp; NPC retains clamp and role, recovery now runs local navigator rather than early bypass.

Validation:4/4 targeted EditMode (mask regression, projection, seeded field, timer hysteresis/reset). Runtime paused timer remained3; return reset0;9.99 no defeat then10.01 defeat, ship not destroyed; repeated failure/late wave notification cannot replace result. Restart via actual CombatManager restored preparation,0 timer, no diagnostics, radius90. Warning first screenshot exposed pause overlap; fixed screenshot support-paused-fixed.png inspected and readable. arena-edge-maxzoom.png inspected, background coverage intact. No physical mouse/keyboard interaction claimed; matrix exercised APIs and actual rendered frames. No full battles/balance run; long-duration NPC obstacle recovery and completely blocked return remain bounded-failure behavior rather than guaranteed route. Earlier two-test/36-radius limitations below are historical. Final ordinary CombatSandbox playing/preparation/fitting closed, no enemies.

## October 30 — MCP restored, radar/arena first implementation (PARTIAL)

Editor confirmed correct project Assets path and CombatSandbox, stopped before edits. Compilation succeeded sufficiently to run two EditMode tests and enter Play; compiling=false, Console0 errors. Core ShipOrders14/14, Unity radar projection + seeded field2/2 passed. Final observed normal preparation: radius36 (initial weapon range6 ×6),15 rocks, seed-2072890813. Screenshot Assets/Screenshots/arena-preparation.png1280x720 captured; not proof of far-target/edge acceptance. Play left preparing, fitting not explicitly queried. No profiles/equipment modified, no commits/push/checkout; no scene/prefab save.

Radar concrete layout discrepancy: panel208x208 vs nominal radius120 and center offset110, so edge triangles outside104 half-size. Now cap by actual dotsContainer rect and12 padding, center anchors/local scale normalized;2% distance hysteresis. Needs actual far NPC/mask acceptance, rotated container acceptance, cleanup dictionary retention.

Added Core BattleArea immutable circle in ShipOrders.cs; BattleArena Unity runtime adapter initializes once from initial player position/range, renders subdued circle, warning and return button. Asteroid field samples entire disk with empty1.25range start, enlarged pair gap for largest current ship; count15. No explicit clusters implemented. Spawner uses arena ring radius-4 and existing bounded collision checks. Orders clamp single/queue/follow/tactical evaluated destination; camera bounds derive arena. NPC hysteretic recovery steers inward without resetting role. Current limits: radius+2 margin is fixed reserve, not physical braking-distance calculation; SafePoint24 inward attempts falls back center without final obstacle proof; recovery bypasses local obstacle navigator; initial range reads weapon.range rather than definition baseline; manual accessible-point correction after clamp may escape circle. These MUST be hardened before declaring complete. No dedicated PlayMode tests, no1920 capture or far-target/edge smoke yet. Preserve working zoom/pan. Final preparation running.

## October 30, 2026 — pause pan / random field — PARTIAL, editor bridge disconnected

Started clean main; earlier work preserved. Exact blocker verified from Library/MCPForUnity/Logs/server-launch-8085.log: No Unity plugin reconnected to plugin hub. Process inspection confirmed main Unity PID2252 project path C:/Personal/Unity/TheGame/My project; other Unity processes are AssetImportWorkers, not second editors. get_active no_unity_session. No repeated polling, no second editor. User instructed existing editor MCP window Start Session/Connect. Cannot confirm current Play/scene state, Console or final preparation. Do NOT report Unity compile/visual acceptance.

CameraFollow max default30->37.5, min3; paused Pan world deltas, bounds accounting viewport, home button, unscaled return after resume. Existing PlayerInputHandler owns pause RMB click-on-release /7px drag/long-hold discard/Shift append, running FollowCursor untouched; TimeController already EndsGesture on pause transitions. Focus/UI/viewport cancellation preserved. Needs physical pause drag, anchor consistency when zooming during drag, boundary/return and both-res checks.

AsteroidField replaces four fixed rocks with up to15, size.6–1.6, bounded seeded System.Random placement in32x22 area; pair clearance plus ship exclusion,1200 default attempt budget. Preserves procedural textures, HP/mass squared scaling, collider, existing drift speed range (variant cycles). ActualSeed/ActualCount log. ShipSpawner tries48 collision-free positions (2.5 minimum clearance), skips on failure. Known wave spawn ring is14 from player; field does not reserve entire ring, instead per-wave collision validation. Production ship-specific collider after archetype swap still needs validation; conservative2.5 clearance not a formal guarantee for arbitrary large ships. Seed deterministic only with same initial obstacles/ships. Added Unity Editor seeded placement/pair gap test NOT RUN, radar projection test also NOT RUN.

External dotnet build Assembly-CSharp.csproj succeeded0 errors49 obsolete-API warnings before final text-input guard/test addition (last guard trivial but final editor compilation pending). git diff --check clean then. No profile/equipment writes, commit/push/checkout. Scenes/prefabs not edited/saved. Requirements not fully accepted: no new PlayMode gesture tests, no Unity tests or real mouse, no measured actual field count, no screenshots/Console. Prior TAB/buttons/zoom/radar acceptance remains blocked. Continue with reconnect, compile, targeted tests, regular CombatSandbox preparation; avoid full combat batch.

## October 30, 2026 — TAB / zoom / radar / toggle readability — PARTIAL, Unity blocked

Started clean local main, previous implementation preserved. No commit/push/checkout, storage/physics/balance/tempo untouched. Existing TargetSwitcher now ignores pointer-over-UI for TAB, respects text/fitting, filters Available, keeps registry order; temporarily suppresses EventSystem navigation for Tab and clears UI selection. Existing weapon lock already returns before UpdateLock at timescale0. Existing CameraFollow adds bounded wheel base zoom and unscaled smoothing; frozen speed multiplier on pause, no competing camera writer. All UI blocks camera wheel conservatively. Parallax implementations already grow background quads with ortho size; not visually verified.

Shared turquoise ToggleVisual used for tactical/orientation/speed/module-priority and sector mode; Selected color equals model-derived normal state, hover weaker. Existing ability numbers retained. Radar reuses image pool, switches sprite to runtime triangle beyond range, preserves red NPC and highlights pending/locked target, clamps icon inside edge; no new detection. Near points now use padded radius too for continuity. Missing player hides pool; registry handles removal. Range labels юн., fitting/movement speeds юн./с; resource units preserved. Added EditMode eight-direction projection test in CombatCatalogMigrationTests (NOT RUN).

Actual verification: Core ShipOrders/TacticalPause 14/14; diff --check passed. Unity get_active retried and still no_unity_session; cannot claim compilation, Console, EditMode/PlayMode or screenshots. No real TAB/wheel events exercised. User must reconnect existing editor Window > MCP for Unity > Start Session/Connect HTTP 127.0.0.1:8085/mcp. No second editor launched. Final normal CombatSandbox Play/preparation state UNKNOWN; no scene/prefab edits saved. Remaining: compile, run affected Unity tests plus new radar test; physical navigation/scroll/pause checks, bright selected radar and destroy lifecycle; both resolutions, max zoom background. Camera bounds default3–30 need visual acceptance; UI collision/tooltip and earlier smoke gaps remain. Do not claim complete.

## October 30, 2026 — independent orientation / tooltip follow-up, UNITY BLOCKED

Read current handoff; local main initially clean (earlier changes already present). No commit/push/checkout. MCP server debug responds at 8085 but get_active repeatedly returns no_unity_session: editor bridge NOT restored. User instructed to reconnect existing editor in Window > MCP for Unity, Start Session/Connect HTTP; no second editor launched. Cannot claim latest Unity compilation, Console, screenshots, pause physics, smoke encounters or final preparation state. No scenes/prefabs saved while state unknown.

Added Core OrientationTargetId with explicit assignment and loss validation, separate from tactical TargetId. Unity adapter captures selected pending/locked target only on button press; register no longer clears other target identity. Manual moves/Stop clear facing target; loss returns course without touching route. Tactical changes preserve explicit target override. UI label На цель and required availability hints. Tooltip now root-Canvas overlay, 340-unit max width, preferred height, common panel/shield envelope + gap, left fallback, no raycasts. W/S shield buttons use same popup. Needs visual collision check with other HUD blocks at both resolutions; arbitrary crowded layouts not accepted. Added Core route/revision/speed/facing lifecycle test. Targeted Core 25/25 passed; diff --check passed before final documentation/lifecycle edits. All Unity acceptance gaps remain; do not report finished.

## Continuation — October 30, 2026 — Unity validation BLOCKED

Added pause-strip existing ability sprites/activation numbers with availability tooltips, separate movement sprite/name/count, late-frame projection. Engage button now requires selected available target and highlights current order; target-facing explains missing order target. Fixed installed weapon hover mount selection and clear detail text on cancel/close/ship selection. Weapon details explicitly explain nominal DPS exclusions; card font reduced to 12 for six/seven lines. Interceptor fixed pass now offsets laterally using combined radii instead of crossing target centre. Added geometric pass-clearance and opposing avoidance tests. Affected Core tests 12/12; diff --check clean.

MCP HTTP initialization renewed session successfully, but editor calls consistently return no_unity_session; refresh wait timed out. Unity process is responding and window names CombatSandbox, but current play/preparation state is UNKNOWN. No Unity compilation, EditMode/PlayMode run, screenshots or physical-click acceptance can be claimed for this continuation. Existing untracked Assets/_Recovery/0 (1).unity and meta left untouched. Profile not intentionally touched. No commit/push/branch switch. Prior acceptance gaps remain except implementations explicitly listed above. Check card overflow, pause tooltip positioning, avoid-side stability and radius-dependent interception in Unity once bridge reconnects.

## UI clarity / NPC passing — October 29, 2026 — PARTIAL, acceptance outstanding

Started clean local main, no checkout/commit/push; MCP confirmed already outside Play before edits. ShieldArcs previews player desired distribution only while paused, using total filling, without model writes. HUD shows total shield HP and distribution; separate status reduced to transfer caption and retained buttons. Fitting removes per-gun summary, sums common WeaponStatsCalculator.NominalDps for installed nonempty slots, labels shield/energy regen separately, adds installed/inventory hover details in fixed bottom-right block. Profile intentionally not migrated/edited by task code.

Core interceptor attack now retained waypoint beyond target, retained through early Exit. Observation includes own velocity and physical radii; safe exit uses closing speed. Artillery/escort safety thresholds and bounded local ship avoidance added (Core math, Unity overlap queries). NOT battle-accepted: avoidance stability and size-dependent waypoint clearance still require smoke scenarios. No balance changes intended.

Validation so far: affected Core 10/10 including new retained pass test, clarity-targeted.trx. git diff --check passed. MCP console read returned 0 errors after refresh request; this is NOT proof of finished compilation or visual acceptance. No EditMode/PlayMode acceptance, no 720p/1080p screenshots, no regen equip/remove runtime check, no numeric production DPS measured. Pause strip still refers to numbered icons in existing panel rather than displaying queue icons itself; movement icon not implemented. Module card stacking limits, stale lifecycle handling, DPS tooltip and layout acceptance remain open. Runtime preparation final state not verified. Do not report this request complete.

## Tactical pause / directional shields — current task (supersedes S=Stop)

Started local main clean; preserved existing committed flight/input integration. No commit/push/checkout. Unity stopped before edits. Implemented Core TacticalPause (exact resume scale, unique ordered ability commands with target IDs and destructive Drain), Core DirectionalShield owned only by DamageState, Unity adapters/UI/arcs. Current controls Space pause, W forward shield step, S rear shield step, X Stop. Existing generated CombatControls has Space and speed presets, no S Stop binding; only old direct sKey handler changed to xKey. No new speed UI. Pause uses existing TimeController; ends gesture, allows clicks/routes/tactics, no FollowCursor while paused. AI UpdateOrders explicitly blocked at zero scale. Weapons already block. UI queue buttons remain clickable during pause, show number plus resource/cooldown/target state. Existing Use* path dispatch checks resources/modules/target again; EMP snapshots runtime ID, does not silently retarget. Drain once then log failure. Normal nonpaused EMP path remains old locked-target semantics.

DamageState.Shields is sole shield HP owner. Legacy currentShieldHP getter sum; setter clamps and shares by capacity. Directional hits use spawnPos captured in ShipWeapon.TryFire, dot with actual hull forward; >=0 front includes exact side boundary. Legacy undirected methods front only. Plasma/Railgun remain same special fractions applied to hit side, bypass independent of opposite HP. Hit delay3s independent even empty. Transfer df=clamp(desired-f,-dt,+dt), full0→1 in1sec; overflowing HP from shrinking capacity moves across with conserved total. Regen Rf/R(1-f), no unused spill, split time at delay/transfer boundaries; operational guard. Boost30 restored by deficit, hull10 bypass, delays unchanged. Same maximum application early-outs to avoid redistributing HP. New battle Initialize 50/50; no combat HP persistence added.

CatalogVersion2 adds s1 shield25/regen5, s2 shield40/regen2. V1 known items only shield fields change; mass/other fields preserved. Unknown IDs untouched. Backup BEFORE migration outside Git/Temp: C:\Personal\UnityProfileBackups\TacticalShields-20261029\ship_loadouts-before.json SHA949EFB04CA44E23C19939CAA03B50276AAA2402BE02B0321050484B60B01DBBF. Production unchanged through tests (verified hash). Separate controlled migration outside Play via detached Parse/EquipmentFileStore:23 items, IDs sequence checked and placement comparison no diff, starterVersion1; no grants. Final SHA37CAEC5268963E7FD5E872CD0A07A59E88C2B430A22C8044FBC2BD29CD626E9A. Known shield stats confirmed. Do not restore backup over migrated profile.

Tests: targeted Core4/4 then FULL dotnet107/107 Passed, final rerun107/107 after same-max guard (DotNet/Game.Core.Tests/TestResults/tactical-release.trx). Two legacy expectations changed to preserve untouched rear HP. EditMode25/25 jobf2a5b8c5a9574bc893dd508679d6ba9f (controls+migration, including identities/holes/custom). Initial PlayMode4/5 failed fixture setting current1000 before max1000: setter now legitimately clamps. Fixed fixture order, final affected PlayMode13/13 job4a5121bba2eb43b0a45a5c1bfc9dca14 includes pause plan/one-shot Boost/exact1.75, fire/EMP/mounts/UI, ReactorAdapterTests, FittingInteractionTests, WeaponRangeTests. Did not run full Unity suite or72 fights/IL2CPP. Last tiny Unity changes after13/13: arc radius follows late HullVisual, button font no-wrap, preserve nonpaused EMP call, compiled and real-scene smoke; no test running while editing.

Visual inspected real GameView1280x720 preparation and pause screenshots Tools/CombatEvidence/tactical-shields-final.png and tactical-pause-final.png. First screenshot exposed radius computed before HullVisual; fixed by reading current model bounds. Final arcs visible clear of hull,8 enabled at50/50. Shared ShieldArcs creates16 reusable half-lines once (up to8 visible), one material/ship disposed on destroy, no selection sprites/colliders/raycast; NPC dim unless selected or hit, hull color unchanged. TacticalShieldPanel is attached to existing layout by TimeController; shield block left of right panel to fit720 high, pause strip nonmodal. Existing sector toggle retained (bottom caption partly overlaps near shield block, predates/new layout needs polish if desired). No scene/prefab edits required: runtime construction matches existing UI; runtime not saved. TMP auto glyph asset regenerated during tests and restored only initially-clean generated file. Unity regenerated slnx left as generated.

Actual-scene synthetic pause smoke: scale0, resume1.75, desired.75/current.5 frozen,8 enabled arcs; resume returned1.75. Final normal RestartCombat, fitting closed: Play=True prep=True enemies=0 fitting=False, console0 errors confirmed in flight-final-state/flight-console. Preparation reset uses existing scene lifecycle; no global tempo changes introduced. Physical keyboard/focus and real fighting visual smoke NOT automated. Manual: start fight, Space, set queue/target/tactics/shield plan, queue/cancel abilities; resume; W/S and X; frontal/rear damage and regen; selected/dim NPC arcs. No claim exhaustive real-battle acceptance.

Docs tactical-controls.md and tactical-pause-shields.md updated with formulas/compatibility/migration. Follow-up limitations: conservative SpriteRenderer selection bounds unchanged; ShieldArcs currently enumerates sprites and recomputes vertex positions each LateUpdate (no GameObject/Material/Mesh allocation per frame, but child array allocation remains); rendering uses thin LineRenderers (at distant zoom can alias). Delay timers intentionally freeze with destroyed shield module and resume on repair. Large-step changing-capacity regen integrates linear shares at event boundaries; framewise equivalence under intermediate full-capacity saturation not separately tested. No telemetry/balance tuning.

## RMB gesture / model selection — October 29, 2026 (supersedes prior RMB semantics)

Local main retained, no commit/push/checkout. Speed/damping/zoom changes preserved; Gizmo untouched. Core OrderMode and adapter ShipOrderMode append FollowCursor (existing serialized values unchanged). Begin/Update/End API: one waypoint, update does not increment NavigationRevision or reset navigator. Release becomes MoveToPoint, no automatic tactics. RMB always uses cursor world point; explicit EngageOrder button added to tactical header. Shift appends once and cannot follow. Stop/button mode change cancels gesture, same held button cannot resume; focus/disable/pause/preparation/fitting/ship replacement/destruction end it. UI/viewport freeze last valid point; UI-origin press cannot become a world gesture. Mouse projection uses current camera ray onto z=0.

Selection replaces physical OverlapPointAll with active enemy registry + child SpriteRenderer bounds projected to screen, 12px tolerance. No physics changes. Visible-bounds hit preferred, then screen center distance, exact ties retain registry order. Lines/particles/canvas excluded; conservative bounds, not alpha-mask. Existing model SpriteRenderers under ship are assumed model parts: future sprite-based effects must not be placed as ordinary model renderers without classification. Actual existing runtime weapon effects are LineRenderers. One-click weapon path already had no confirmation flag: SetTarget starts timed lock, Update completes then Shoot; repeat selection is idempotent. Thus no proven weapon-level double-click bug; exact collider misses explain unreliable first selection, not evidence of a second-confirm requirement. Frame pending color interpolates amber to red; locked stable red; existing HUD owns lock progress, no second selection system.

Validation: Core ShipOrdersTests 12/12 Passed (new test proves unchanged navigation revision across 100 updates); EditMode ShipOrderTests 15/15 Passed job9e0b9e4559264f8f97294e556612087d (gesture, UI-origin, frozen UI point/release, Shift, Stop/focus, pending preservation, child sprites and 11px accepted/14px rejected at orthographic5/15). PlayMode targeted4/4 Passed job0e87734aece84968bd5f5449689fefd0: one IssueClick begins lock and auto-fires during FollowCursor, EMP still blocks; single asteroid avoidance, Stop physics, real UGUI raycast. No full suite.

Real CombatSandbox synthetic gesture smoke: FollowCursor -> MoveToPoint points=1 dest=(0,12) on release over UI. This ran during preparation (physics intentionally stationary), NOT proof of real-scene follow motion/avoidance/zoom. Physical mouse and focus not automated. Manual checklist: start ordinary battle; click/hold RMB while moving cursor at low/high speed and zoom; pass asteroid; release over UI/outside view and verify final stop; Shift queue; S while held and focus loss; click small NPC once, observe progress then fire; repeat click keeps progress; manual flight keeps fire subject to sector/range/resources. Real-scene visual checks remain open.

Final MCP evidence reread: Play=True prep=True enemies=0 fitting=False, console0 errors. Active ordinary CombatSandbox; scene clean before Play. Runtime objects were not saved. TMP fallback font was expanded automatically during UI tests; generated changes removed (initially clean). Existing asset whitespace warnings predate this task. Controls updated in tactical-controls.md. IMPORTANT: final production profile SHA256 is949EFB04CA44E23C19939CAA03B50276AAA2402BE02B0321050484B60B01DBBF, different from earlier flight checkpoint; unchanged profile cannot be claimed. Input/order changes contain no save/grant code, but scene lifecycle ran existing persistence. No profile restoration attempted. Unity also regenerated My project.slnx; left untouched rather than assume original content.

## Flight / selection / speed zoom — October 29, 2026

Current main retained; no commit/push/switch. Final state verified via ignored Tools/CombatEvidence/flight-final-state.txt: CombatSandbox Play=True, preparation=True, enemies=0, fitting=False. Console errors0 (flight-console.txt). Profile hash4B80BC09066D1D3ECC90A6F2EB5CF384863CFFE685C5B40F9178801C9EBC8D2F unchanged between this task's hash checks; it differs from previous handoff checkpoint and must not be represented as equality to that historic checkpoint. No profile writes/grants added by flight work.

Selection investigation: MoveTo/Stop already do not write ShipWeapon selection; no actual movement-triggered reset reproduced. Concrete competing assignment was TargetSwitcher.Update implicit nearest-enemy acquisition. Removed implicit acquisition, explicit Tab retained and UI/modal guarded. UI frame/HUD previously ignored pending selection (and HUD showed fallback enemy); now display pending weapon selection, not tactical target. Existing PlayerInputHandler remains sole mouse handler; IssueClick rules unchanged. EditMode tests extended to verify pending progress and completed target through MoveTo/append/Stop. Runtime IssueClick smoke confirms target survives movement/append/Stop; red NPC color RGBA(.694,.068,.068,1) verified unchanged. Physical mouse end-to-end smoke incomplete; do not claim reproduced root cause of reported reset.

Flight: proportional velocity controller previously ignored linearDamping1.2. Open-space fixed-step measurement: Sidewinder speed6.15384 vs8, Cobra3.88885 vs6, neither reaches90% in10s. Stop to<.1: .98s/1.84961 units and1.04s/1.46777 units. Added desired-velocity damping feed-forward inside existing directed elliptic thrust budget (no velocity clamping/writes). Mass, angular alignment, speed limits, avoidance/final braking and destroyed engine guards retained. Versioned standard hull-only FlightVersion2: Sidewinder max10/force19; Cobra max8/force28.5. Assets saved outside Play, runtime OnEnable upgrades known standard IDs, custom IDs ignored; no weapon/module migration version bump or grants. Sidewinder real physics10, t90=1.62s, Stop1.20s/3.71436units. Cobra real applied definition test7.916 after8s, t90=2.04s. Earlier candidate force29 reached8/t90=1.90 and Stop1.18s/3.03210units; final28.5 braking distance was not separately measured. Actual equipped ships may saturate thrust against drag and cannot be promised naked-hull acceleration.

CameraFollow is sole zoom writer: actual Rigidbody2D speed smoothed .15s, non-boost equipped maxSpeed denominator; <=20% base size, cruise x1.35, capped x1.6 at2x speed; SmoothDamp out.5/in1s. Base camera size captured, external/manual changes treated as user base. Pause and fitting freeze automatic zoom. Existing two parallax layer implementations enlarge quads to cover viewport. No NPC materials/sprites/colors edited. Real dynamic zoom, background-edge and pause/modal visual acceptance still incomplete; runtime battle smoke player was already stopped/disabled by battle outcome when long-run sampled, so not proof of zoom transition. No claims all requested manual checks complete.

Tests: EditMode ShipOrderTests+CombatCatalogMigrationTests22/22 Passed. PlayMode filter unexpectedly ran full25:24 passed, new speed fixture failed because movement cached no health and mass remained1. Fixture corrected by wiring late-added health (production prefabs unaffected), targeted XML20261029-132355-962 Passed1/1, both acceleration assertions preserved; other24 unchanged not rerun. Prior full result XML20261029-132117-525. No tests edited while running. MCP targeted job reported initialization timeout despite authoritative Passed XML. Unity compilation successful. No standalone build or72 battles.

## Ordinary CombatSandbox integration — October 29, 2026 (supersedes demo priority)

User authorized production profile upgrade; main branch retained, no commit/push/switch. CombatSandbox scene saved outside Play Mode with runtime ShipSpawner role configuration and hull references: waves Interceptor; Artillery; Escort; Interceptor+Escort. NPC use existing CombatDemoLoadouts definitions (runtime despite historical name), shared ShipStatsCalculator.ApplyNpc, EnemyAI role pilot and ordinary paid ShipAbilities. No player inventory access for NPC, no new demo/profile/screen. Editor-only ApplyExperiment is not used by ordinary combat. Runtime Windows paths are compiled into Assembly-CSharp; standalone Windows executable build/launch was NOT performed.

Production backup (outside Temp, excluded from Git): C:\Personal\Unity\TheGame\My project\Tools\CombatEvidence\ProductionUpgrade-20261029. JSON copies verified SHA256; PlayerPrefs registry exported. Original current JSON hash 9871051137244C9782F4246FF34F85693DEDD30626AB8B935FB44DB50C7B96A2. This is the current authorized checkpoint, not the lost historical baseline.

One-shot combatStarterVersion=1 persisted through Unity/Core adapter and Core snapshot copy. Grant checks category-compatible stable IDs across free inventory and every ship. Existing records untouched, no auto-equip, no replenish after version stamped. Migration + grant use one atomic EquipmentFileStore replacement before publishing ledger. Failure-injection test verifies original bytes and pending cleanup. Known-standard migration remains CombatStandardDefinitions.Upgrade balanceVersion=1; unknown IDs untouched.

Actually added: lightweight-engine, maneuver-block, radiator, capacitor, w4 pulse laser, laser-turret. Already owned: s2 heavy shield, w2 plasma, w3 railgun. All 17 previous items retain every field and instanceId; placements/empty slots unchanged; total23. Existing profile had already received versioned standard parameter migration: no previous item fields changed in this authorized upgrade. New records use unchanged catalog values. After hash3755316138E82B78476077C6F554ABBC876B9118EC1F1492D2B2AAACDC899631 remained identical through restart/reentry/fitting. No presets rewritten.

Fitting standard descriptions now derive from actual catalog effects; 1280x720 HUD/fitting screenshots inspected. Long statistics moved into masked scroll at readable14pt rather than clipped/shrunk text; reverse/lateral stats use calculated values. Existing unrelated UI/selection settings preserved.

Checks: Core Release build0 warnings/errors, Core tests102/102; affected EditMode21/21 XML20261029-124818-265 (MCP initialization timeout was false negative, authoritative XML Passed); affected FittingInteraction PlayMode3/3, latest1.4544937s. Earlier new fixture initially failed because raw JsonUtility null slot becomes empty FittingItem: test now uses production Parse normalization, assertion retained. No full regression or72-batch. Real CombatSandbox smoke confirmed roles/weapons: Interceptor w4/w4 Approach/Exit; Artillery w3/w2 Recover energy83.67/155; Escort w4/turret Orbit and resource spending; W4 both roles. Defeat panel observed in unmodified battle; victory and all wave transitions verified with diagnostic lethal damage, temporary runtime-only player HP and shorter pauses. Restart reload clears all diagnostic changes. This is not an unassisted four-wave victory/balance test. Ability code audited to call ordinary paid methods; detailed per-ability NPC telemetry not captured.

Final: ordinary CombatSandbox Play Mode, preparation=true, fitting=false, enemies0, role configuration=true, scene dirty=false, Console errors0. Windows player build/run remains unverified. Backup and evidence available outside Temp. Existing historical full-suite limitations remain; no claim full regression clean.

# Current handoff — October 29, 2026

CURRENT PRIORITY: interactive playable demo, 72-fight batch deferred by user. CombatExperiment now defaults Interactive=true: A/B role selectors, AI/player-A toggle, fixed-cover toggle, start/restart, elapsed time/winner, ship stats and paid ability buttons. Player right-click issues normal MoveTo; weapon targets B automatically; A AI disabled in player mode. Scene Assets/Scenes/CombatArchetypesV1.unity, menu TheGame/Combat v1/Create isolated experiment scene, main camera tagged. Normal build starting scene/settings untouched.
Latest smoke reports outside Temp: Tools/CombatEvidence/Demo/20261029-120851. AI interceptor/escort A wins33.48s; interceptor/artillery B wins21.80s; player-controlled interceptor vs interceptor with cover B wins24.41s, player fired2 shots and movement/paid boost verified. All roles enter and finish fights; not a manual player-vs-each-role acceptance or balance result. Targeted EditMode catalog/demo tests8/8 Passed, XML20261029-121227-355.xml. Main production save hash987105... unchanged through demo session. No blind save restoration.
Known limitations: Editor-only demo, IMGUI basic presentation, auto target B rather than full tactical HUD; no two-resolution visual acceptance; original interceptor pass-through and artillery facing still need manual review. Do not claim full regression or72-batch completed. No balance tuning/commit/push. Final intended state Play Mode demo selection/preparation.

STORAGE AUDIT: main save no longer matches original B0386A (now987105...). Line comparison to outside-Temp checkpoint-20261029 has no differences, so predates latest fixture runs. Preserved current copy Tools/CombatEvidence/save-after-tests.json; no blind restoration. Audit earlier production migration/access before further combat execution or storage-safety claims.

NEWEST: victory-chain fixture stabilized without bypassing shots/damage: explicit isolated gun and accuracy, no movement/catalog dependency. Three sequential framework XML Passed (0.299036/0.244387/0.255382s). Added CombatTestEvidence Editor callbacks preserving every run XML+log under ignored Tools/CombatEvidence/TestRuns, outside Temp. Full subsequent PlayMode completed23/24, failed AutoFire lock/EMP test from random misses; fixture-only accuracy/evasion made explicit, targeted rerun Passed2.080163s. Full suite after that last fixture change pending. See latest test-localization paragraphs in combat-archetypes-v1.md. No72-batch or balance changes. Editor closure considered external, not confirmed crash.

LATEST: Read the latest-investigation subsection of combat-archetypes-v1.md first. Fresh PlayMode retry ended in full Unity shutdown; NOT passed. Editor relaunched. TimeController null enable/disable guard fixed, but not yet validated by a completed Unity run. Prior run had compile/reload during execution: do not edit files during tests.
Temp was cleared on shutdown: original save/prefs backups and smoke reports are gone. Shutdown log copied outside Temp into Tools/CombatEvidence/playmode-shutdown.log. Original gameplay-prefs baseline unavailable; no registry restored. Main save still matches original documented SHA256. Core build separately succeeded (0 warnings/errors), dotnet tests separately passed 102/102.
72-fight matrix NOT started; existing loop does not implement requested matrix. Remaining user checklist is unfinished. Do not claim completion, recovered original prefs baseline or final preparation scene. No balance tuning, commit, push or branch switch.

See the verified update at the top of `C:\Personal\Unity\TheGame\My project\docs\combat-archetypes-v1.md`; the older handoff below is superseded.
Core 102/102; EditMode 69/69 after latest refactor. Full PlayMode runner aborted at ExitPlayModeTask after 21/24; editor confirmed stopped, orphan status cleared.
Isolated scene created and valid three-fight smoke completed; reports in `C:\Personal\Unity\TheGame\My project\Temp\CombatArchetypesV1\Reports\20261029-105644`.
Fixed zero weapons in initial invalid smoke: Editor-only ShipStatsCalculator.ApplyExperiment shares stat application but skips migration/persistence, preserving production preparation guard.
EnemyAI role mode no longer resolves legacy Player tag in Awake. Editor is stopped, experiment scene active.
Three save files still match backup. Full PlayerPrefs differs; inspect structured values before making any game-progress equality claim. Never overwrite all prefs to hide Unity session differences.
Next: add lifecycle/error/telemetry tests, fix independent seed coverage and per-weapon geometry metrics, rerun Unity tests, then batch/visual checks. Do not call the feature complete or balance validated.
No commit/push/branch switch; preserve all pre-existing settings changes. Generated test-runner scene remains untracked; inspect before removing. New experiment/tool/scene metas must be retained.

# Historical handoff: combat archetypes v1 — partial, blocked on Unity verification

User requested A/B/C/D vertical slice, no commit/push/branch switch. Current main
HEAD e0991fa. NOT implemented end-to-end. Read combat-archetypes-v1.md before continuing.
Initial tree had user changes in render settings, TMP fallback, PlayerSettings,
EditorBuildSettings and untracked Build Profiles; preserved, never reverted.
Verified copied storage and exported registry under ignored
Temp/SaveBackups/combat-v1-20261029-121549 (name uses machine clock).
Fresh independent baseline 89/89. Added Core catalog and bonus/axis math plus
8 tests: full dotnet 97/97, Release zero warnings/errors. Unity adapters still
use legacy values; no migration, ship tuning, new AI, demo or stand implemented.
Unity refresh timed out after 60s; subsequent read_console reported session not
ready/ping unanswered. Final independent shell inspection exceeded 300s and was
left running by tool; output at
C:/Users/user/AppData/Local/Temp/cline/proceed-while-running-1793265975405-uvqypo3.log.
Do not claim final save/prefs comparison, Unity compile or visual checks passed.
No play session started. Next: recover/check Unity responsiveness, inspect console,
confirm generated .meta for new Core script, finish A verification before B.

# Previous handoff: stage 6 audit

## Final acceptance and commit preparation

User reports manual acceptance completed with no observations so far. Editor is
outside Play Mode, CombatSandbox clean; no runtime objects saved. The final root
solution inspection found DotNet entries reintroduced/rearranged since the audit;
its pre-regeneration copy was preserved in ignored Temp/Stage6Commit, then SyncVS
regeneration removed them through the existing callback. No source/API changes.
The user authorized committing the completed extraction and pushing main normally.
Historical statements below about pending manual acceptance and leaving Play Mode
running are superseded by this acceptance. Automated results remain unchanged.

Read the Stage 6 section at the top of architecture-refactor.md first; it supersedes
historical next-stage instructions. main/cbb4478 retained; no commit/push/checkout.
All stage-4/5 local work preserved; no scene/prefab diff retained.

Stage 6 changes: Core starter identity factory supplied by adapter (no hidden Guid),
three independent boundary/identity tests, production ledger access guard seam,
fitting fixture failure-safe cleanup and shortened real-shot/wave victory integration.
No further proven duplicate removed; serialized compatibility fields intentionally kept.
Core Release 0 warnings/errors; dotnet 89/89 stage6.trx; EditMode 61/61
0eb48e4d3cef4a7fa644684d81237c9e; final PlayMode 23/23
54aa9f0e161e48d7ac4588acfdf0c227, all final failed/skipped=0.
Two initial new battle-fixture attempts failed (not hidden); corrected setup and
facing, targeted 1/1 then full 23/23. See audit for job IDs/limitations.
5 scenes/8 prefabs scanned without saving: missing/broken 0/0. diff-check exit 0.

Temp/SaveBackups/stage6 holds JSON baseline and registry exports; all three real
JSON files match baseline after tests and isolated preview/restart. Registry differs
only in three Unity session metadata values. No stage-6 real-save restoration needed.
Preview memory ledger/presets installed before components via temporary no-domain-
reload; original editor settings restored and script reload clears all substitutions.
Final normal CombatSandbox Play preparation is intended for user manual checks.
Do not save runtime objects. Final manual session uses real original profile: user
equipment/preset/ship changes may now persist normally (preview overrides removed).

1280x720/1920x1080 HUD/route/tactical/sectors and fitting images captured AND viewed
under Temp/Stage6Acceptance. Actual RestartCombat produced new player identity,
empty queue/registry, no target, angle zero, full energy/zero heat, preparation.
Completed shortened battle only: 2x1 stationary 1-HP enemies, high fallback damage,
API orders/target, ordinary Update shooting/destruction/waves/victory. Not full
unchanged battle or physical input. Physical V/mouse/tactical/abilities and C# Dev
Kit root solution GUI loading remain user checks; exact checklist in architecture.

# Historical handoff: stage 5

Read orders-ai-core.md first. Stage 5 production extraction connected: Core
ShipOrders owns order state; PilotDecisions owns four personality decisions.
Unity facade resolves per-ShipHealth GUID identities and retains navigator/physics.
ShipMovement and ShipLocalNavigator formulas unchanged. No stage 6/commit/push/checkout.
Baseline main/cbb4478; all previous stage-4 changes retained.

Core Release 0 warnings/errors; dotnet 86/86 (stage5.trx); full EditMode 61/61
3494931a076c491390ad604f0cbf3b33. Full PlayMode 22/22 initial
af5216acbe1f4cfeaa2a70e05585bfec and repeat c885a1ad65f045038031c0db109723f4.
An intervening job 2eac28156cf543448b5b0a216c12db32 stalled after two tests,
editor had exited play; cleared orphan and retried, not counted as passing.
Final full PlayMode job 22c1f5f20f144d0faab9b933ee2b22c0 passed 22/22 after forced
asset refresh confirmed compiled fitting preset isolation methods.

Isolation: existing ledger override BEFORE fitting/ship components, profile null
and restored; added narrow LoadoutPresets Read/Write delegate seam. EditMode preset
tests and both PlayMode fitting tests use memory and restore delegates. Initial
PlayMode wrote one temporary isolated-* preset key; a repeat before forced refresh
also left a temporary key. Both exact keys deleted, no gameplay registry restore.
Final registry comparison after latest job: ONLY three Unity session metadata keys
changed; no added keys or gameplay preference changes. All three real JSON hashes
match baseline; final byte-hash comparison with backups also equal.
Temp/SaveBackups/stage5 has baseline manifest and exports. Do not claim earlier
runs were unchanged throughout: temporary preset writes above were corrected.

Game View 1280x720 and 1920x1080 captured AND viewed, original Cobra equipment,
API-issued two-point route in preparation. Temp/Stage5Acceptance/stage5-720.png and
stage5-1080.png. Physical V/mouse/tactical input and C# Dev Kit UI remain manual.
Final editor STOPPED, CombatSandbox restored, sceneDirty=false; generated test scenes
removed after tests. No runtime previews saved. Final CombatSandbox validation:
0 missing scripts / 0 broken prefabs; Console 0 errors. Not an all-project asset audit.
Stage 5 complete with physical-input/IDE checks still manual; do not start stage 6.

# Historical handoff: stage 4

## Acceptance follow-up completed (no stage 5)

Read the acceptance follow-up at the end of mount-geometry-core.md. Added one
isolated PlayMode lifecycle test; targeted lifecycle/combat/cover/visual suites
passed (4/4 plus lock/EMP 1/1). Production code unchanged in this follow-up.
720p and rotated 1080p Game View screenshots actually captured AND viewed.
Final CombatSandbox intentionally LEFT IN PLAY MODE, preparation, sectors ON,
original Cobra definition/equipment loaded. Do not save runtime objects.
Real save files match pre-acceptance hashes after restoring a documented temporary
preview-ship persistence side effect. No global registry restore. IDE UI loading
and physical V/mouse interaction remain manual checks, not failed automated tests.

Read mount-geometry-core.md first. Stage 4 extraction connected; geometry owns
angles by stable mount ID, adapters and fitting/visualizer share Core math.
Actual baseline HEAD cbb4478/main clean; prior stages already committed by user.
This task has not committed/pushed/switched branches. Root Unity solution and
DotNet/Game.Core.sln separated, generation callback installed. No stage 5.
Latest evidence and remaining acceptance limits are in mount-geometry-core.md.
Historical stage-3 git/next-stage statements below are superseded.

# Historical handoff: stage 3 - 2026-10-28

Stage 3 production extraction is connected. Read combat-core.md first for exact
ownership, shot sequence and known limitations. Core DamageState owns all hull,
shield and module HP; ShipData/ShipModule forward old APIs. Core WeaponCombat
owns stable-ID cooldowns, hit chance and one synchronous spend with existing
Reactor. Asteroid keeps Physics2D and a Core CoverState. No stage-4 geometry,
aiming or AI extraction. Historical sections below are superseded.

Final checks: Core Release 0 warnings/errors, dotnet 60/60, EditMode 56/56
(fccaf039dec641939ea0f8f039c6883d), PlayMode 21/21
(cbe92d5c52294bda8545bec224beaf4f). Interrupted/failed attempts documented in
combat-core.md; no old test weakened. Console final read 0 errors/warnings;
pre-existing CS0618 rebuild warnings remain. Loaded CombatSandbox validation:
0 missing scripts/broken prefabs. Save hashes and gameplay PlayerPrefs unchanged.

Game View HUD/fitting and overheat reviewed at 1280x720; captures ignored under
Temp/Stage3Captures. API/handler setup only, NOT physical mouse verification.
No exhaustive wave progression scenario or whole-project prefab audit performed.
Editor outside Play Mode; no scene/prefab changes intended. main HEAD unchanged,
all stages 0-3 uncommitted; no checkout/commit/push. Stop here, not stage 4.

# Stage 1 handoff — 2026-10-28 (supersedes older state/next instructions)

- Stage 1 implemented and connected. Stop; do not start stage 2 without instruction.
- Local main, HEAD df8d21c2c05844b34e08cf4d1ba887718f28c0ed unchanged. No commit,
  push or checkout. All uncommitted stage-0 changes preserved.
- Sole ownership/placement ledger is now `Assets/Game.Core/PlayerEquipment.cs`.
  Unity PlayerEquipment/ShipLoadoutManager are facades. EquipmentDataAdapter owns
  immutable full saved item records only (no placement); EquipmentFileStore owns
  atomic I/O. Core has no Unity/JSON dependencies; live refs only netstandard.
- Core operations: CanMove/Move/Unequip/AutoEquip/ApplyPlan/EnsureShip and shared
  EquipmentRules for compatibility, preset resolution and legacy repair.
  Explicit permission evaluated by Unity at command time. Prepare/validate/write/
  publish under synchronous lock, detached writer snapshot, reentrant writes refused.
  Read snapshots cannot modify state. No parallel legacy ledger implementation.
- Existing JSON v4/preset keys, saved item parameters, instance IDs, holes and
  stable mount IDs preserved. UI entry points retained; no scene/prefab edits.
- Final: Core Release 0 warnings/errors; dotnet 26/26; EditMode 39/39 job
  745f236d6456477e987b5bd4abb3c8b2; PlayMode 17/17 job
  0c32a8410e7b40d78d3a34ed3e991384; all 0 failed/skipped.
  New PlayMode test drives actual UI and live stats against isolated file/ledger.
- Verified backup `Temp/SaveBackups/architecture-stage1-20261028-144405`.
  Real JSON/.previous/pre-v4 hashes unchanged; registry gameplay values unchanged,
  only PT_Run +3 Unity session values differ. No reset/blanket restore.
- Final MCP: outside Play Mode/not compiling/CombatSandbox clean;
  0 missing scripts/broken prefabs; final console empty. Rebuild still exposes 9
  known runtime CS0618 plus 4 pre-existing test CS0618 and 1 test CS0252; no new-code
  warning fixes. Intermediate tooling/test failures documented in architecture doc.
- No manual visual comparison, standalone/IL2CPP or exhaustive asset audit.
  Reactor/stats/combat/AI deliberately not extracted. Read latest architecture doc
  and shared-inventory doc before future work. Preserve all uncommitted changes.

# Architecture refactor — stage 0 (2026-10-28)

This section supersedes older “leave Play running” and partial-stage notes below.
Stage 0 completed on local main at df8d21c; no commit/push/branch change.
See architecture-refactor.md for the 12 rules, dependency/state ownership map,
full paths, commands, backup, actual job IDs and limitations.

Added Assets/Game.Core (asmdef noEngineReferences + sole compatibility rule and
metadata), DotNet/Game.Core and Game.Core.Tests, scoped SDK 8 global.json and
gitignore exceptions/artifact protection. Existing WeaponMountRules.Accepts now
delegates to Core; serialized Unity enums and all existing GUIDs remain in place.
ShipOrderTests adds boundary/numeric-mapping coverage. Unity regenerated root
My project.slnx with the new assembly. No generated csproj was hand-edited.

Actual baseline: EditMode 35 passed/0 failed/0 skipped; PlayMode 16/0/0.
Actual final: dotnet Release build 0 errors/warnings; dotnet tests 12/0/0;
Unity EditMode 36/0/0; PlayMode 16/0/0. One intermediate EditMode initialization
timeout ran no tests; retry passed. MCP refresh also timed out, but later real
reads verified compiled Core loaded with only netstandard referenced.
Recompile exposes 9 existing CS0618 warnings; not fixed as part of this stage.
Scene validation before/after: 0 missing scripts / broken prefabs, no repairs.

Verified ignored backup: Temp/SaveBackups/architecture-stage0-20261028-140445.
Current JSON + previous + pre-v4 source hashes unchanged after tests. PlayerPrefs
gameplay values unchanged; only Test Runner PT_Run and Unity session keys changed.
Never reset or blanket-restore PlayerPrefs. Scene/prefabs/settings not edited.

Only compatibility is extracted: ledger, stats, reactor, damage, weapon aiming,
orders and AI remain coupled to Unity. No manual visual/physical interaction
acceptance or standalone build claimed. Stop here; await stage 1 request.

# Shared inventory — continuation completed for review (historical)

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
## Stage 2 continuation � 2026-10-28
Core stats and authoritative reactor extraction implemented. See stats-reactor.md for exact boundaries, baseline and final validation. Core 40/40, EditMode 47/47, PlayMode 19/19. Real save hashes/gameplay preferences unchanged. Manual HUD/fitting visual review remains outstanding. Stop before stage 3; all changes local and uncommitted.
