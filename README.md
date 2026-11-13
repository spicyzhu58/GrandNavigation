# GrandNavigation — AP6412 Route B

Single-player naval combat in Unity, with playable 2D and 3D versions. Destroy all designated enemy ships to win; losing the player ship results in defeat. If both conditions occur in the same frame, defeat takes priority.

**Final versions; manual acceptance passed, confirmed by the team on 13 November 2026.** Live GitHub references match the local project: final 2D is `cac76c431e12decbdcdab842bc138dcec7ebc141` on `feature/battle-2d`; the final integrated project is `1e5fd09783eeee48eed29ecaa0e5fc91755fc4a6` on `level-art`. Final snapshot tags are published; submission documents remain to be completed.

## 1. Team and repository

- Repository: [spicyzhu58/GrandNavigation](https://github.com/spicyzhu58/GrandNavigation)
- Course: AP6412 — Foundation for Technology & Coding
- Route: B — 2D and 3D versions of the same game
- Group number: **4**

| Member | Main responsibilities | Contribution evidence |
| --- | --- | --- |
| **KONG JINCAN — G2601963A** (`spicyzhu58`) | Gameplay requirements, movement, combat, enemy AI, game flow and 2D adaptation | `649d532`, `8c30506`, `7e7c1cd`, `9c26a67`, `1e4d21a` |
| **YE KAIXIN — G2607926G** (`yekaixin113`) | Ship and turret integration, environment, water, combat feedback, integration and delivery verification | `38de1de`, `88f02ed`, `1e5fd09` |

Both students must submit their own complete AI logs, individual reflection, and presentation evidence. The assignments of work below distinguish gameplay/system design from technical art, water and visual integration; commits are supporting evidence, not proof of authorship of every imported asset.

## 2. Requirements and dependencies

- Unity Editor **6000.3.27f1**, as recorded in `ProjectSettings/ProjectVersion.txt`.
- C#; Universal Render Pipeline **17.3.0**.
- Input System **1.20.1**, AI Navigation **2.0.16**, Cinemachine **3.1.7**, Unity Test Framework **1.6.0**.
- The complete package list is in [Packages/manifest.json](Packages/manifest.json); preserve `Packages/packages-lock.json` for package resolution.
- Install the Unity build support module for the intended target. Windows standalone is the proposed delivery target; minimum hardware and final build compatibility have not been established.
- Water integration uses GPU compute and rendering features. Validate it on the actual demonstration computer.

Unity restores packages when opening the project; initial import may require network access. Normal play uses the included assets. External source paths mentioned in asset-processing notes describe the original import workflow and are not player installation steps.

## 3. Installation and Editor launch

1. Obtain the complete project ZIP, or clone the repository and check out the documented version. For this local documentation snapshot, use `1e5fd09`; this matches the latest remote integrated project revision.
2. In Unity Hub, select **Add project from disk** and choose the directory containing `Assets`, `Packages` and `ProjectSettings`.
3. Open it with Unity 6000.3.27f1 and allow package restoration and asset import to finish.
4. Open one of the scenes below and press Play. Resolve any missing-package or missing-asset errors before demonstrating.

| Version / purpose | Scene | Start and result flow |
| --- | --- | --- |
| Playable 2D | `Assets/_Game/Scenes/Battle2D.unity` | Start Game → combat → Victory / Defeat → Retry → Start Game |
| Verified 3D game-flow baseline | `Assets/_Game/Scenes/IntegratedGame.unity` | Main Menu → Preparation → GO → combat → result → Retry / Main Menu |
| Ship / environment art pass | `Assets/_Game/Scenes/TropicalNavalBattle.unity` | Separate art-integration scene; see NavalArt notes |
| Water integration | `Assets/_Game/Scenes/WaterIntegrationTest.unity` | Separate water test scene; see WaterIntegration notes |
| Additional art scene | `Assets/_Game/Scenes/FreeNavalBattle.unity` | Final local art/water battlefield; see FreeBattle.md |

**Requested final 3D demonstration entry: `Final-Integration`.** Repository inspection on 13 November 2026 did **not** find a scene named `Final-Integration.unity` in `level-art` or the `3d-final` snapshot scene directory. The verified saved scenes are `Assets/_Game/Scenes/IntegratedGame.unity` (complete menu-to-combat flow) and `Assets/_Game/Scenes/FreeNavalBattle.unity` (latest art/water battlefield). `Final-Integration` must be committed/exported and its exact scene path recorded before it can be claimed as the runnable final entry. Until then use `IntegratedGame.unity` as the verified flow-demonstration scene and show `FreeNavalBattle.unity` separately for art/water; **do not silently describe these as one integrated scene**.

## 4. Controls

| Action | 2D | 3D combat controls |
| --- | --- | --- |
| Adjust and maintain target speed | W / S | W / S |
| Steer | A / D | A / D |
| Aim | Mouse world position | Mouse aim / orbit; hold Shift for aim view |
| Fire salvo | Left mouse button | Left mouse button |
| Lock target | Right-click an enemy | Right-click the enemy under the screen-centre pointer |
| Clear lock | Esc | Esc |
| Zoom | Mouse wheel | Mouse wheel |
| Reset observation camera | — | R |

Locking provides targeting feedback and does not guarantee a hit. Reload, collision, dispersion and target motion affect firing. Combat input in the 3D menu-flow scene becomes available after GO and the camera transition. Scene-specific debug controls are documented in the module READMEs.

## 5. Build and standalone launch

1. Open **File → Build Profiles** and select the intended desktop target with its build support installed.
2. Prepare separate scene lists for each deliverable. Use `Battle2D.unity` as the first enabled scene for a 2D build, or the confirmed final 3D entry scene for a 3D build. Include scenes required by that version's reload / menu routing.
3. Build each version into a separate output directory. Keep the generated executable, data directory, Unity libraries and other generated files together.
4. Launch the executable and verify start, movement, firing, victory, defeat and retry outside the Editor.

Manual acceptance was reported by the team on **13 November 2026**. Recorded source snapshots: `2d-final` (`cac76c4`) and `3d-final` (`1e5fd09`). **Acceptance environment (Unity Editor vs standalone executable), operator, build output paths and whether the tested project exactly matched each tag are not independently recorded**; fill in the evidence record below before submission. No final standalone binary has been verified in the repository. No project executable was found in the inspected workspace. Archived GPU tests identify NVIDIA GeForce RTX 4070 Laptop GPU; this does not identify the later manual acceptance environment. The historical 2D README reports no standalone validation at the time it was written; any later packaging or run must be recorded separately.

## 6. Shared rules, architecture and differences

The 3D ship model uses `Ship` implementing `IDamageable`, with `PlayerShip` and `EnemyShip` derived from it; `Battleship` and `Destroyer` derive from `EnemyShip`. Movement, health and weapons have separate responsibilities. Enemy behaviour uses a finite state machine.

The 2D implementation uses `ShipActor2D` and eight execution components for 2D movement, input, weapons, projectiles, AI, camera and battle management. It directly shares seven core types: `IDamageable`, `HealthSystem`, `MovementSettings`, `ShipMovementMath`, `GunneryMath`, `EnemyAIProfile` and `EnemyAILogic`. It also shares state enums. `HealthSystem` remains a Unity component.

| System | 3D | 2D |
| --- | --- | --- |
| Movement / collision | XZ plane, Rigidbody, 3D physics | XY plane, Rigidbody2D, Physics2D |
| Camera | Perspective observation / aim view | Orthographic follow camera |
| Projectile | Gravity ballistics and physical turret limits | Straight-line projectile and simplified salvo |
| AI | Full 3D FSM / navigation | Shared decision rules with simpler local avoidance |
| Result | Enemy elimination / player death; defeat priority | Same result rules, separate battle manager implementation |

The two versions do not share the full ship inheritance hierarchy or the same battle-manager implementation. They retain common objectives and selected rules, with explicit differences in ballistic and navigation behaviour. See [2D architecture, change map and CCR](Assets/_Game/Battle2D/README.md). Final [2D and 3D class maps](Documentation/ClassMaps.md), [change map and CCR](Documentation/ChangeMap.md), [historical adaptation file list](Documentation/adaptation-files.tsv), and [final snapshot file list](Documentation/final-snapshot-files.tsv) are available.

## 7. Development chronology and CCR

The actual development order was **3D → 2D adaptation**. Route B asks for a frozen 2D baseline followed by 3D development. This discrepancy must be disclosed; teacher acceptance has not been documented here. Any later release tags identify snapshots and must not imply a different historical order.

The historical 2D adaptation defines CCR as modified or deleted baseline core types divided by a fixed set of 16 baseline core types, excluding newly added adapters. It reports **0 / 16 = 0%** for the 3D baseline `b3c5b41` to the 2D adaptation. Seven core types were directly shared and eight 2D execution components were added. This is neither a 100% reuse claim nor the final 2D-to-3D CCR required by the assignment.

The final report must state the chosen comparison commits, type list, changes and calculation. Final snapshot CCR, using the same fixed 16-type inventory but **different comparison endpoints and purpose**: **1/16 = 6.25%**, comparing `cac76c4` to `1e5fd09`. Only Weapon changes within that inventory: it removes the three-turret cap and replaces the fixed six-shell display with actual battery capacity. The other 15 inventory types are unchanged. This endpoint comparison does not establish chronological 2D-first development or measure all 2D adapter changes. Reproduce with `git diff cac76c4 1e5fd09 -- Assets/_Game/ShipMovement/Runtime Assets/_Game/EnemyAI/Runtime Assets/_Game/BattleResult/Runtime`; exclude presentation types outside the fixed inventory.

## 8. Version and GitHub evidence

| Evidence | Location / status |
| --- | --- |
| 3D flow baseline | [`b3c5b41`](https://github.com/spicyzhu58/GrandNavigation/commit/b3c5b41), branch `feature/seamless-gameflow` |
| Initial playable 2D adaptation | [`1e4d21a`](https://github.com/spicyzhu58/GrandNavigation/commit/1e4d21a), branch `feature/battle-2d` |
| 2D integration into local art branch | [`1e5fd09`](https://github.com/spicyzhu58/GrandNavigation/commit/1e5fd09), branch `level-art` |
| Historical adaptation comparison | [3D baseline → initial 2D implementation](https://github.com/spicyzhu58/GrandNavigation/compare/b3c5b41...1e4d21a) — development comparison, not the final Route B comparison |
| Merge history | [level-art history](https://github.com/spicyzhu58/GrandNavigation/commits/level-art); integration merge `1e5fd09` |
| spicyzhu58 contribution branch | [feature/battle-2d](https://github.com/spicyzhu58/GrandNavigation/tree/feature/battle-2d) — contribution branch identified in project history |
| yekaixin113 contribution branch | [level-art](https://github.com/spicyzhu58/GrandNavigation/tree/level-art) — contribution branch identified in project history |
| Final 2D snapshot tag | [2d-final](https://github.com/spicyzhu58/GrandNavigation/tree/2d-final) → `cac76c4` |
| Final 3D release tag | [3d-final](https://github.com/spicyzhu58/GrandNavigation/tree/3d-final) → `1e5fd09` |
| Final release comparison | [Final snapshot comparison](https://github.com/spicyzhu58/GrandNavigation/compare/cac76c431e12decbdcdab842bc138dcec7ebc141..1e5fd09783eeee48eed29ecaa0e5fc91755fc4a6) (two-dot endpoint comparison) |

Annotated tags `2d-final` and `3d-final` were created and published on 13 November 2026 at the exact commits above. They identify final code snapshots and do not backdate development. The requested 2D-first chronology is not implied by these names. The historical `3d-pre-2d-adaptation` tag and a `feature/final-integration` branch were absent from the remote inventory. These tags point to existing game commits, so newly prepared README and Documentation files are not included in those tagged trees; include them alongside the snapshots in the submission package.

## 9. Verification and known limitations

Historical results recorded in the 2D README on 12 November 2026: compilation with zero errors; five Battle2D tests passed; one three-cycle Seamless test passed; one EnemyAI PlayMode test passed; 19/19 Gameplay EditMode tests passed. These are recorded historical results, not tests rerun for this README or proof of final art-integration stability.

To run available automated checks, open **Window → General → Test Runner**, select EditMode / PlayMode and run the relevant suites. Water rendering tests require a graphics device; do not use `-nographics`. Export test reports with the tested commit and environment.

For each final standalone version, record a manual check of start, driving, firing, real damage, enemy behaviour, victory, defeat and repeated retry. Check that partial enemy elimination does not end the battle, reloading prevents repeated salvos, and simultaneous player / final-enemy death resolves consistently. Manual acceptance: **passed**, as confirmed by the team on 13 November 2026. Archived evidence: [3D acceptance, 22 passed / 0 failed](Documentation/Evidence/final-acceptance.xml); [2D merge run, 4 passed / 1 failed](Documentation/Evidence/battle-2d-merge-tests.xml); [failed test passing on rerun](Documentation/Evidence/battle-2d-merge-recheck.xml). The rerun covers BothEnemyTypesMoveAndFireRealProjectiles and is not a complete suite rerun. Existing engine captures: [3D play preview](Documentation/Evidence/play-preview.png), [terrain](Documentation/Evidence/terrain.png). These files were copied from local Artifacts for submission; they were not newly generated during this review. Detailed manual operator/date/build records remain to be supplied.

Known limits include simplified 2D ballistics and avoidance, separate 2D ship and battle-manager implementations, and unverified final standalone performance. Water visuals do not provide full wave-driven ship physics. Manual acceptance has been confirmed; measured standalone performance specifications remain undocumented.

## 10. Assets and module documentation

See [current asset sources and import provenance](Documentation/AssetSources.md). The three art-source relationships below were confirmed by yekaixin113.

- [2D gameplay and adaptation](Assets/_Game/Battle2D/README.md)
- [3D menu-to-combat flow](Assets/_Game/Seamless/README.md)
- [Combat controls and implementation](Assets/_Game/PlayerCombat/README.md)
- [Ship / environment asset integration](Assets/_Game/NavalArt/README.md)
- [Water sources, adaptations and limits](Assets/_Game/WaterIntegration/README.md)

The [water source manifest](Assets/_Game/WaterIntegration/source-manifest.json) records original local source paths and SHA-256 hashes. The [final battlefield notes](Assets/_Game/WaterIntegration/FreeBattle.md) document the finished scene. Third-party ship, environment, shader and texture assets are credited through module notes and source manifests where available. Final original-source URLs, authors, licenses and redistribution permissions: **TODO: complete an asset attribution list before packaging**. Team contributions describe integration and adaptation; third-party source assets are not claimed as original team work.

### Original implementation by yekaixin113

As confirmed by the contributor on 13 November 2026, yekaixin113 authored the project-specific turret binding, scene layout, collision configuration and water integration. Credit includes turret pivots and muzzle binding, placement and arrangement of scene objects, collision setup, and connecting water rendering/effects to gameplay. AI assistance is disclosed in the project AI records. This authorship statement covers the implementation and arrangement; underlying imported meshes, textures, terrain source data and shader code retain their separate provenance.

### Current asset source register

On 13 November 2026, yekaixin113 (G2607926G, Group 4) confirmed that the following three works were used as references and bases for modifications during production of the existing art. This source relationship is contributor-confirmed; the local import scripts record collection/package paths and do not independently establish a file-level match to the linked downloads. The references below are retained alongside that import history.

| Source asset / original page | Author | Listed license | Use in current project / modifications | Evidence |
| --- | --- | --- | --- | --- |
| [German Bismarck Battleship](https://sketchfab.com/3d-models/german-bismarck-battleship-96fbc0484c0d4f028f1745c6c1a8a242) | Donnacha Fallon (Lycan1795) | CC Attribution (CC BY); exact version pending original license file | Battleship reference and adaptation basis; project ship processing includes turret separation, pivots, muzzle binding and material adaptation by yekaixin113 | Contributor-confirmed source relationship; original-file correspondence not independently verified |
| [Samidare destroyer](https://sketchfab.com/3d-models/samidare-destroyer-b37939147c854e61857f5b248f9efd29) | everlasting17th (everlastinggrey) | CC Attribution (CC BY); exact version pending original license file | Destroyer reference and adaptation basis, confirmed by yekaixin113; project assets use the name Gearing, while this source depicts Samidare | Contributor-confirmed source relationship; exact reused parts not itemized |
| [Pirate Kit](https://kenney.nl/assets/pirate-kit) | Kenney | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | Environment reference and adaptation basis; project-specific layout and collision configuration authored by yekaixin113 | Contributor-confirmed source relationship; imported Tropical Environment package is separately recorded below |

Credit: battleship source by Donnacha Fallon; destroyer source by everlasting17th; environment source by Kenney. Art adaptation and original project-specific turret binding, scene layout, collision configuration and water integration by yekaixin113 (G2607926G). AI assistance is disclosed separately. Underlying third-party meshes, textures, heightmaps and shader code retain their source attribution.

The two Sketchfab pages were listed as CC Attribution in the web search; their exact license versions remain pending. Kenney's official page states CC0. These references do not resolve the separate water/FFT source licenses or automatically cover all files from the imported collections.

## 11. AI records and submission materials

ChatGPT assisted requirements and design discussion; Codex assisted implementation, inspection, debugging and tests. Each member must provide chronological prompts, replies and corrections, with links to work influenced by those conversations. Module summaries do not replace complete AI logs.

| Required material | Delivered location |
| --- | --- |
| KONG JINCAN (`spicyzhu58`) complete chronological AI prompts and responses | **NOT YET ARCHIVED/VERIFIED — export full ChatGPT/Codex history and link exact repository path** |
| YE KAIXIN (`yekaixin113`) complete chronological AI prompts and responses | **NOT YET ARCHIVED/VERIFIED — contributor must supply complete logs and link exact repository path** |
| Group PDF report, both class maps, change map and CCR | **TODO: ZIP path / URL** |
| Individual reflection sections | KONG JINCAN draft below; YE KAIXIN reflection to be authored by YE KAIXIN in the group PDF |
| Group Declaration of Original Work | **TODO: ZIP path** |
| Each member's Declaration on Use of Generative AI Assistance | **TODO: ZIP paths** |
| 2D / 3D runnable project snapshots and builds | **TODO: ZIP paths and exact revisions** |
| Presentation / demonstration evidence | **TODO: ZIP path / URL** |

Submit one `AP6412_Group04.zip` for Group 4. Include the complete required project files, necessary assets and evidence; identify both playable version locations. Generated `Library`, `Temp` and local cache directories are not source deliverables. Some development evidence is under Git-ignored `Artifacts`; copy any cited evidence into the actual submission so reviewers can access it.


## 12. Final acceptance record (required for submission)

The final code labels identify **source snapshots**, not a verified Windows executable. The team's 13 November 2026 manual-pass statement and archived automated test reports must not be conflated with an independently validated standalone build.

| Item | 2D | 3D |
| --- | --- | --- |
| Final tag | [`2d-final`](https://github.com/spicyzhu58/GrandNavigation/tree/2d-final) | [`3d-final`](https://github.com/spicyzhu58/GrandNavigation/tree/3d-final) |
| Commit | `cac76c431e12decbdcdab842bc138dcec7ebc141` | `1e5fd09783eeee48eed29ecaa0e5fc91755fc4a6` |
| Verified existing scene | `Assets/_Game/Scenes/Battle2D.unity` | `Assets/_Game/Scenes/IntegratedGame.unity` (flow); `Assets/_Game/Scenes/FreeNavalBattle.unity` (art/water) |
| Requested final demonstration label | — | `Final-Integration` — **requested but not found as a saved scene; requires integration evidence** |
| Reported manual acceptance date | 2026-11-13 (team statement) | 2026-11-13 (team statement) |
| Actual test mode | **UNRECORDED: Editor / Standalone** | **UNRECORDED: Editor / Standalone** |
| Tested revision and test operator | **UNRECORDED** | **UNRECORDED** |
| Final package / standalone location | **UNRECORDED** | **UNRECORDED** |
| Archived automated evidence | [2D merge and rerun reports](Documentation/Evidence/battle-2d-merge-tests.xml) | [22/22 final PlayMode tests](Documentation/Evidence/final-acceptance.xml) |
| Existing screenshots | Add a 2D gameplay capture | [Play preview](Documentation/Evidence/play-preview.png), [terrain](Documentation/Evidence/terrain.png) |

**Suggested acceptance procedure** (not a claim of completed testing): open the identified tag with Unity 6000.3.27f1, record the actual scene and Editor/Standalone mode, run start/movement/aim/lock/fire/AI/victory/defeat/retry cases, capture screenshots and a Unity Test Runner result, and note any failures and rerun results. For a standalone build, record its actual location and include executable and companion files in the submission ZIP. Do not put a hypothetical build path in the report.

## 13. Individual reflections and AI-development evidence

### KONG JINCAN — G2601963A — gameplay and systems design

This was my first opportunity to take responsibility for the game-design workflow across a complete playable prototype. We chose a naval setting because my teammate, a technical artist, wanted to explore ocean rendering, lighting and visual effects. I adapted the class tank-battle concept into warship combat, taking inspiration from *World of Warships* to provide more meaningful steering, target selection and salvo decisions without reproducing its full complexity.

I specified the control scheme, ship inertia and movement, fire-control rules, enemy behaviour, battle flow, and gameplay acceptance conditions. The major challenge was aiming. Automatic lead initially made locked targets too easy to hit; I therefore revised the design toward an approximate predicted-hit region, repeated prediction updates, projectile dispersion, target-occlusion handling and a constrained firing arc. Some ideas, including trial-fire as a separate mechanic, were removed to keep the implementation lightweight. The Bismarck battery and salvo count also had to be aligned with the configured turrets instead of hard-coded UI assumptions.

The most useful lesson from AP6412 was to give AI **one well-scoped module or change at a time**, with explicit expected behaviour, boundary conditions and test criteria. AI assisted my C# implementation, code inspection, test planning and edge-case review, but it did not make the final design decisions. I reviewed results through gameplay tests and requested revisions when a technically functioning mechanic weakened player agency or integration stability. Object-oriented separation of ship, movement, weapon, health, AI and presentation responsibilities made these changes easier to reason about, although our 2D version still needed dedicated engine-specific adapters. The actual history went from 3D to an additive 2D adaptation, which must be disclosed rather than reconstructed as 2D-first.

Selected **conversation topics for indexing only** (not full transcripts): ship movement specification and speed changes; fire-control V0.1 and subsequent lead/dispersion updates; target locking and occlusion; AI finite-state behaviour; 2D adaptation with shared health/math/AI helpers; final test and CCR review. Provide complete exported prompt-and-response logs from the original tools, with chronology, links and affected commits. Conversation summaries and this reflection **are not substitutes** for those logs.

### YE KAIXIN — G2607926G — technical art and integration

Contribution summary: ship/turret model adaptation and binding, art scene assembly, collision settings, water rendering and effects integration, and final visual delivery and testing support, as documented in [AssetSources.md](Documentation/AssetSources.md) and the art/water module notes. **Individual first-person learning/reflection and full AI conversation evidence must be supplied and verified by YE KAIXIN; they are not fabricated here.**

## 14. Submission index and completion gate

- **Final code:** `2d-final` and `3d-final`. The most recent working documentation is on [`level-art`](https://github.com/spicyzhu58/GrandNavigation/tree/level-art) and may not exist inside the frozen tag trees.
- **Final class maps:** [Documentation/ClassMaps.md](Documentation/ClassMaps.md) (both 2D and 3D, core vs engine/presentation responsibilities).
- **Change evidence:** [Documentation/ChangeMap.md](Documentation/ChangeMap.md), [historical file statuses](Documentation/adaptation-files.tsv), [final snapshot file statuses](Documentation/final-snapshot-files.tsv).
- **CCR scope A (historical 3D-to-2D adaptation):** `b3c5b41 → cac76c4`, changed/deleted original 16 core types = **0/16 (0%)**; new 2D adapters are excluded.
- **CCR scope B (final tagged endpoint comparison):** `cac76c4 → 1e5fd09`, changed/deleted fixed inventory types = **1/16 (6.25%)**; `Weapon` changed, 15 unchanged. This is not the same event or denominator scope of all changed files.
- **Validation:** [final acceptance XML](Documentation/Evidence/final-acceptance.xml), [2D merge run](Documentation/Evidence/battle-2d-merge-tests.xml), [rerun](Documentation/Evidence/battle-2d-merge-recheck.xml), [play preview](Documentation/Evidence/play-preview.png), [terrain screenshot](Documentation/Evidence/terrain.png).
- **Attribution:** [Documentation/AssetSources.md](Documentation/AssetSources.md), [WaterIntegration source manifest](Assets/_Game/WaterIntegration/source-manifest.json).
- **Required before ZIP submission:** commit or precisely identify `Final-Integration`; enter the true final ZIP/build output path, actual tested version, test date, Editor vs standalone mode and operator; add missing gameplay screenshots; complete third-party licensing checks; archive **both students' full chronological AI logs**; include group PDF with each student's personal reflection and signed declarations.

The proposed submission name is **`AP6412_Group04.zip`**. Its **actual file location remains unverified**. This README does not claim an executable or complete AI transcripts exist when the repository does not establish them.
