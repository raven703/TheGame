# Stage 4 — mount geometry extraction

## Build/solution boundary

Actual starting HEAD was cbb4478 on main, clean worktree (stages 0–3 already
committed by the user), not the old handoff's df8d21c. No checkout/commit/push here.
The root solution contained two DIFFERENT Game.Core projects: Unity-generated
Game.Core.csproj and DotNet/Game.Core/Game.Core.csproj. The latter links the same
Assets/Game.Core sources; no duplicate source tree was found.

The installed Visual Studio package's GetSolutionProjects preserves external
projects from the previous solution. UnitySolutionBoundary uses the supported
OnGeneratedSlnSolution callback to exclude DotNet projects from root slnx output.
Root My project.slnx is Unity-only; DotNet/Game.Core.sln is independent. SDK 8
(DotNet/global.json) cannot parse slnx, hence the independent solution uses sln.
No generated csproj, asmdef identity/GUID/namespace or noEngineReferences changed.
Workspace dotnet.defaultSolution already selects My project.slnx; no global edit.
SyncAll was actually invoked, root external entries disappeared. SDK 10's
`dotnet sln "My project.slnx" list` parsed root successfully. IDE UI/language-server
load is NOT verified: user should check C# Dev Kit Solution Explorer after reload.

From C:\Personal\Unity\TheGame\My project\DotNet:

```powershell
dotnet build Game.Core/Game.Core.csproj -c Release
dotnet test Game.Core.sln -c Release
```

## Geometry and ownership

MountGeometry is the sole angle owner, dictionary keyed by stable MountId.
AimPoint is just two floats; world XY units, degrees, counterclockwise positive.
Unity passes actual firePoint world position (or historical ship-center fallback),
world hull forward (up/right selected by spriteNoseIsUp), local hardpoint offset,
target position and scaled deltaSeconds. Core rotates the fixed axis. Local
firePoint rotation and the rendered barrel never feed that axis.

AdvanceAim returns InRange/InSector/Aligned and typed AimRefusal; CanAim retains
the old bool API. Shoot calls it once per occupied installation, before existing
WeaponCombat spend/reload/cover/damage. No second Update or additional frame lag.
Tracking continues during cooldown and toward a clamped boundary for outside or
distant targets, as before. Pending lock/no target/EMP/preparation stop normal
Shoot and freeze angles. Actual angle, not goal, gates firing.

Full sectors use circular delta; partial sectors use a bounded interval and never
take the short path through the forbidden rear. Own Fixed and hardpoint sectors
share the SAME axis in this model; their minimum is consequently their exact
intersection, not an approximation for distinct axes. Fixed never advances angle.
Mount boundary has no epsilon; the historical own-Fixed .0001 degree tolerance
is preserved. Range is inclusive; aim tolerance clamps to 0..15 degrees.

Refresh, removal and replacement retain angle on the same stable mount, matching
characterization. No equipment/UI angle reset. Different ship IDs have independent
initial-zero state; destruction discards the owning ShipWeapon/Core instance.
Empty fitted slots are skipped before geometry, do not compact and cannot fallback.
WeaponCombat remains sole reload owner and Reactor sole resource owner.

Fitting text and FiringArcVisualizer use Core EffectiveSector via ShipWeapon;
actual rays use Core Rotate and Angle. Unity still builds all meshes/lines/materials,
overview rings and deduplication; one toggle/default off and shared range unchanged.
Existing visual barrel freeze without targeting is intentionally preserved, not
silently restyled/fixed. No orders/movement/AI extraction (stage 5 not started).

## Evidence

Pre-replacement EditMode 60/60 (56 existing + 4 characterization), job
e969bb76a84a4a2bbf65171439a62f7c. Earlier two 56/56 runs did not yet discover the
new fixture; not counted as its characterization evidence. First forced refresh
timed out and one subsequent ping failed; later compilation/tests succeeded.
Initial SDK-8 slnx command failed; replaced independent solution with supported sln.
Shell output capture was unreliable; build/test results were read from log/TRX.

Core Release 0 warnings/errors, independent tests 75/75 (15 new geometry cases).
Post-extraction EditMode 60/60: 0016d1f5c2c6428993473d768cda0c61;
latest after adapter/fitting cleanup: 8110d7179196450981165c4c0dcfa3cc.
PlayMode 21/21: ef8f56db490b4527b2af4ae2e679da2e; final repeat 21/21:
1dbe126083c64503975e285151dbfa41, zero failures/skips.
Loaded CombatSandbox validation: zero missing scripts/broken prefabs, no repair.
Console: no errors, 9 existing CS0618 compilation warnings left untouched.

