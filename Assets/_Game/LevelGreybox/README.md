# Level Greybox V0.1

Open Assets/_Game/Scenes/LevelGreybox.unity and press Play. No build-profile or menu integration is required for this editor prototype.

## Controls and editing
Existing WASD, mouse orbit/wheel, RMB lock, held Shift aim, LMB immediate salvo, Esc unlock and R orbit reset are preserved.
F5 resets the player to the spawn point; F6 recreates all three targets with full HP; F7 toggles the small level debug HUD.
Debug HUD shows actual speed, world position, heading and locked-target horizontal distance.
Select Island_A/B/C and edit LevelIsland.size (X, height Y, Z); its independent root stays at sea level.
Select PlayerShip to edit existing ShipMovement.settings and Weapon. Select Yellow prediction region for ellipse radii.
The initial three targets are behind cover: steer around the islands to obtain a direct line of sight/fire.

## Saved geometry
Ocean is a stock Plane at (0,0,0), scale (256,1,256), measured Renderer bounds 2560 x 2560.
No ocean Collider. The four wall inner faces are exactly X/Z +/-1280.
The central +/-600 area is only a selected gizmo, not a physical boundary.
Island_A: root (-260,0,40), solid 220 x 35 x 140.
Island_B: root (320,0,70), solid 200 x 30 x 160.
Island_C: root (70,0,310), solid 170 x 25 x 120.
All are primitive box solids, above Y=0, with matching colliders and independent unit-scale roots.
Player spawn (0,0,-360), heading +Z. Existing combat ship prefab reused; scene-only hull/bow edits give a measured 32 UU length, with 32 UU collision length.
Target XZ positions: (-360,220), (380,180), (0,420), each 1000 HP, no motion or attacks.
Target hull roots are Y=1 to place the existing 2 UU tall hull on the sea. Spawn markers are Y=0.

## Scene-only settings
Movement: max forward 10, reverse 3, forward acceleration 1.67, brake 2, reverse acceleration .8, max yaw rate 5 degrees/sec, full-turn authority at 10 UU/sec. Other existing movement settings retained.
Weapon: fixed relative muzzle speed 140, gravity 9.81, effective horizontal range 1200, reload 5 seconds, 3 twin turrets and original 260-degree arcs/dispersion/damage.
Fire observation .5 seconds, visual smoothing .12 seconds, fixed ellipse longitudinal radius 24 and lateral radius 12.
Camera far clip 5000; existing target-lock aim camera scene settings distance 90, height 45, FOV 36.
Raycast selection retains the existing 2000 UU limit and solid-island occlusion.
LevelShell prefab variant: lifetime 20 seconds, absolute X/Z cleanup beyond 1400, trail time 1.2 seconds.
Shared code changes are only opt-in horizontal range support (default false) and projectile bounds cleanup (default zero/off); old scenes/prefabs/defaults and OOP relationships are preserved.

## Executed validation (2026-11-01)
Unity 6000.3.27f1 compilation succeeded with no errors.
18/18 existing EditMode tests passed.
Combined PlayMode run: 27/28 passed; the remaining geometry test used an exact float equality for a physics-updated position. After replacing that assertion with a .001 UU tolerance, its independent rerun passed 1/1. Thus all 28 covered cases passed across the final run/rerun; no claim of a single 28/28 run.
Coverage includes original movement/camera/combat regressions, 6 pure large-map ballistic cases, and 6 new level integration cases: geometry and reset, measured motion, island sliding and boundary containment, actual five-range flights, all-three-island low-arc interception and actual ship damage, and injected RMB/Shift selection/view checks at 400/600/800/1200 plus simultaneous drive/aim input.
Static/forward 10/reverse 3/turning-direction (6,0,8) platform velocity models were checked at all five distances. Existing moving-salvo regression also passed.
Actual muzzle height in flight test approximately 3.16 UU (barrels elevate). Solved / observed fixed-step flight duration:
- 400: 2.869 / 2.900 sec
- 600: 4.333 / 4.360 sec
- 800: 5.833 / 5.860 sec
- 1000: 7.389 / 7.420 sec
- 1200: 9.027 / 9.060 sec
All five actual projectiles generated water impacts and were removed. Observed durations include fixed-step/destruction observation latency.
Measured Rigidbody motion: 0-10 speed 6.00 sec; 10-0 brake 5.00 sec; steady full-rudder 180-degree turn 36.08 sec; half-turn chord radius 114.80 UU; steady actual speed 9.982 and turn rate 4.99 degrees/sec. Motion tests used accelerated game time with unchanged .02 second physics steps; timings above are simulated seconds. Turn time starts after rudder/yaw settle, not on key press.
Additional live scene check: at 1000 UU with dispersion temporarily off, actual aligned salvo spawned 6 shells, target HP fell 1000->850, reload completed and no projectiles remained.
An actual projectile spawned beyond the 1400 cleanup limit was removed.
Game-view captures inspected initial normal view, target-lock view/ellipse/reticles/debug distance and an overhead layout view. Runtime test changes were discarded on stopping Play.
Final Console ground truth: no compile failure, zero errors and warnings.
No packages installed/upgraded, remote changes, push, main merge, or changes to old scene assets.

## Limits for further playtesting
No standalone build or sustained physical mouse/keyboard playthrough was performed; tests use Input System event injection and real Unity Play Mode physics.
Islands are deliberate box greyboxes. Camera transition paths around all possible island approaches and subjective long-range aiming/feedback readability still warrant manual playtesting.
Sea-impact reticles remain estimates without obstacle interception; actual projectiles do collide with islands.
No new AI, win/loss, menu navigation, or reticle redesign is included.

