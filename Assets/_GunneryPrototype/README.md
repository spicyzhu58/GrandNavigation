# Grand Navigation — Gunnery Prototype V0.2

Open Scenes/GunneryPrototype.unity and press Play. No package installation or manual component setup is required.

## Controls
- Overview: left-click orange ship to lock; right-click to enter aim view; wheel zooms.
- Aim: mouse X changes lateral impact position; mouse Y changes range; left-click fires ONE salvo; right-click returns to overview.
- Escape exits aim and clears lock. R resets target, projectiles and reload.
- 1 = stationary / 0 m/s; 2 = lateral / 8 m/s; 3 = diagonal / 12 m/s; 4 = repeatable maneuvers / 10 m/s.
- Yellow translucent ellipse = uncertain fire-control area. Cyan cross = YOUR fixed world-space sea impact point.
- Watch all three turret indicators. READY permits firing; TRAVERSING and RELOAD prevent that turret firing.
- For a moving target, choose a point in/near the yellow area, then correct from observed splashes before clicking. The prediction intentionally never drags your aim point.
- There is no ranging shot, double-click fire mode, hold-to-fire or player ship movement.

## Tuning
On Gunnery Systems:
- LeadPredictionController: fixed speed 65 m/s, gravity 9.81 m/s², maximum horizontal range 250 m.
- WeaponManager: 3 m circular dispersion, dispersionEnabled=false for accuracy tests.
- ManualAimController: metresPerPixel controls manual correction sensitivity.
On each player turret: reloadSeconds=5, traverseDegrees=65, alignmentTolerance=1.5 degrees.
On TargetShip: TargetMotion mode/speed/spawnPosition; Health maxHP=600.
Projectile prefab: damage=40, swept sphere radius=.18 m.

The 65 m/s physical arc reaches 100/160/220/250 m in approximately 1.5/2.5/3.5/4 seconds (depends on muzzle height). Flight time is computed from fixed initial speed, never chosen to force an impact.

## Implementation
BallisticSolver is a pure static calculation module. LeadPredictionController iterates target velocity and physical flight time. ManualAimController owns a world-space point and a fixed horizontal aim basis.
The custom camera rig smoothly follows the target in aim mode, has independent zoom and shake, and cannot mutate aim coordinates. Cinemachine remains installed but is not required by this rig.
ProjectileController advances analytic positions in FixedUpdate, SphereCasts each segment, ignores the shooter's hierarchy, resolves only once and intersects the Y=0 mathematical sea plane.
The salvo snapshots one center, samples independent circular dispersion, solves each real muzzle and applies cooldown only to turrets that actually fired.
Original straight modes stop at the boundary. Maneuvering alternates deterministic straight/turn phases and reflects at the sea boundary with consistent reported world velocity. Press R to reset the engagement. This is not enemy tactical AI.
Visual effects are transient primitive puffs and trails. Audio uses short generated noise/tone clips and has no external asset dependency.

## Assets
- Scenes/GunneryPrototype.unity — fully wired scene.
- Prefabs/PlayerShip.prefab — geometry and three twin-turret mechanisms; scene-level targeting references are supplied by the scene.
- Prefabs/TargetShip.prefab — collider, kinematic motion and Health.
- Prefabs/Projectile.prefab — projectile and trail.
- Materials/ — URP whitebox materials.
Editor construction tools are under the Gunnery Prototype menu. Rebuilding recreates the generated prototype scene; do not rebuild over unsaved prototype edits.

## Testing
EditMode tests cover fixed-speed arrival, invalid/unreachable inputs, moving intercepts and prediction/manual-aim independence.
PlayMode tests cover swept high-speed collision, water impact, six actual muzzle origins, fixed speed, reload, moving target hits, bounded dispersion, invalid shots, death/reset, and real Input System mouse/keyboard event flow.
Run via Unity Test Runner or Unity CLI:
    unity command test run --mode editor --filter GrandNavigation.Gunnery.Tests --project-path D:\UnityHub\GrandNavigation
    unity command test run --mode playmode --filter GrandNavigation.Gunnery.Tests --async_tests true --project-path D:\UnityHub\GrandNavigation
    unity command test status --project-path D:\UnityHub\GrandNavigation
Wait for test teardown before opening the scene or starting another run.

## Scope
No driving, tactical AI, enemy attacks, torpedoes, secondary weapons, armor system, waves or win/loss logic.
HUD is English IMGUI to avoid external font assets. No standalone player build has been validated.

## V0.2 sampled fire control
The first lock on a target observes immediately. Each target retains its own observation and timestamp.
The selected target is observed again only when its 2.5 scaled-game-second interval is due; switching views or rapid unlock/relock never forces a fresh observation.
A never-observed target is sampled immediately. Returning to an already-observed target reuses its own data until due; it never inherits another target's velocity.
R is a developer battle reset, clearing all observations and aim history while restoring the target. Pause and time scale affect both movement and observation age consistently.
Between observations, sampled position + sampled velocity * age estimates present position, then the unchanged fixed-speed ballistic solver finds an intercept.
Target live transform is still used for the lock box/camera. Live velocity is read by fire control ONLY inside the due observation callback; no future sequence data is used.
HUD displays observation age rather than a precise time-of-flight/centre marker. Ordinary HUD never displays a fire-control answer cross.
The ellipse uses a world mesh and transparent depth-tested URP material; it is perspective-projected/clipped by the camera and occluded by the ship. It does not modify gun dispersion.
Initial aim is an off-centre point near the region (default 2.25 m lateral), not the precise solution. Further view switches/relocks preserve that target's player correction. Switching back from another target also restores the stored correction.

Inspector defaults:
- LeadPredictionController: fireControlUpdateInterval=2.5 s.
- baseLongitudinalRadius=8 m; baseLateralRadius=5 m; minRadius=3 m; maxRadius=18 m.
- ageGrowthPerSecond=.8; distanceGrowthPerMetre=.015 beyond 100 m; speedGrowth=.1; turnGrowthPerDegree=.06.
- Longitudinal growth is the sum of those terms; lateral growth is 55% of that sum, clamped separately. regionResponse=4 smooths radius/centre/heading changes. All inputs are sampled-state data.
- PrototypeHUD: regionFillOpacity=.08; regionBorderOpacity=.5; regionBorderWidth=.15 m.
- TargetMotion Maneuvering: maneuverSpeed=10 m/s, straightDuration=5 s, turnDuration=1.5 s, turnSequence=[-15,+20] degrees.
- TurnPattern supports Sequence (default), Alternating, LeftOnly, RightOnly; turnAngle=15 degrees is used outside Sequence mode.
- ProjectileController: splashLifetime=1.5 s. Foam and columns have no active colliders; gun dispersion remains 3 m and unchanged.

Validation scenarios: 1 stationary (stable area); 2 constant velocity (accurate extrapolation); 4 then R (repeatable maneuvers causing stale prediction); fire twice with a deliberate cyan-cross correction and compare splashes.
No optional precise-answer debug overlay is enabled or added.