Backup Temp/SaveBackups/architecture-stage4-20261029-085230 was created and copy
hashes verified before tests. This folder uses actual machine clock; task environment
declares 2026-10-28, while machine timestamps read 2026-10-29. Baseline real hashes:
current 91F41E0905A3BFEC36B9CE79233B1161573A7766C77F37C9D29634E064618921;
pre-v4 3F3CB9C51D0D3B96F43208D12AC9158AD7FF38F86111C193C142E780267814D5;
previous 85823C197B732CE6CBF09895ED9B659195A270A7210B92EE89E04B07610BF4DA.
After automated suites all three match. Registry entry comparison changed only
PT_Run and three Unity session keys; gameplay/preset keys unchanged. No restore,
save migration, instanceId/key change or real player loadout modification.

No physical mouse/keyboard interaction claimed. Game View visual acceptance at
1280x720/1920x1080 and all-asset prefab audit are not yet verified by this document.

Final SyncAll verified live: 0 external DotNet projects, no duplicate project names,
Core references only netstandard; editor outside Play Mode/not compiling. Final
git diff --check passed and three real saves still match. A subsequent parallel
scene/console recheck disconnected after SyncAll; earlier explicit clean scene and
Console results above remain the evidence, not a claimed successful disconnected
check. Final visual acceptance remains incomplete; do not call stage 4 fully accepted
until the two Game View resolutions and broader lifecycle integration are reviewed.

## Acceptance follow-up

Existing stage-4 working tree retained, HEAD cbb4478/main unchanged. No production
code changes, architecture additions, commit/push/checkout or stage 5 work.

Added ReactorAdapterTests.MountLifecycle_ShipSwapRefreshRemovalHolesAndRecreation_AreIsolated.
It uses isolated definitions and actual Shoot/resource/reload state: same runtime
hull switches A (three mounts, holes at 0/1) to B (one different stable mount),
starts at zero/no B reload, A state is untouched; repeated equipment application
preserves angle/deadline; removal/replacement preserves the same mount's state;
empty slots never fallback; Destroy + new ShipWeapon has fresh angle/reload owner.
This deliberately bypasses persistence/UI ship selection, not a claimed physical
ship-selector test. Standalone run 1/1: fea08b5871bb468baab95bf424e00253.

Targeted PlayMode 4/4: 808555a5c67749d188728ad97cf7d99c (new lifecycle,
PhysicalNearestCoverTakesDamage_RefreshKeepsReload_EmptySlotCannotFallback,
Hardpoints_ActualShots_RespectGeometryResourcesAndStableSlots, WeaponRangeTests).
The requested lock test name in that invocation did not exist, so only four tests
were discovered. Correct AutoFire_RequiresLock_RespectsEmp_AndDoesNotOverrideRoute
then passed 1/1: ba9ccc024c634c8885c3ec3bf89cc6a7. No failed assertions or weakened
tests. These run actual firing/Update/resource/cover paths, not geometry-only checks.
No full-suite repetition: only one test was added, production code unchanged;
previous complete 75/60/21 results remain historical evidence, not new 22/22 claim.

Game View fixed sizes selected through editor API, screenshots captured and read
as images: Temp/Stage4Acceptance/stage4-720.png (1280x720),
stage4-1080-rotated-sectors.png (1920x1080, hull +35 degrees). Reviewed own 60-degree
Fixed sector on a 360-degree hardpoint, full-circle turret, 180-degree turret,
offset firePoint origins and rotated geometry, barrel/ray agreement. Runtime
inspection confirmed every rendered ray origin/direction matches MountOrigin and
BarrelDirection/MountAxis; RingCount=1, SectorCount=3 with two identical Fixed
installations plus two turrets and one empty slot. Multiple displaced circles in
the picture are legitimate per-mount outlines, not duplicate overview rings.
Open/Close fitting handlers returned active=True, Visible during/after=True.
No physical click/keyboard injection. No extraction-caused visual defect found.

IMPORTANT save-isolation incident: opening the real fitting with the temporary
stage4-preview definition called existing persistence, adding a third ship record
and rotating .previous. Hash check caught it. Diagnostic after-files preserved in
Temp/SaveBackups/stage4-acceptance; stopped preview and restored current/.previous
from verified pre-acceptance copies (all three files then hash-matched). This was
NOT an unchanged-throughout run and no claim otherwise. Registry comparison showed
only PT_Run and three Unity session keys; no gameplay/preset key changes. Final
Play was restarted using ORIGINAL Cobra definition/equipment, not preview ledger.
Final post-restart hashes: current/pre-v4/.previous all match acceptance manifest.

SyncAll regenerated project files again: 0 DotNet entries, 0 duplicate project
names. VS Code UI unavailable: manual C# Dev Kit loading confirmation remains.
Final Console errors=0; CombatSandbox missing scripts=0/broken prefabs=0;
git diff --check passed. Final editor is intentionally PLAYING in PREPARATION,
sectors ON, original equipment, scene not saved. Existing CS0618 untouched.

Remaining manual checks: physical V toggle; pointer open/close fitting and confirm
toggle state; verify C# Dev Kit root solution loads without the former name error.
All-project prefab audit and exhaustive wave progression are outside this acceptance.