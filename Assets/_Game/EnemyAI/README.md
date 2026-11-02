# Enemy AI Greybox V0.1

Open `Assets/_Game/Scenes/EnemyAIGreybox.unity` and Play. Existing WASD, mouse, RMB lock, held Shift aim, LMB salvo remain unchanged. F6 recreates all NPCs; F7 toggles AI debug. The right-hand debug panel can reset each NPC separately or reload this test scene (including the player).

## Source and scope
Read all populated rows of the sole `Sheet1` worksheet in `FSM_NPC_完整版.xlsx` (supplied desktop workbook). It explicitly enables Retreat and specifies the small-ship lookahead as **8 seconds**, overriding the earlier prompt's 7. The workbook calls the smaller class a cruiser; the requested existing OOP class `Destroyer` implements that small-ship configuration. No carrier attacks, coordinated ally support, healing or game-end flow.

The scene is copied from the saved LevelGreybox at commit 339e35a. The original scene, islands (including child offsets), movement and player combat configuration are retained. Only test enemies and the old static-target debug component are replaced in the new scene. The AI scene is appended to the build scene list to support its debug reload button; no existing entry is removed or reordered.

## Inspector configuration
`EnemyAI/Profiles/Battleship.asset` and `Destroyer.asset` hold the 25 design parameters. Small ships share read-only configuration, never runtime FSM/target/random/recovery state. Each NPC has its own `EnemyShipAI` (profile + waypoints), `EnemyNavigation` (mask, map extent, steering and bounded recovery settings), existing ShipMovement and Weapon. Make a separate profile asset if an individual ship needs different tuning.

| Parameter | Battleship | Destroyer |
|---|---:|---:|
| Detection / engage enter / exit (UU) | 1200 / 900 / 1000 | 600 / 400 / 500 |
| Preferred distance / tolerance (UU) | 750 / 100 | 300 / 50 |
| Decision / minimum state / forget (s) | 1 / 3 / 8 | .5 / 2 / 5 |
| Retreat HP ratio | .2 | .3 |
| Patrol / approach / combat / retreat speed (UU/s) | 10 / 15 / 15 / 15 | 20 / 25 / 20 / 20 |
| Waypoint / ally arrival radius (UU) | 80 / 180 | 60 / 120 |
| Broadside angle / switch delay | 80 deg / 20 s | 70 deg / 12 s |
| Reaction / aim error / aim tolerance | 2 s / 24 UU / 3 deg | 1 s / 15 UU / 5 deg |
| Avoid lookahead / clearance | 12 s / 60 UU | 8 s / 35 UU |
| Stuck timeout / minimum forward progress | 8 s / 25 UU | 5 s / 20 UU |

NPC movement capability: large 15 UU/s, 6 deg/s yaw; small 25 UU/s, 12 deg/s yaw. HP 450 / 150. Large has three twin turrets, small two; all use existing 260-degree traversal, real reloads, 140 UU/s relative muzzle speed, 9.81 gravity, inherited ship velocity, 1200 UU range and Gaussian dispersion. Player remains 15 UU/s with its original six-gun system.

## Implementation boundaries
- Patrol -> Approach on visible hostile player or attributed damage. Only Approach/Engage distance transitions obey minimum state duration. Separate enter/exit thresholds provide hysteresis.
- Visibility requires sensor range and physical line of sight. Lost targets retain last confirmed position; hidden live coordinates/velocity never drive navigation or lead. Obstruction immediately cancels shooting. Reacquisition restarts reaction delay.
- Low HP immediately enters latched Retreat, before target-loss/range transitions. Nearest live ally is a destination, not a support order. No ally: move away from last known threat. Destroyed immediately cancels commands; existing ShipSinking handles the single sinking presentation. Existing shells continue.
- EnemyNavigation only calls ShipMovement command methods. A small visibility graph routes around the actual static island Collider bounds (no hardcoded island coordinates). Clearance sphere sweeps include hull extent and configurable margin; continuous local steering and braking predict obstacles/bounds. Passing side persists. Progress is projected along the navigation direction over a window. Limited reverse attempts are followed by stopped replanning, with new reverse attempts requiring physical progress. No runtime navigation teleport.
- AI lead uses existing CombatBallistics. One uniform disk offset is held until a successful salvo, then resampled for the next command; each barrel's Gaussian scatter remains independent. Eligible turrets may fire as a subset; ineligible turrets are never forced to fire with the rest. AI-specific gating is passed to Weapon.TryFireAimed, preserving player TryFire behavior. Straight LOS and segmented arc checks reject terrain/allies before firing.
- Shared code changes are limited to optional source-aware Ship damage notification, passing projectile owner through that path, and an additional guarded Weapon firing/clear-aim API. IDamageable.TakeDamage(int) remains intact.
- Reset instantiates a fresh prefab per slot, restoring health, movement, weapon reload, sinking presentation and FSM without affecting other NPCs. Alive-component recycling also clears FSM memory on re-enable.

## Validation
Actual Unity 6000.3.27f1 CLI/Pipeline results: AI EditMode 4/4; AI PlayMode 8/8 (rerun after navigation fixes, including initial clearance overlap and the saved three-ship layout); existing player EditMode 18/18 and PlayMode 16/16; TargetLock PlayMode 5/5. Compilation succeeded. Loaded-scene comparison confirmed identical island root/child world positions and sizes, no missing scripts, player maximum speed 15 and weapon range 1200. EditMode covers hysteresis, minimum hold time, priority, uniform disk error, configuration rejection and projected-progress thresholds. PlayMode covers source-aware acquisition, retreat latch/reset, visibility/memory/reaction, NPC real damage, player projectile sinking, movement shooting, local avoidance/bounds, recovery, ally reselection and patrol waypoints. Existing player tests cover movement/camera/fire/OOP and target-lock regression.

Limitations: static-island corner routing plus local avoidance is a bounded greybox controller, not a general-purpose navigation framework. Long multi-ship congestion runs, all simultaneous island/ally arrangements, standalone builds and manual combat balance remain playtest work. Automated hit tests disable AI error/weapon scatter to validate execution; default randomized hit rate is not a guarantee or a balance result. No claim of perfect navigation in arbitrary edited layouts.

Final saved-layout run (120 simulated seconds, real Unity physics): NPC_3 travelled 991.93 UU / longest stop 4.02 s; NPC_2 travelled 1052.80 UU / longest stop 4.00 s; Battleship travelled 1219.42 UU / longest stop 4.54 s. Current Console: 0 errors, 0 warnings. These short-run results do not establish arbitrary-layout or long-session congestion guarantees.
