# Player equipment and fitting

## Ownership and persistence

`FittingItem.id` is the definition identifier (legacy immutable stats/art snapshot).
`instanceId` identifies one physical owned copy. Equal definition IDs do not imply
equal ownership. Placement is either the top-level free inventory or one ship's
stable slot index; weapon hardpoint IDs and actual fire sectors are unchanged.

`Game.Core.PlayerEquipment` is the sole detached ledger (stage 1, 2026-10-28).
The existing Unity `PlayerEquipment` is a facade, not another owner. Reads return snapshots. Move, unequip and
preset application stage a full snapshot, validate uniqueness and exact conservation
of instance IDs and item data, invoke the writer, then publish. A throwing writer
leaves the original ledger and UI unchanged. UI refresh happens only after success.
The disk writer validates JSON, flushes a `.pending` file, reads it back, and uses
`File.Replace` (old state in `.previous`). No fallback overwrites a failed save.
Unreadable/future/duplicate-identity saves are errors, never fresh profiles.

## Stage 1 boundary

Core contains identity/ownership, free stock, placement, compatibility, empty-only
moves, unequip, auto-equip, plan resolution/application, one-time starter issuance
and legacy slot-repair rules. Commands receive current permission explicitly from
the Unity facade; Core never reads CombatSession/GameState. Synchronous locked
commands reject writer reentrancy and publish only after successful persistence.
Snapshots and the writer's candidate are detached; items are immutable.

EquipmentDataAdapter retains complete saved FittingItem records by opaque key,
not inventory placement. It maps ShipDefinition capacities/stable hardpoint IDs to
minimal Core data; visual values and saved stats remain outside Core and are
reconstructed from saved records, never refreshed from a catalog. Existing enum
values, v4 format, preset keys and empty indices are preserved. Legacy parsing,
JsonUtility, PlayerPrefs, paths, verified backups and EquipmentFileStore atomic
file replacement remain Unity/infrastructure adapters. No scenes/prefabs changed.

Verified stage-1 backup: `Temp/SaveBackups/architecture-stage1-20261028-144405`.
Final results: Core Release 0 warnings/errors; dotnet 26/26; EditMode 39/39;
PlayMode 17/17 (all final runs 0 failed/skipped). Real isolated UI test checks
green highlight, cancel/close, occupied refusal, write failure, unequip, preset,
ship change/auto-equip, live speed/UI refresh and file re-read. Real JSON,
`.previous`, pre-v4 backup hashes unchanged; gameplay PlayerPrefs unchanged.
Intermediate failures, warning distinctions and exact job IDs are recorded in
`docs/architecture-refactor.md`. No standalone/IL2CPP or manual visual acceptance.
Reactor and stats remain Unity-coupled; stage 2 has not started.

## Fitting rules

Drag/drop accepts **only empty compatible slots**. Occupied slots cannot swap or
replace; source slots stay reserved until a successful move. Remove first to the
shared inventory to transfer between ships. `CanMove` is used by UI highlights,
hover rejection, actual moves and auto-equip. Combat locks all mutations. New
ship selection preserves installed equipment, never grants another starter set.
New profiles receive the first selected ship's starter inventory exactly once;
later ships start empty. Demo equipment is not injected on window open.

Empty cards show mount/category icon and slot number. Occupied cards retain these
plus item name. Green background/border appears only for eligible empty targets.
Tooltip width is bounded, height measured, position flipped and clamped in root
Canvas units. Decorative graphics and drag cards do not receive raycasts.

## Presets

PlayerPrefs key is retained: `TheGame.Loadout.v2.{shipId}.{slot}`. Presets are plans
by definition ID with stable empty indices, not grants. Resolve against only the
selected ship's equipment and shared free stock; never the other ships. Validate
counts, category, mount compatibility and capacity before a single ledger commit.
An equivalent owned copy can satisfy the requirement regardless of instance ID.
Legacy definition lists retain their order and multiplicity. A failed plan changes
nothing, including the shared free stock.

## Format v4 and migration

Persistent file: `Application.persistentDataPath/ship_loadouts.json`.
Top level: `formatVersion:4`, `starterInventoryIssued`, `inventory`, `loadouts`.
Per-ship serialized inventory is empty; runtime UI views receive a detached shared
inventory snapshot. Null/empty slot placeholders are normalized without compaction.
Legacy ship inventories are concatenated, every actual record receives a separate
GUID, equipment stays installed, and starter issuance is marked complete. No
deduplication by definition ID. Write v4 only after validation and a verified source
backup (`.pre-v4.bak`, unique suffix if an earlier backup differs). Loading v4 again
does not migrate or grant. Missing instance IDs in v4 are corruption, not new copies.

## Local migration audit

Before first implementation, verified backup:
`C:\Personal\Unity\TheGame\My project\Temp\SaveBackups\shared-inventory-20261028-111723`.
JSON SHA256: `3F3CB9C51D0D3B96F43208D12AC9158AD7FF38F86111C193C142E780267814D5`.
Includes editor PlayerPrefs registry export. Standalone PlayerPrefs key absent.
Continuation backup of already-v4 current state:
`C:\Personal\Unity\TheGame\My project\Temp\SaveBackups\shared-inventory-continuation-20261028-113723`.

Original v3: Cobra 4 free +4 installed; Sidewinder 3 free +6 installed =17.
Current v4: 7 free +10 installed =17. Definition counts match exactly:
w1,w2,w3,s1,s2,e1,e2,demo turret: two each; t1: one.
The user changed placement between backups (Cobra weapons now empty); preserve
the newer placement rather than restoring the old layout. Two legacy demo turrets
are retained intentionally, not guessed to be duplicates. The continuation audit
checks current placement and instance IDs against the continuation backup.