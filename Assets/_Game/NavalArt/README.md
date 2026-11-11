# Tropical Naval Battle — first playable art pass

Open `Assets/_Game/Scenes/TropicalNavalBattle.unity` and press Play. The original greybox scenes and prefabs remain available.

- Player: Bismarck, four twin main turrets (eight shells per full salvo).
- Enemies: one Bismarck and two Gearing destroyers; Gearing has three twin main turrets (six shells).
- Existing movement, health, muzzle velocity, gravity, range, damage per shell, dispersion, reload and traverse/elevation settings are retained. The increased salvo capacities were explicitly approved.
- Every main turret has a yaw pivot, two elevation pivots, real barrel meshes and muzzle transforms. Bismarck barrel sleeves move with the barrels. Secondary guns and torpedo mounts are decorative in this pass.
- The HUD and weapon loop now enumerate the actual battery instead of assuming three turrets. Existing three-turret prototypes retain their behavior.
- Enemy reset prefabs retain the imported art; F6 restores each ship and its scene patrol route.

Environment: sunny procedural sky, textured ocean normal map, three authored island meshes, sand/grass textures, rock models and 120 palms from the supplied Tropical Environment package. Island collision uses separate simplified convex meshes compatible with the existing AI. This is an initial environment pass, not the original package's demonstration terrain. Ocean shading is static; wakes, animated waves and detailed shoreline blending are not included.

Controls remain those of the combat prototype: WASD to drive, mouse to aim/orbit, LMB to fire, RMB to lock, hold Shift to aim, wheel to zoom, Esc to unlock, R to reset orbit; F6 resets enemies and F7 hides AI debug.

## Background asset pipeline

Source downloads are never modified. The selected OBJ materials/textures are copied into `Source/Bismarck` and `Source/Gearing`. Mesh streams are converted into native Unity meshes under `Meshes`, with URP materials under `Materials`.

1. Run Blender in background mode with `Tools/NavalArt/inspect_models.py` to inspect and cache welded components under ignored `Artifacts/NavalArt`.
2. Run Blender with `Tools/NavalArt/prepare_assets.py`. `-- --ships-only` skips environment extraction. The script is specific to the inspected source models and splits their known gun geometry; it is not a universal automatic rigging tool.
3. Unity batch method `GrandNavigation.Gameplay.Art.Editor.NavalArtBuilder.Build` creates the scene once and refuses to overwrite an existing scene.
4. `FinishAndCapture` applies environment details, checks original gameplay parameters, captures offscreen screenshots and removes unused environment imports. It replaces generated palm groves; use it before manual environment editing.
5. `RefreshShipsAndCapture` rebuilds the generated ship art and reset prefabs from the mesh streams, then runs step 4. It replaces the generated ship visual hierarchies; use deliberately after mesh changes.

Evidence is in ignored `Artifacts/NavalArt`: `parameter-verification.txt`, `playmode-results.xml`, Unity logs and preview PNGs. The PlayMode filter `NavalArtPlayTests` verifies both broadsides, elevation, all muzzle emissions, reload, actual damage, reset prefabs and island collision compatibility. Relevant existing combat/HUD/AI tests were also run as regression coverage.

The full-resolution source ships are retained for this first functional pass. No target-hardware frame-rate claim is made; LOD and material batching can be addressed when the intended hardware and simultaneous ship count are known.
