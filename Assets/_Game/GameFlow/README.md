# Game Integration V1.0

Open `Assets/_Game/Scenes/MainMenuV2.unity` and Play. Start Game preserves the animated Ship Preparation transition; GO loads `Assets/_Game/Scenes/BattleLoop.unity`. Sink the registered enemies to win; player death loses. Retry reloads BattleLoop; Main Menu returns to Stage A. The original prototype scenes remain available, but are not the GO destination.

## Configuration and lifecycle
`GameSceneRoutes.asset` holds both full scene paths. The existing MenuFlowController and ResultUI reference this one asset. Both use guarded single-scene asynchronous loading; no persistent manager or new navigation framework is added. Time is restored before GO, Retry and Return; menu Awake also resets time and unlocks/shows the cursor. Battle camera Start restores combat cursor ownership. Result pause releases it. Old scene objects and listeners are destroyed rather than retained across loads.

The normal merge commit preserves menu and battle histories. The sole merge conflict was the build scene list: MainMenuV2 is first, and all previous entries are retained. No package, prefab, map layout, AI configuration, combat parameter or core Gameplay script changed. Existing scene Transform/RectTransform blocks match their source branches exactly.

## Verification
PlayMode integration tests use Input System virtual mouse events routed through real uGUI buttons. Three complete loops exercise Start, the original transition, GO, real movement/fire, Victory, Retry, Defeat, Main Menu and another game. Assertions cover one camera/listener/EventSystem, fresh player/AI/manager counts, HP and lock reset, time scale, cursor mode and player max speed 15. Controlled damage triggers victory/defeat for reproducibility; this is not a claim of three manually played combat victories. A separate test sends 20 GO / Return commands in one frame and confirms exactly one scene load per command group and Victory -> menu.

Native physical mouse playthrough and standalone builds are still manual acceptance tasks. Earlier MainMenuV2/BattleResult READMEs describe their isolated historical versions; their disabled navigation notes are superseded by this integration.

Final verification: integration 2/2, battle-result 6/6, menu 2/2, tactical occlusion 5/5 and assisted target selection 5/5 PlayMode tests passed (20 total). Compilation succeeded; final Console zero errors/warnings. Both scenes have zero missing scripts. No standalone build or native physical mouse playthrough performed.
