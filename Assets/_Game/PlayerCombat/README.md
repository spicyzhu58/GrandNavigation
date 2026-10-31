# Player Combat Prototype V0.1

Open `Assets/_Game/Scenes/PlayerCombatPrototype.unity` and press Play. No menu or build-list change is required.

## Controls
- W/S: adjust target speed; A/D: rudder. Movement continues in aim mode.
- Mouse: free orbit; wheel: zoom. R resets the observation camera.
- Right-click: lock the enemy under the screen-centre pointer (cursor is captured).
- Hold either Shift: aim view. Mouse moves the cyan world-space sea impact point. Release Shift to return.
- Left-click: immediate salvo, in either camera mode. Esc clears lock.
- Restart Play to reset a sunk target. No trial shots or tactical AI.

A turret fires immediately when loaded and its ACTUAL barrels are in the allowed local sector. A rear turret may fire astern while still turning: it never waits or bends shells toward the cursor. Each turret has a 260-degree traverse sector and a 5-second reload. Front turrets exclude the rear 100 degrees; the rear turret excludes the front 100 degrees. Mechanical traverse follows the allowed interval without wrapping through the forbidden sector.

The first aim entry initializes the cyan point at the current prediction if valid. Subsequent entry, observation updates, ship movement and camera motion do not move that world point. A new target does not silently reset the existing aim point.

## Designer parameters
- Ship Weapon: muzzle speed 65 m/s relative to platform, gravity 9.81, configured range 300 m (physical reach still governs), damage 25. Gaussian sigma = 1 + distance * .8 / 100 metres; radial rejection at 3 sigma. Dispersion can be disabled.
- Main turret 1/2/3: independent neutral yaw, traverse arc/speed, elevation speed/limits, aim tolerance, reload.
- Combat Systems / CombatFireControl: observation .5 s. Target motion is only read when observation is due; every-frame extrapolation uses the saved state. A changed/cleared lock discards the observation.
- Yellow prediction region: longitudinal radius 8 m, lateral 4 m, visual smoothing .12 s. Transparent world-space mesh and border; no centre glyph; hidden when prediction is invalid. This is a fixed reference area, not a probability distribution and not weapon dispersion.
- CombatAimController: .15 metres/pixel. CombatCamera: aim FOV 32, distance 65, height 24, smoothing .18 s. Aim view fits the target, prediction and chosen impact point together with a configurable 12 m framing margin.
- Target Destroyer: HP 150, speed 8. Set TestTargetMotion speed to zero for static testing. Player HP 600. Optional TargetBattleship prefab has HP 450 and the same non-tactical motion component. No target weapons or tactical AI.
- CombatEffects: water-column lifetime 1.5 s.

## Architecture and reuse
Ship implements IDamageable and delegates to HealthSystem; PlayerShip and EnemyShip inherit Ship, Destroyer and Battleship inherit EnemyShip. Ship owns Weapon and ShipMovement. PlayerCombatInput calls PlayerShip.FireWeapon -> Weapon.TryFire -> CombatTurret -> Weapon.Spawn -> Projectile. Projectile resolves damage through IDamageable. ShipSinking handles death without expanding the Ship base class. New enemy subclasses can reuse these components.

The existing movement component and its math are unchanged. PlayerShipInput only adds an optional Space shortcut switch (off in this scene). ShipFollowCamera only adds an optional Esc ownership switch (off here). Their old scene behavior remains enabled by default.

CombatBallistics extends the prototype constant-gravity/fixed-speed model: it solves the earliest positive flight-time root for a specified WORLD impact point, including platform velocity, then fires `relativeVelocity + ship.WorldVelocity`. Flight time is an output, not a tuning parameter. Barrel-pivot/muzzle positions are re-solved before an aligned shot. Unaligned shots use current barrel direction with small Gaussian angular scatter. The old Gunnery namespace/assets are not imported or changed.

Continuous segment sphere casts, mathematical sea-plane intersection, owner exclusion and one-shot resolution follow the old prototype approach. Friendly hulls block shells but do not take damage. No armor or damage to specific ship parts. Angular point velocity of individual muzzles is deliberately not added: the contract is the ship root world linear velocity.

The numeric solver uses a bounded time search (120 seconds, .05-second brackets) followed by bisection. The current 300 m envelope is covered by tests; extremely near-tangent solutions at the physical range limit may be rejected safely. No homing, forced misses, per-shot arbitrary speed, or firing-time waits are used.

## Validation
EditMode tests cover moving/static ballistic precision, invalid solutions, arcs, Gaussian distribution, observation timing, intercept consistency, damage limits and OOP regression.
PlayMode tests load actual saved scenes, use Input System virtual devices, and verify immediate fire, six-shell cap, reload, swept collision, friendly protection, moving shots, lock/Shift/manual aim/driving/sinking, plus the existing movement and camera tests. See the delivery report for actual run results; test source alone is not evidence of passing.

Validated on 2026-10-31 through the existing Unity CLI / Pipeline: compilation succeeded; 17/17 EditMode and final combined 13/13 PlayMode tests passed. Saved scene has zero missing scripts, three turrets, six muzzles and player HP 600. Game-view captures confirmed the fixed yellow ellipse without a centre glyph, independent cyan aim and target framing. A runtime salvo produced six water impacts; the integration test also verified six real shell hits sink the 150 HP target and clear lock. Native physical mouse/keyboard playthrough and a standalone build were not performed for this combat integration (Input System event injection was used). Historical Unity Connect token-exchange network errors were observed; final Console ground truth reported zero errors/warnings and compilationFailed=false.
