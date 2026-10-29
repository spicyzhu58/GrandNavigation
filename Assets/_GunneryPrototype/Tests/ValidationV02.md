# Gunnery V0.2 validation — 2026-10-29

Base: c095f4f (V0.1). Branch: feature/fire-control-v02.
Unity 6000.3.27f1 / URP 17.3.0 / Input System 1.20.1; existing CLI/Pipeline connection used.

## Implementation and preservation
Existing LeadPredictionController, ManualAimController, TargetSelectionController, TargetMotion, PrototypeHUD, PrototypeSession, FeedbackEffects and ProjectileController were incrementally changed.
FireControlTrack is a pure observation-history helper, not a duplicate gameplay controller.
No scene rebuild or scene/Prefab file changes. No changes to Packages, Unity version, ProjectSettings, BallisticSolver, WeaponManager, TurretController, camera controller, Health or projectile trajectory/collision logic.
ProjectileController changes only add splashLifetime and forward it to the existing feedback call.
One pre-existing VFX bug was fixed: primitive effects now disable colliders immediately before deferred destruction, preventing visual effects from intercepting later shells.

## Sampling rules
Initial lock observes immediately through the existing selection component's event.
Each target has independent cached position, velocity and timestamp. First-ever target lock observes immediately; returning to a cached target reuses its sample until due.
Unlock/relock, switching targets and aim toggles cannot force a sample ahead of schedule.
Selected targets refresh every 2.5 scaled-game seconds. Paused/slow time governs observations and target motion together.
Estimate = sampled position + sampled velocity * sample age, followed by unchanged fixed-speed intercept solving.
Only the observation callback reads live target velocity. It never reads future maneuver phases or turn sequence.
Invalid/dead targets are purged. R starts a fresh debug engagement and clears samples/aim history.
Manual aim is preserved per target across view switches and reselection; the first point is near, not exactly at, the prediction centre.

## Region and movement
Yellow translucent world-space mesh ellipse, dashed border, no centre glyph. Shader depth-tests against the ship; near/frustum clipping uses real camera projection.
Age, sampled speed, engagement distance and observed heading change enlarge the region. Radius/centre/heading are smoothly updated, with yaw-only orientation on the sea plane.
Zero-speed samples retain stable heading. The region does not feed projectile dispersion or aim.
Key 4 selects Maneuvering at 10 m/s. Default sequence: straight 5 s, left 15 degrees over 1.5 s, straight 5 s, right 20 degrees over 1.5 s, repeat.
Boundary reflections report actual step displacement as velocity.
Water columns and foam last 1.5 s by default, adjustable on ProjectileController; existing trails and weapon spread are unchanged.

## Actual test results
- Compilation: completed, 0 errors, 0 warnings.
- EditMode final: 22 passed, 0 failed, 0 skipped.
- PlayMode final combined suite: 14 passed, 0 failed, 0 skipped, duration 47.74 s.
- Existing fixed-speed, high-speed swept collision, six real muzzle origins, cooldown, damage, scatter and camera-independent aim tests pass.
- New tests: continuous sampled extrapolation, no premature live reads after turning, per-target cache, relock/view-toggle protection, zero-speed heading, region bounds/smoothing, target invalidation, default real maneuver stale error/new observation, pause/time scale, boundary reflection, and two corrected six-shell salvos.
- Default maneuver produced >4 m stale-vs-current-velocity intercept difference before the next observation; afterwards the sampled velocity matched the new straight heading.
- Two deliberate sea aim centres (40,0,150) and (60,0,155) produced 6 and then 12 cumulative splashes, each last impact within .02 m of its respective centre, with spread disabled.
- Water effect tested alive after 1.1 s and destroyed after 1.6 s; its collider disabled immediately.
- Real Input System event flow passes. An intermediate combined run had one input-lock failure while an isolated rerun passed; the fixture now disables native input devices only during injection and restores them in teardown. The final combined suite passed.
- Final normal PlayMode session: six fired, six splashes, six simultaneous water columns. Paused only to inspect/capture the impacts, then unpaused and stopped.
- Prediction-region and splash/HUD screenshots were visually inspected.
- Final normal-session Console: 0 errors, 0 warnings.

Local detailed test JSON and screenshots are under ignored Temp/v02-*.
No standalone build or optional precise-answer/debug impact overlay was added.
The existing scene is left open, stopped and unmodified.
