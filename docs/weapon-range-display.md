# Tactical weapon range display

One UGUI button, `Дальность и секторы [V]`, toggles the existing
FiringArcVisualizer. Off on scene entry; fitting hover does not change it.
No separate weapon selection/detail mode is provided.

The center ring is an overview reference, labelled in world units (`ед.`).
Actual sectors originate at each firePoint and use ShipWeapon.EffectiveSector,
shared with Fixed aiming. Turret rays use the stored MountAngle. A full-circle
turret has no radial boundaries. Identical origins/axes/sectors/ranges merge;
identical ring radii merge. Empty slots contribute nothing.

Combat currently uses one ShipWeapon.range for all its installed weapons.
This display intentionally does not invent per-item ranges or alter combat.
Asteroids still block shots: the fill is a potential zone, not guaranteed LOS.
Cooldown and resource states do not animate the geometry.

Runtime objects/materials/meshes are explicitly destroyed. Static geometry is
rebuilt after the equipment/geometry signature changes, not on toggle. Line and
fan geometry has no colliders; labels and explanatory tooltip do not intercept
raycasts. Sprites/Default is used with the project's current URP renderer.

Validation: 35/35 EditMode and 16/16 PlayMode passed. WeaponRangeTests covers
Fixed intersection/Universal, 360 versus partial turret, actual angle, firepoint
origin, empty slots, deduplication and repeated toggles. MCP mixed runtime preview
uses a cloned ship definition and temporary equipped list, never saves inventory.
Screenshots: Temp/Diagnostics/weapon-overview-1920x1080.png and weapon-final-720.png.

Remaining acceptance work: dedicated bright-background comparison; physical V
and pointer interaction; real fitting open/close integration assertion; ship-swap
state transfer and destroyed-module muted palette. No selected-weapon screenshot
exists because the requested single-toggle interaction excludes selection mode.
Final PlayMode rerun after rendering correction: 16/16 passed,
job 240c35a6526741268a96edd974d9083a. EditMode: 35/35,
job 9c5b76cd029944b0adcb305e2c347955.