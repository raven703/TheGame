# Tactical orders validation — 2026-10-27

## Implemented
- Single ShipOrderController integrated with PlayerInputHandler, ShipMovement, ShipWeapon explicit tactical API, and all four EnemyAI personalities.
- Independent weapon selection / order target, RMB, Shift queue, Stop/S, speed caps.
- Force/torque-based movement without velocity assignment, braking-distance arrival, queue corners, engine gate, boost.
- Radius-aware local avoidance, stable side, moving bounds, destination projection, bounded retries, visible failed queue.
- Runtime route/numbered markers/tactical diamond; UGUI stop/speed/status; saved scene layout outside play.

## Final automated runs after last navigation correction
- EditMode: 22 passed, 0 failed/skipped, 0.6334035 s.
  MCP job d66a372a88ca4780998e56136e08297b.
- PlayMode: 13 passed, 0 failed/skipped, 61.4580118 s.
  MCP job d26d6bb7a2904a4f8cdf72942abd005f.
- Original 17 tests are included in EditMode total.
- Scene validation: 0 missing scripts, 0 broken prefabs, 0 issues.
- Console read without clearing: no application errors on final checks; MCP WebSocket reconnection warning occurred intermittently.
- git diff --check passed after trailing-whitespace cleanup (line-ending conversion warnings only).

## Visual / integration checks
- Fresh screenshot viewed at 1920x1080: Temp/Diagnostics/orders-1080-1.png.
- Fresh screenshot viewed at 1280x720 with Stop [S] popup: Temp/Diagnostics/orders-720-tooltip.png.
- Route line and numbered final/intermediate markers viewed: Temp/Diagnostics/orders-720-route.png.
- Real scene Stop button UI raycast blocks world input: true.
- Popup screen rectangle blocks world input: true; pointer exit hides popup.
- Runtime engine HP temporarily set to 0: UI warning appears; HP restored immediately.
- Fresh Play confirmed preparing=true, mode=HoldPosition, cap=100%, route empty, popup hidden.
- Runtime markers absent outside Play before final entry; scene dirty=false.

## Known limitations / not manually verified
- Local avoidance only, not global routing. Dense fields/cages can terminate with visible error.
- Destination projection uses conservative bounds circle, not exact nearest concave surface.
- Sudden fast-moving obstacles may collide physically; collisions remain enabled.
- Full physical mouse + keyboard sequence is not manually exercised; input command semantics, actual UGUI raycast, button handlers and popup integration checked separately.
- No long-duration dense-field stress test; no player build generated.
- Engine damage uses existing destroyed-module gate; no new partial-damage thrust balance introduced.

## Repository / editor
- Local main only; no commit/push/new branch.
- Initial tracked tree was clean; earlier untracked controller/meta/handoff preserved via implementation/documentation.
- Unity-generated fallback font glyph updates and solution test project reference retained.
- Final editor should remain Play Mode, preparation, 1280x720 for user review.

See tactical-controls.md for controls and architectural/avoidance details.