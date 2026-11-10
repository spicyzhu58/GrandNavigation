# Battle Result V0.1

Open `Assets/_Game/Scenes/BattleLoop.unity` and Play. Existing WASD / mouse / right-click lock / held Shift aim / left-click salvo are retained. Sink the three listed enemies to win; player HP zero loses. Retry reloads the whole scene. Main Menu is deliberately disabled with an empty `ResultUI.ReturnToMainMenu()` integration point.

## Lifecycle
BattleManager's Inspector `player` and `participants` explicitly define the opening roster. Duplicate, friendly and inactive entries are excluded. Enemy HealthSystem.Died callbacks record a unique health reference; records survive sinking/destruction. An alive enemy destroyed by external code is not considered killed and can leave the battle incomplete: use actual damage/death, not Destroy, to remove a required combatant. An empty roster warns and cannot win automatically.

At late execution order 10000, LateUpdate checks player death before all-enemy death; no callback commits a result. Once decided, the result cannot change. A one-time scene-local pass disables existing Gameplay behaviours (input, cameras, AI, weapons, projectiles and debug), blocks HealthSystem damage, pauses scaled time and releases the cursor. uGUI/EventSystem remain active. This deliberately freezes sinking visuals too. No persistent singleton or cross-scene registration is used.

Retry restores timeScale to one and reloads BattleLoop using SceneManager. Its saved scene creates fresh HP, movement, weapons, AI, roster, lock/cache, effects and UI. Manager destruction unsubscribes death events and releases its pause. Existing prototype scenes have no manager and preserve normal damage behavior; HealthSystem.DamageBlocked defaults false.

## Scene preservation
BattleLoop is a saved copy of EnemyAIGreybox. All existing serialized Transform blocks are identical. Existing component differences only serialize the latest previously implemented camera/occlusion defaults and disable EnemyAIDebug in this scene, preventing F6/debug replacement of the fixed opening roster. No map, ship, weapon or AI tuning changed. Build settings append BattleLoop without changing the original startup scene.

## Validation
Unity compilation passed. Six new real-scene PlayMode tests passed: partial kills/victory/frozen damage, both same-frame death orders, live destruction without false kill, duplicate/empty roster, and three sequential retries through uGUI pointer-click dispatch while paused. Tests verify fresh object counts, HP, player spawn/target speed, AI Patrol, cleared shells and target lock, single result display and disabled menu. Pointer-click dispatch exercises the actual Button listener; native physical mouse playthrough and a standalone build remain manual validation.

Final regressions: existing player EditMode 19/19, player PlayMode 17/17, EnemyAI PlayMode 8/8 passed. Final Console ground truth: zero errors/warnings, compilationFailed=false. Game-view capture visually verified Victory overlay and both buttons. At timeScale zero, InputSystem UI module and click action were active and UI raycast hit Retry. No native physical mouse or standalone build claim.
