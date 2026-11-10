# Main Menu V2 — 3D whitebox

Scene: Assets/_Game/Scenes/MainMenuV2.unity. Open and press Play.
This feature is based on main's clean Unity baseline; it has no GunneryPrototype dependency.

## Flow
- Start Game: hide menu UI, orbit and rise around the same ship for 3.5 seconds, then show Ship Preparation.
- GO: displays "Gameplay scene not implemented yet." No scene is loaded.
- Back: reverse the continuous orbit and return to the main menu.
- Exit: quits a standalone player; the Editor displays a message without closing.
- Buttons are unavailable during transitions; repeated requests are ignored.

## Inspector
- MenuCameraRig: duration (3.5 s), easing curve (Ease In Out), transitionClearance (12 m).
- MenuPose / PreparationPose: transform position and rotation, lookTarget and fieldOfView.
- One real Camera follows a polar orbit around OrbitPivot, interpolating radius, height, target, rotation offset and FOV. The middle of the orbit pulls back to keep the entire ship visible.
- PreparationPose observes down the longitudinal axis, bow at the bottom of the screen.
- Background rendering remains enabled; individual recovery frames are capped to avoid skipping the orbit after focus changes.
- MenuFlowController: gameplaySceneName="Gameplay" is reserved for future navigation; GO intentionally never loads a scene in this prototype.
- ShipPreparationController: model slot plus preparationOpened/preparationClosed events for later integration. No upgrade data or gameplay framework is implemented.

## Replace the ship
MenuBattleship.prefab contains ShipRoot / ShipModelSlot / PlaceholderBattleship.
Replace the visual child under ShipModelSlot; retain ShipRoot and ShipModelSlot.
The ship's bow points along +Z. Camera poses and pivot are independent scene objects.
ShipIdleMotion moves only the visual child with a very slight bob and roll; it never moves the camera pivot.

## Assets
Runtime scripts, materials, prefab, editor builder and tests are inside Assets/_Game/MainMenuV2/.
The builder refuses to overwrite an existing scene or material.
Build Settings' shared scene list starts with MainMenuV2 and retains the original SampleScene.
No packages, Unity version, experimental feature branches or remote repository are changed.

## Verification
Run:
    unity command test run --mode playmode --filter GrandNavigation.MainMenuV2.Tests --async_tests true --project-path D:\UnityHub\GrandNavigation
    unity command test status --project-path D:\UnityHub\GrandNavigation

MenuFlowTests uses actual Input System mouse input for Start, GO, Back and Editor Exit.
It covers two complete round trips, rapid repeated transition requests, stable scene/camera/ship identity, and no GO navigation.
A second test checks 241 samples across the complete orbit at 1280x720 and 1920x1080: ship bounds remain in frame, camera position/rotation remain continuous, and the bow finishes below the stern.
Standalone player build and platform-level process exit have not been tested.

Validation completed 2026-10-30:
- Unity recompile: 0 errors, 0 warnings.
- PlayMode: 2/2 passed after orbit-framing and background-frame fixes.
- Visual checks: main view, actual mid-orbit frame, and preparation view; screenshots in ignored Temp/menu-v2-*.png.
- Runtime Console: 0 errors / 0 warnings.
