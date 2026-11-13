# GrandNavigation — AP6412 Group 4

A single-player naval combat game with independently playable **2D and 3D versions** in Unity. Steer a warship, aim and fire salvos, and sink all designated enemies. Player destruction causes defeat; if the player and last enemy die in the same frame, **defeat takes priority**.

**Final accepted gameplay snapshot:** [`final-submission`](https://github.com/spicyzhu58/GrandNavigation/tree/final-submission), commit [`6274d85`](https://github.com/spicyzhu58/GrandNavigation/commit/6274d8549e8a8b4e80ced9831b9aa0dbc0b7025d). Both versions were accepted by the project owner in the development conversation on **13 November 2026**. The acceptance statement does not specify standalone build mode or measured performance. Main may contain later documentation commits; the release tag remains the frozen gameplay snapshot.

This README updates the [team-authored level-art README](https://github.com/spicyzhu58/GrandNavigation/blob/ab52e7b/README.md) for the actual integrated release. The earlier art snapshot `3d-final` is **not** the latest seamless 3D release.

## 1. Team and repository

- Course: **AP6412 — Foundation for Technology & Coding**
- Group: **4**
- Repository: https://github.com/spicyzhu58/GrandNavigation
- Route: **B — playable 2D and 3D versions of the same game**, with the chronology deviation disclosed below.

| Member | Responsibilities recorded in the team README | Development branches / evidence |
|---|---|---|
| KONG JINCAN — G2601963A (`spicyzhu58`) | Gameplay requirements, movement, combat, enemy AI, flow and 2D adaptation | [feature/ship-movement](https://github.com/spicyzhu58/GrandNavigation/tree/feature/ship-movement), [feature/player-combat](https://github.com/spicyzhu58/GrandNavigation/tree/feature/player-combat), [feature/battle-2d](https://github.com/spicyzhu58/GrandNavigation/tree/feature/battle-2d), [feature/final-integration](https://github.com/spicyzhu58/GrandNavigation/tree/feature/final-integration) |
| Ye Kaixin — G2607926G (`yekaixin113`) | Technical art: local water prototypes, ship/turret adaptation, level layout and collision, rendering/effects integration, visual debugging and delivery verification | [level-art](https://github.com/spicyzhu58/GrandNavigation/tree/level-art); `38de1de`, `88f02ed`, `1e5fd09` |

Responsibilities are retained from the contributor's README. Imported assets keep their original credits; commit history alone does not establish authorship of every asset. Each student must supply their own reflection and full AI history.

## 2. Engine and dependencies

- **Unity 6000.3.27f1**, C#; target delivery platform: Windows x86-64.
- **URP 17.3.0**, Input System **1.20.1**, Cinemachine **3.1.7**, Unity Test Framework **1.6.0**, uGUI **2.0.0**.
- Additional recorded packages include AI Navigation **2.0.16**, Unity Pipeline **0.9.1-exp.1**, Timeline **1.8.13** and Visual Studio Editor **2.0.26**. See [manifest](Packages/manifest.json) and [package lock](Packages/packages-lock.json) for the complete resolved dependencies. Installation does not imply every package is used by every scene.
- Install the Windows build support module through Unity Hub when building for Windows. Unity restores declared packages on first import; this may require network access.
- The final 3D ocean uses compute shaders, FFT, SSR and shallow-water rendering. Use a compatible graphics device; water GPU tests require graphics enabled, not `-nographics`.
- No external model-processing tools, original `E:` source directories, or Unity CLI installation are required for normal play. Import tools and source manifests document the development workflow.

## 3. Obtain and run both versions

Clone the repository or extract the submitted project. For the frozen release, use:

```bash
git clone https://github.com/spicyzhu58/GrandNavigation.git
cd GrandNavigation
git switch --detach final-submission
```

In Unity Hub, **Add project from disk**, select the folder containing `Assets`, `Packages` and `ProjectSettings`, and open with Unity **6000.3.27f1**. Wait for package resolution and asset import. Open a scene below and press **Play**.

| Version | Actual scene path | Complete flow |
|---|---|---|
| **Final 3D** | [`Assets/_Game/Scenes/FinalIntegratedGame.unity`](Assets/_Game/Scenes/FinalIntegratedGame.unity) | Main Menu → Start Game → Ship Preparation → GO → continuous stern-camera transition → Gameplay → Victory / Defeat → Retry / Main Menu |
| **Playable 2D** | [`Assets/_Game/Scenes/Battle2D.unity`](Assets/_Game/Scenes/Battle2D.unity) | Start Game → Gameplay → Victory / Defeat → Retry → Start Game; Quit exits the standalone game |

The final 3D menu displays the same real Bismarck PlayerShip used in combat. GO does not load another scene. Retry reloads FinalIntegratedGame directly into battle; Main Menu reloads it in menu mode. In the Editor, the 2D Quit button stops Play; the 3D menu Exit shows an Editor notice.

`IntegratedGame`, `FreeNavalBattle`, `TropicalNavalBattle`, `WaterIntegrationTest`, and earlier prototypes remain available as development/regression scenes. **Use FinalIntegratedGame for the final 3D demonstration**, rather than those earlier scenes.

## 4. Controls and objectives

| Action | 2D | 3D |
|---|---|---|
| Increase / decrease target speed | W / S; release to hold target speed | W / S; release to hold target speed |
| Rudder | A / D; release to centre | A / D; release to centre |
| Aim / observe | Mouse world position | Mouse observation and aim command |
| Aim view | Not implemented | Hold Shift; release to return |
| Lock enemy | Right-click an enemy | Right-click enemy at the screen-centre pointer; screen-space selection assistance |
| Clear target | Esc | Esc |
| Fire salvo | Left mouse button | Left mouse button |
| Zoom | Mouse wheel | Mouse wheel |
| Reset observation camera | — | R |

Win by sinking every designated enemy. Losing the player ship causes defeat. Locking and prediction provide information, not guaranteed hits. Reload and actual collision govern damage. There is no separate trial-fire mechanism.

3D player and NPC Bismarck use **four twin main turrets / up to eight shells per salvo**; Gearing destroyers use **three twin turrets / up to six**. Individual turrets obey firing arcs and reload. Player maximum speed is **15 UU/s**. The final NPC configuration retains the original gameplay profiles, spawn positions and routes at integration time; the release includes the owner's later scene-position adjustments in `6274d85`.

## 5. Build two standalone versions

1. Save the scene and exit Play. Open **File → Build Profiles** and select **Windows**, x86-64.
2. For a **3D build**, enable **only FinalIntegratedGame** in Scene List, as scene 0. All menu, battle and result stages are in this scene.
3. For a **2D build**, enable **only Battle2D**, as scene 0. Use separate Build Profiles with scene-list overrides if preserving both configurations.
4. Disable Development Build for the delivery build. Build into separate folders, for example a `3D` folder and a `2D` folder **outside the Unity source project**.
5. Distribute the **entire build folder**, including the executable, `_Data` folder, DLLs and other companion files. Verify start, movement, fire, damage, AI, victory, defeat and Retry in each executable.

These are build instructions, not claims that those example output folders or verified binaries already exist. Actual standalone build paths, operator, mode and performance evidence still need to be entered in the submission package. Do not commit `Library`, `Temp`, `Obj`, logs or generated build outputs.

## 6. Architecture and shared rules

The 3D hierarchy is `Ship : MonoBehaviour, IDamageable`, with `PlayerShip : Ship`, `EnemyShip : Ship`, and `Battleship` / `Destroyer : EnemyShip`. Ship holds HealthSystem, Weapon and ShipMovement; player input or AI issues commands to those components; Weapon uses Projectile. Damage is delegated through `IDamageable.TakeDamage(int)` to HealthSystem. BattleManager resolves results in a unified frame stage with defeat priority.

2D uses `ShipActor2D` plus ShipMovement2D, PlayerInput2D, Weapon2D, Projectile2D, EnemyAI2D, Camera2D and BattleManager2D. It directly shares **IDamageable, HealthSystem, MovementSettings, ShipMovementMath, GunneryMath, EnemyAIProfile and EnemyAILogic**, plus state enums. It does not reuse the entire 3D Ship inheritance chain or BattleManager implementation.

| Responsibility | 3D execution | 2D execution |
|---|---|---|
| Coordinates and physics | XZ, Rigidbody / Collider / Physics | XY, Rigidbody2D / Collider2D / Physics2D; local +Y bow |
| Presentation | Meshes, perspective camera, water effects | Sprites, orthographic follow camera |
| Gunnery | Fixed relative muzzle speed, gravity, ship-velocity inheritance, turret arcs | Straight-line bullets, shared Gaussian sampling, reload and collision damage |
| AI | Full existing FSM, sensing and navigation | Shared transitions with simplified local navigation; Patrol / Approach / Engage / Destroyed |
| Win / lose | Designated enemies eliminated, player death, defeat priority | Same objectives and result rules through a 2D lifecycle adapter |

Final 3D prediction retains **0.5-second observations, per-frame extrapolation, 24/12 UU ellipse radii, 0.12-second display smoothing, 0.1-second visibility checks and 1.5-second occlusion grace**. PredictionRegion supplies configuration and smoothed presentation data; CombatHUD projects one overlay and hides it when visibility is invalid. Water presentation remains active during menus/results; gameplay is gated separately.

Detailed implementation notes: [2D and initial Class/Change Map](Assets/_Game/Battle2D/README.md), [final integration](Assets/_Game/FinalIntegration/README.md), [seamless flow](Assets/_Game/Seamless/README.md), [naval art](Assets/_Game/NavalArt/README.md), [water integration](Assets/_Game/WaterIntegration/README.md).

### Ye Kaixin — technical-art development and integration

The TA work followed **local prototype → isolated integration scene → playable art scene → final game-flow integration**. Ye Kaixin developed and assembled the local visual work, specified the integration constraints, reviewed the results and directed corrections with AI-assisted implementation. Local prototype activity is evidenced by the development conversations and source manifests; it must not be assigned invented Git commits. Third-party source art and code remain separately attributed.

| Stage | Implemented work and design decision | Code / evidence |
|---|---|---|
| Local water prototypes | Combined the appearance, SSR and shallow-water interaction of the contributor's local Stylized Water project with FFT from the separate Water Asset project. Adapted the newer source rendering APIs to Unity 6000.3.27f1 / URP 17.3.0, preserving the game engine version. | [Source paths and hashes](Assets/_Game/WaterIntegration/source-manifest.json), [water implementation notes](Assets/_Game/WaterIntegration/README.md) |
| Functional ship art | Built TropicalNavalBattle using Bismarck/Gearing visuals. Separated turret housings and barrels, set yaw/elevation pivots and muzzle references, adapted materials and retained gameplay components. Ye Kaixin explicitly approved all modelled main turrets firing: eight Bismarck shells and six Gearing shells. This drove the removal of the fixed three-turret/six-shell assumption. | [38de1de](https://github.com/spicyzhu58/GrandNavigation/commit/38de1de), [asset conversion](Tools/NavalArt/prepare_assets.py), [Weapon](Assets/_Game/ShipMovement/Runtime/Weapon.cs) |
| WaterIntegrationTest | Ported water appearance first, then FFT and local ship/projectile interactions. NavalFFT schedules waves; WaterSceneContext connects rendering ownership and gameplay water events. Kept movement, damage and mean-sea-plane ballistics stable; no physical buoyancy was added. | [NavalFFT](Assets/_Game/WaterIntegration/Runtime/NavalFFT.cs), [WaterSceneContext](Assets/_Game/WaterIntegration/Runtime/WaterSceneContext.cs); historical module record: 11 tests passed |
| FreeNavalBattle level assembly | Adapted source Terrain height data to three islands at the existing game scale, built 24 convex coastal collision sectors, checked spawn clearance and patrol routes, then added 195 terrain-grounded palms and 72 rock placements with three LOD levels. Refined cliff materials, shoreline attenuation, reflections and caustics. | [88f02ed](https://github.com/spicyzhu58/GrandNavigation/commit/88f02ed), [terrain builder](Assets/_Game/NavalArt/Editor/FreeBattleTerrainBuilder.cs), [finishing script](Assets/_Game/WaterIntegration/Editor/FreeBattleFinish.cs) |
| Visual bug and lifecycle fixes | The yellow prediction region was hidden by displaced water. Moved its visible outline to a HUD overlay while preserving lock/visibility validity. A regression also exposed scene-specific pipeline settings leaking into global Editor configuration; restricted switching to Play mode and restored the previous pipeline on exit. | [FreeBattle notes](Assets/_Game/WaterIntegration/FreeBattle.md), [CombatHUD](Assets/_Game/ShipMovement/Runtime/Combat/CombatHUD.cs); historical final run: 22/22 passed after a 21/22 run and correction |
| Project handoff and final integration | Ye Kaixin's art/water work was consolidated on level-art; 1e5fd09 merged the independent 2D version. Kong Jincan's final integration reused those visual hierarchies in the seamless menu/preparation/battle scene, preserved the original AI and added final flow/combat checks. Later scene-position adjustments are frozen in 6274d85. | [1e5fd09 merge](https://github.com/spicyzhu58/GrandNavigation/commit/1e5fd09), [5bf140b integration](https://github.com/spicyzhu58/GrandNavigation/commit/5bf140b), [final integration notes](Assets/_Game/FinalIntegration/README.md) |

The final integration further makes PredictionRegion the source of overlay radii and smoothing and excludes water presentation from gameplay pausing in GameFlowController/BattleManager. These later integration changes are credited separately from Ye Kaixin's earlier TA work. Existing module test results are stage-specific; the 45-second AI/GPU check and Camera.Render timing do not certify standalone FPS.

Design evidence: visual water and collision/ballistics have distinct responsibilities; scene-level rendering ownership is restored on unload; actual turret configuration replaces fixed battery assumptions. These are concrete separation and extensibility decisions, with a deliberate limit: ship components still depend on Unity and the 2D execution adapters remain separate. Imported shaders, new TA adapters, editor tools and scene wiring are outside the fixed 16-type CCR inventory, so that metric does not measure the total TA workload.

Presentation: [current Canva deck](https://www.canva.com/d/3QiZXnjsSg5P35N). Its TA section documents local prototypes, WaterIntegrationTest, FreeNavalBattle and the Git integration milestones. The deck's previously edited 6.25% value compares cac76c4 to 1e5fd09; update the final-release slide to 12.5% if presenting 6274d85, and keep 6.25% only as the earlier milestone. The deck itself was not edited in this README update.

## 7. Git versions, comparisons and integration evidence

| Evidence | Link / exact scope |
|---|---|
| Current main | [main](https://github.com/spicyzhu58/GrandNavigation/tree/main) |
| **Final accepted 2D + integrated 3D release** | [final-submission](https://github.com/spicyzhu58/GrandNavigation/tree/final-submission) → `6274d85` |
| Independent 2D snapshot | [2d-final](https://github.com/spicyzhu58/GrandNavigation/tree/2d-final) → `cac76c4` |
| Earlier 3D art snapshot (superseded for final delivery) | [3d-final](https://github.com/spicyzhu58/GrandNavigation/tree/3d-final) → `1e5fd09` |
| Real pre-2D 3D baseline | [b3c5b41 baseline commit](https://github.com/spicyzhu58/GrandNavigation/commit/b3c5b41280373e7d17e48f680f9daf84f6379cb1); the historical tag name is not present on the remote |
| Initial adaptation change | [b3c5b41 → 1e4d21a](https://github.com/spicyzhu58/GrandNavigation/compare/b3c5b41...1e4d21a) |
| Final endpoint file comparison | [cac76c4 → 6274d85](https://github.com/spicyzhu58/GrandNavigation/compare/cac76c4..6274d85) |
| Final 3D integration changes | [1e5fd09 → 6274d85](https://github.com/spicyzhu58/GrandNavigation/compare/1e5fd09...6274d85) |
| Existing merge commit preserving both histories | [1e5fd09](https://github.com/spicyzhu58/GrandNavigation/commit/1e5fd09783eeee48eed29ecaa0e5fc91755fc4a6), integrating 2D into level-art |
| Final integration branch history | [feature/final-integration](https://github.com/spicyzhu58/GrandNavigation/commits/feature/final-integration) |
| Main history | [main commits](https://github.com/spicyzhu58/GrandNavigation/commits/main) |

Main was **fast-forwarded** from `edf3f0e` to `6274d85` and pushed normally. No PR or additional merge commit was created for that operation; it must not be presented as a PR review. Feature branches remain intact. The older `level-art` documentation was not merged wholesale into main; links below identify its separately authored evidence.

### Actual chronology and CCR

Actual development was **3D → additive 2D adaptation → final 3D art integration**. Route B requests 2D frozen first, then 3D. This is a real mismatch requiring teacher confirmation; tags and documentation do not reverse history. No historical commits were rewritten or backdated.

CCR here means **modified/deleted baseline core types ÷ a fixed inventory of 16 baseline core types**. New adapters, presentation, tools, tests and enums are excluded. Inventory: IDamageable, HealthSystem, MovementSettings, ShipMovementMath, Ship, PlayerShip, EnemyShip, Destroyer, Battleship, Weapon, Projectile, CombatBallistics, GunneryMath, EnemyAIProfile, EnemyAILogic, BattleManager.

- Historical 3D-to-2D adaptation: **0/16 = 0%**, `b3c5b41 → cac76c4`.
- Current final endpoint comparison: **2/16 = 12.5%**, `cac76c4 → 6274d85`: Weapon supports model-specific batteries; BattleManager excludes water presentation from result pausing. The 13 files containing the 16 types were compared directly; other inventory files are unchanged.
- The older teammate document's **1/16 = 6.25%** applies only to its earlier endpoint `1e5fd09`.

CCR is not a total-code reuse percentage. Seven types are directly shared with 2D, and eight 2D execution components were added. This endpoint comparison is not evidence of a historical 2D-first conversion.

## 8. Verification and limits

| Version / stage | Actual recorded result |
|---|---|
| Integrated art commit `5bf140b` | Unity compilation passed; FinalFlowTests passed two complete menu/battle/result/retry/menu loops with virtual UI input and camera handoff checks |
| FinalCombatTests at integration | Two tests passed: real eight-shot damage/reload, battery bindings, Shift state, prediction visibility/recovery/unlock, water FFT readiness, island ray blocking and NPC movement |
| 2D regression during integration | Battle2DTests **5/5 passed**, including actual input, collisions, damage, both enemy types, results and three retries |
| Resource / scene inspection during integration | Missing scripts 0; one main Camera and AudioListener; menu/preparation Game View reviewed |
| Latest gameplay snapshot `6274d85` | Owner manually confirmed acceptance after the NPC-position/URP/configuration update; automated suites were **not rerun on that exact commit** |
| Standalone builds and hardware performance | Exact build paths, test mode and measured performance **not recorded here** |

Tests are available through **Window → General → Test Runner**, EditMode / PlayMode. Water tests need GPU rendering. Full-map long-duration AI navigation and target-hardware performance require manual assessment. Existing imported Terrain tree references and FBX LOD warnings were outside the final integration task. The ocean changes presentation; it does not provide real buoyancy or wave-driven ship physics.

Earlier teammate evidence is available on the documentation branch: [3D XML](https://github.com/spicyzhu58/GrandNavigation/blob/ab52e7b/Documentation/Evidence/final-acceptance.xml), [2D run](https://github.com/spicyzhu58/GrandNavigation/blob/ab52e7b/Documentation/Evidence/battle-2d-merge-tests.xml), [2D recheck](https://github.com/spicyzhu58/GrandNavigation/blob/ab52e7b/Documentation/Evidence/battle-2d-merge-recheck.xml). These are historical test outputs, not proof of automated testing of `6274d85`.

## 9. Attribution and teammate documentation

Retained from the contributor-authored README:

| Source | Credit / recorded relationship |
|---|---|
| [German Bismarck Battleship](https://sketchfab.com/3d-models/german-bismarck-battleship-96fbc0484c0d4f028f1745c6c1a8a242) | Donnacha Fallon / Lycan1795; contributor-reported battleship reference/adaptation source; CC Attribution reported, exact license version requires original-file confirmation |
| [Samidare destroyer](https://sketchfab.com/3d-models/samidare-destroyer-b37939147c854e61857f5b248f9efd29) | everlasting17th / everlastinggrey; contributor-reported destroyer basis. Project assets are named Gearing; the linked source is Samidare. Exact reused parts require contributor confirmation |
| [Pirate Kit](https://kenney.nl/assets/pirate-kit) | Kenney; CC0 reported in contributor documentation; environment reference, not a blanket license for separately imported Tropical assets |

Ye Kaixin's project-specific contribution includes turret pivots/muzzle bindings, scene layout, collision setup and water integration. Underlying third-party meshes, textures, terrain and shader code retain their separate provenance. The above source claims are preserved from teammate documentation, not independently reverified during this README edit.

See [teammate asset register](https://github.com/spicyzhu58/GrandNavigation/blob/ab52e7b/Documentation/AssetSources.md) and the committed [water source manifest](Assets/_Game/WaterIntegration/source-manifest.json). Water/FFT source licensing and complete redistribution permissions still require confirmation before distributing assets. Original local paths and hashes document provenance, not a license grant.

Supporting coursework drafts on the teammate branch: [Class Maps](https://github.com/spicyzhu58/GrandNavigation/blob/ab52e7b/Documentation/ClassMaps.md), [Change Map](https://github.com/spicyzhu58/GrandNavigation/blob/ab52e7b/Documentation/ChangeMap.md), [adaptation file list](https://github.com/spicyzhu58/GrandNavigation/blob/ab52e7b/Documentation/adaptation-files.tsv). These predate FinalIntegratedGame and must be reconciled with the final code/CCR before inclusion in the report. Kong Jincan's personal reflection draft is in the [contributor-updated README](https://github.com/spicyzhu58/GrandNavigation/blob/dd145d4/README.md#13-individual-reflections-and-ai-development-evidence); no new first-person reflection is written on another member's behalf here.

## 10. AI records and submission checklist

ChatGPT/Codex assisted design discussion, implementation, inspection, debugging and test execution. Human review included correcting aiming behaviour, integration references and test assumptions. Each student must archive **complete chronological prompts, responses, corrections and decisions**. Summaries, this README and selected examples are not substitutes for full records.

| Required evidence | Current location / action |
|---|---|
| KONG JINCAN complete AI log | **Pending:** export actual ChatGPT/Codex history and add exact repository path or accessible linked location |
| Ye Kaixin AI evidence | [Shared ChatGPT conversation supplied for the PPT](https://chatgpt.com/share/6af69795-9920-83ec-a96f-c75fed08cf6b); complete chronological archive coverage remains to be checked |
| Group report PDF | **Pending:** supply actual path in submission ZIP; include final Class Maps, Change Map, CCR, tests and source references |
| Individual reflections | Each member writes and verifies a named section in the PDF |
| Declaration of Original Work | **Pending:** signed group form in ZIP |
| Generative AI declarations | **Pending:** each member's supplied course form |
| Presentation | [Canva deck](https://www.canva.com/d/3QiZXnjsSg5P35N); approximately 15 minutes; both members explain their contributions. Reconcile the final CCR endpoint as noted above. |
| Standalone builds and screenshots | Record actual 2D/3D output paths, revisions, operator and test mode; do not invent build locations |
| Route B chronology exception | Disclose 3D-first history and record teacher response |

One member submits **`AP6412_Group04.zip`** through NTULearn by the course deadline. Include the runnable Unity project/source/essential assets, clear 2D/3D version locations, README and GitHub evidence, report PDF, full AI logs and required declarations. Include any cited evidence that otherwise lives only in ignored local `Artifacts`. Actual final ZIP location is not recorded here.
