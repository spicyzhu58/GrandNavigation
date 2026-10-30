# Ship Movement Prototype V0.1

Scene: Assets/_Game/Scenes/ShipMovementPrototype.unity
Open the scene and press Play. Bow is local +Z. This branch is based directly on main, with no menu or GunneryPrototype dependency.

## Controls
- Hold W/S to increase/decrease target speed; release to hold the target.
- W+S cancels. S can request reverse; actual speed brakes through zero before reversing.
- A/D steers left/right. A+D cancels. Release centres the rudder and gradually removes yaw rate without returning to the original heading.
- Reverse steering is inverted; at rest the ship cannot turn in place.
- Space calls the course Weapon dependency hook. No Projectile is assigned in the scene, so no shot is spawned.

## Movement / physics
ShipMovement alone owns Rigidbody velocity and angular velocity, updated in FixedUpdate.
The dynamic body uses ContinuousDynamic collision detection, interpolation, no gravity, fixed root Y and locked X/Z rotation.
Longitudinal velocity approaches target speed at separate acceleration/braking/reverse rates. Lateral velocity decays exponentially, allowing brief drift.
Rudder angle and yaw rate have separate response limits. Turn authority increases with forward-axis speed, saturating at 12 m/s; reverse effectiveness is 60%.
Physics resolves contacts; the collision handler removes any remaining inward horizontal normal velocity while preserving tangent velocity.
The contact material has zero friction and zero restitution. Islands and four map walls are static colliders. There is no collision damage or buoyancy simulation.
HUD speed is signed forward-axis velocity; WorldVelocity exposes the complete world velocity including lateral drift.
The camera follows the physics root, not a decorative child.

Inspector defaults:
- maxForwardSpeed=25 m/s, maxReverseSpeed=8 m/s, targetSpeedRate=10 m/s².
- forwardAcceleration=4, brakingDeceleration=6, reverseAcceleration=2.5 m/s².
- maxRudderAngle=35°, rudderRate=90°/s, maxTurnRate=16°/s.
- reverseTurnEfficiency=.6, fullTurnSpeed=12 m/s, turnAcceleration=24°/s², lateralDamping=1.4 /s.
- Shared player/AI commands: SetTargetSpeed, SetThrottleInput, SetSteeringInput.
- Read-only WorldVelocity, WorldPosition, Forward, ActualSpeed, LateralSpeed, TargetSpeed, RudderAngle, TurnRate.

## Course OOP relationships
Inheritance:
    Ship : MonoBehaviour, IDamageable
      PlayerShip : Ship
      EnemyShip : Ship
        Destroyer : EnemyShip
        Battleship : EnemyShip

Composition: Ship holds serialized HealthSystem and Weapon component references, and caches ShipMovement.
Dependency: PlayerShipInput calls ShipMovement; Space calls PlayerShip.FireWeapon -> Weapon.TryFire -> Projectile.Launch.
Interface: Ship.TakeDamage(int) delegates to HealthSystem; every derived ship inherits it.
Extension: a future AircraftCarrier : EnemyShip can add its own Weapon subclass or attack component without changing Destroyer, Battleship or IDamageable.
Only PlayerShip is attached to the player root: there is no second independent Ship component.

Weapon and Projectile are intentionally tiny course scaffolding. Projectile.Launch only records activation; no real projectile is instantiated or simulated. ResolveHit demonstrates IDamageable delegation.
Projectile.ComposeWorldVelocity preserves the future contract:
    projectileWorldVelocity = muzzleRelativeWorldVelocity + shipWorldVelocity
The first term is already rotated into world space. This prototype does not integrate moving fire or modify an existing BallisticSolver.

## Resources
- Runtime/: movement, input, camera, HUD and OOP types under GrandNavigation.Gameplay.
- Prefabs/PlayerShip.prefab: primitive hull, bridge, funnel and three twin-gun turret visuals.
- Materials/: independent URP materials and contact PhysicsMaterial.
- Editor/ShipMovementSceneBuilder.cs: creates a new scene only; refuses to overwrite an existing scene.
- Tests/: EditMode math/OOP and PlayMode physics/keyboard validation.
Build shared scene list starts with ShipMovementPrototype and retains SampleScene.

## Executed validation
Unity 6000.3.27f1 via CLI / Pipeline, 2026-10-30:
- Compilation succeeded, no compile errors.
- EditMode: 7/7 passed. Speed limits, hold/cancel, reversal through zero, acceleration rates, zero/reverse steering, inheritance/composition/interface/Weapon dependency.
- PlayMode: 4/4 passed. Actual Input System keyboard input, inertial acceleration, rudder return, yaw decay, reversed astern turning, lateral drift/damping, fixed root height.
- Contact test: 130 consecutive physics steps thrusting into the island at up to 25 m/s, no significant penetration; glancing collision preserves tangent motion.
- Map boundary collision and camera/HUD/root wiring verified.
- Runtime Console: no errors or warnings.
- Visual inspection: initial third-person framing and readable HUD.

Run:
    unity command test run --mode editor --filter GrandNavigation.Gameplay.Tests --project-path D:\UnityHub\GrandNavigation
    unity command test run --mode playmode --filter GrandNavigation.Gameplay.Tests --async_tests true --project-path D:\UnityHub\GrandNavigation
    unity command test status --project-path D:\UnityHub\GrandNavigation

Limitations: primitive islands and rectangular collision hull; no enemy AI, real weapons, damage gameplay, waves or buoyancy. Standalone player build has not been tested.

## Free observation camera
Component: Follow Camera > ShipFollowCamera. ShipMovement and PlayerShipInput are unchanged.
- Mouse: 360-degree world-space orbit; pitch defaults to 10–75 degrees.
- Wheel: smooth zoom; reset distance 35 m, range 15–65 m.
- R: smoothly reset behind the ship's CURRENT heading, with default yaw/pitch/distance.
- Esc: release/capture the cursor for Inspector tuning. Released mouse movement does not rotate the camera.
- WASD still controls the ship independently. Orbit yaw does not follow ship yaw.
- CameraPivot is a separate observation child of the physics root; Camera Pivot Height drives its vertical offset.
- SetControlActive(false) releases camera ownership for a future fire-control view. No aiming mode is implemented.

Inspector sections: Follow target, Mouse orbit, Zoom, Smoothing, Camera safety.
Sensitivity is degrees/pixel; Zoom Sensitivity is metres/normalized scroll unit, default 4 m per notch with the project's Uniform Across All Platforms input setting. Input System 1.20.1 returns about 1 per notch, not the legacy Windows value 120.
Rotation/Follow/Zoom Smooth Time are in seconds. Default values are applied by R; other settings react during Play Mode.
OnValidate clamps invalid min/max/default relationships and non-positive smoothing/radius values.

To preserve a tuned Play Mode setup: component menu > Copy Component; stop Play; select the same component > Paste Component Values; save the scene. Unity normally discards Play Mode edits on stop.
Solid obstacles use sphere casts; obstruction recovery is smoothed. A close obstruction cannot compress the camera into the visual hull; sea clearance and overlap lift protect the camera position. The prototype's box islands are verified; arbitrary complex caves/overhangs are not covered.
