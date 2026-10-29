# Grand Navigation — Gunnery Prototype V0.1

Open Scenes/GunneryPrototype.unity and press Play. No package installation or manual component setup is required.

## Controls
- Overview: left-click orange ship to lock; right-click to enter aim view; wheel zooms.
- Aim: mouse X changes lateral impact position; mouse Y changes range; left-click fires ONE salvo; right-click returns to overview.
- Escape exits aim and clears lock. R resets target, projectiles and reload.
- 1 = stationary / 0 m/s; 2 = lateral / 8 m/s; 3 = diagonal / 12 m/s.
- Yellow square = current fire-control prediction. Cyan cross = YOUR fixed world-space sea impact point.
- Watch all three turret indicators. READY permits firing; TRAVERSING and RELOAD prevent that turret firing.
- For a moving target, move the cyan cross near the current yellow square before clicking. The prediction intentionally never drags your aim point.
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
Target motion is straight and constant-speed; it stops at the whitebox boundary. Press R to restore its starting position. This is not enemy tactical AI.
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
