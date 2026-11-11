# Seamless Gameflow V1.1

Open `Assets/_Game/Scenes/IntegratedGame.unity` and Play. This is the first build scene. Start Game orbits to Preparation; GO makes the 2.5-second stern transition before enabling combat. Back returns to the initial menu. No scene load or player/camera replacement occurs between menu and gameplay. Original MainMenuV2 and BattleLoop remain unchanged.

## Responsibilities
GameFlowController gates existing enabled Gameplay components during MainMenu/Preparation/Transition, makes ship rigidbodies kinematic after their Awake initialization and blocks damage. Time remains one. All references and active ship objects remain intact. It restores only originally enabled behaviours and original rigidbody kinematic flags on Playing. BattleManager Start and enemy AI Start are delayed until combat is enabled. Result is driven exclusively by BattleManager.Ended; existing pause/health blockade remains authoritative.

The UI, camera poses, MenuCameraRig and ShipPreparationController come from MainMenuV2. Its extra ship, environment, camera and EventSystem are not copied. Poses are translated to the actual player spawn. Map, ship model, materials, AI profile and tuning are unchanged. Original battle Transform blocks are identical in the copied scene.

GO uses MenuCameraRig's existing eased polar orbit with `goDuration` (default 2.5 seconds, Inspector). BattlePose is initialized from ShipFollowCamera's actual default yaw/pitch/distance/pivot/sight-elevation and original battle FOV. AdoptCurrentView synchronizes orbit state and clears damping velocity; Start does not overwrite an adopted state. CombatCamera retains its original normal FOV. A single physical Camera and AudioListener render the whole sequence.

ResultUI's optional ReloadRequested callback lets this scene reload itself for Retry (DirectBattle) or Main Menu (Menu). Other scenes retain their original routing. The launch token is consumed by matching scene Awake, cleared on sceneLoaded and subsystem initialization, and cleared on a load exception; it is not a persistent singleton. Retry skips both menu animations and restores fresh battle objects.

## Testing
New real-scene PlayMode test uses Input System virtual mouse clicks and keyboard input. Three loops cover menu inactivity, Back, preparation, GO spam, camera/ship/scene identity, handoff position/rotation tolerance 0.1 and unchanged FOV, WASD movement, firing, Victory, direct Retry, Defeat, Main Menu and next game. Each loop checks one player, camera, listener, EventSystem and manager; max forward speed remains 15. Controlled damage triggers results rather than a claimed manual combat victory. Camera per-frame displacement is bounded by 8 UU during transitions. Native physical mouse feel and standalone builds require manual playtest.

Final validation: new seamless three-loop PlayMode test 1/1, existing player PlayMode 17/17, BattleResult 6/6, tactical occlusion 5/5; existing EditMode 19/19. Game-view captures inspected menu, preparation and stern gameplay. Runtime NPCs entered Patrol with nonzero world speed after handoff. Integrated scene has zero missing scripts. Final compile succeeded. No claim of manual battle completion or standalone build.
