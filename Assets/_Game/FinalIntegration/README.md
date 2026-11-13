# Final 3D integration

Base: `level-art` / `1e5fd09`. Branch: `feature/final-integration`.
Open `Assets/_Game/Scenes/FinalIntegratedGame.unity`, Play. It is first in the shared build scene list. Original scenes and Battle2D remain available.

## Scope and preservation

FinalIntegratedGame is a copy of IntegratedGame, with FreeNavalBattle visual hierarchies/materials/water reused. No art asset regeneration, package upgrade, or AI algorithm change. The existing real player, controllers, camera, menu poses, battle roster and result UI remain authoritative.

Bismarck player and NPC: four twin batteries, eight rounds. Gearing NPCs: three twin batteries, six rounds. NPC battery counts follow the user's clarification. Original per-shell damage, reload, arcs, elevation/traverse rates, velocity inheritance and dispersion are preserved. New reset prefabs contain original NPC gameplay components/profile references and updated art; scene routes are assigned by the existing debug reset mechanism.

Scene inspection compared original and final four ships: identical world spawn pose, movement settings JSON, health/weapon scalar parameters, EnemyShipAI/EnemyNavigation scalar parameters, identical EnemyAIProfile assets and waypoint world positions. No differences found. Player maximum forward speed remains 15.

Island roots: A(-450,0,40), B(320,0,570), C(70,0,310). Original roots retained, FreeNavalBattle visual children and 24 convex coastal sectors copied. Original hull Colliders retained. Long-term navigation around the detailed coast still needs human playtesting.

## Prediction and water compatibility

PredictionRegion remains the source of radius, direction, displayed centre and smoothing. New optional screenOverlay disables its world mesh, and CombatHUD reads that component instead of hardcoded 8/4 radii or raw prediction data. Final scene retains longitudinalRadius=24, lateralRadius=12, smoothTime=0.12; fire-control observations=0.5 seconds, visibility checks=0.1, loss delay=1.5, lock distance=1200. No fire-control solver or aim-input changes. Visibility is gated by valid, visible lock state. Older scenes with an intentionally disabled world region can activate the same region as an overlay; ordinary old scenes keep their world region.

GameFlowController/BattleManager exclude the existing Gameplay.Water presentation namespace from gameplay-component pausing. This keeps FFT/render-pipeline ownership alive during menus and results; driving, AI, weapons, damage and results retain existing gates. WaterSceneContext still restores the previous pipeline on scene unload. Shared URP assets and 2D code/resources are not edited.

## Actual validation

- Unity CLI compile passed with no compilation errors.
- FinalFlowTests: passed two full loops with Input System virtual mouse UI clicks, menu/preparation/GO continuity, camera handoff, driving, firing, victory, Retry, defeat and Main Menu; one player/camera/listener/EventSystem/manager checks.
- FinalCombatTests: two tests passed. Eight real player projectiles, reload gate, imported-muzzle alignment/elevation and actual target damage; NPC battery counts; Shift state; prediction overlay without world duplicate; occlusion hides overlay, temporary recovery, 1.5-second loss unlock.
- Water/island check: FFT ready and advancing, supported water shader, 24 convex island sectors, all three islands block near-waterline rays, water-impact callback fires, original NPC can move on its route.
- Initial integration test exposed overwritten muzzle references during editor JSON copying. Fixed by preserving independent barrel/muzzle arrays and revalidated the saved scene/reset prefabs. No null-muzzle exception remained in passing tests.
- Original IntegratedGame and FreeNavalBattle files remain unchanged; no modifications in Battle2D.

## Remaining human checks

No standalone build or target-hardware performance certification. Review visual composition in both menu stages, wave/shore appearance, full-map long-duration AI navigation, shell/model contact readability, and manual Shift/lock handling. Existing imported source Terrain tree-reference / FBX LOD warnings are not repaired by this task. Models and imported shaders retain their existing source/licensing requirements.

The pre-existing uncommitted Rock9_MAT.mat and three TerrainLayer/meta pairs remain outside this commit; no teammate work is included or discarded.

Final checks: Battle2DTests 5/5 passed, including input, movement/collision, real damage, both AI types, victory/defeat and three retries. Final scene static missing scripts=0, Camera=1, AudioListener=1. Actual Game View reviewed in menu and preparation: real Bismarck and animated water visible, UI present. Target-hardware performance and standalone builds remain unverified.
