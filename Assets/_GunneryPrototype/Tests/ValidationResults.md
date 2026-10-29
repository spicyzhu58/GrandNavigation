# Validation record — 2026-10-29

Environment: Unity 6000.3.27f1, URP 17.3.0, Input System 1.20.1.
Execution: installed Unity CLI 1.0.0-beta.13 through the running Pipeline 0.9.1-exp.1 editor connection.

## Final results
- C# compilation: completed; 0 errors, 0 warnings.
- EditMode: 14 passed, 0 failed, 0 skipped.
- PlayMode combined run: 8 passed, 0 failed, 0 skipped; 22.06 seconds.
- Serialized scene: 0 missing scripts, 3 wired turrets, 6 muzzles, projectile prefab reference present.
- Final normal Play session: 0 Console errors / warnings, six shells hit, HP 600 -> 360.
- Overview and aim/HUD captures visually inspected.
- Editor left stopped with GunneryPrototype.unity open.

## Verified behavior
1. Fixed-speed low arcs at 100, 160, 220 and 250 m; invalid, vertical-degenerate, non-finite and unreachable solutions rejected.
2. Constant-velocity intercept at 0, 8 and 12 m/s.
3. Prediction changes do not modify manual sea coordinates.
4. Swept 1000 m/s shell hits a 0.1 m hull collider exactly once.
5. Analytic projectile reaches the mathematical sea point within 0.01 m.
6. Full salvo spawns exactly six projectiles at the six current muzzle positions; speed 65 m/s.
7. Immediate and pre-reload shots blocked; shot after five-second reload succeeds.
8. Moving diagonal target hit using prediction; camera zoom/tracking does not change aim.
9. Scatter endpoints remain within 3 m of the shot's immutable center.
10. Unaligned turrets and invalid/out-of-range points cannot spawn shells.
11. Target death exits aim; reset restores HP, colliders, projectiles and reload.
12. Real Input System event injection: left-click lock, right-click aim, mouse delta adjustment, one-click six-shell salvo, hold does not auto-repeat, Escape unlock and R reset.

## Limits
No standalone executable build or subjective audio listening test was performed.
A custom smooth camera rig is used; installed Cinemachine was not changed.
The test runner reports results before all teardown is complete; verification waits for editor stopped state before opening/playing scenes.
Early resolved compile and test-teardown errors were archived to Temp/gunnery-console-history.json before clearing; the final normal run produced no new errors.
Full local results are in ignored Temp/gunnery-editmode-results.json and Temp/gunnery-playmode-results.json.
