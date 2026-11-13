# Version changes and CCR

## Exact snapshots

- Pre-adaptation 3D: `b3c5b41280373e7d17e48f680f9daf84f6379cb1`.
- Final independent 2D: `cac76c431e12decbdcdab842bc138dcec7ebc141`.
- Earlier integrated art snapshot: `1e5fd09783eeee48eed29ecaa0e5fc91755fc4a6`.
- Final accepted gameplay: `6274d8549e8a8b4e80ced9831b9aa0dbc0b7025d` (`final-submission`).

Actual chronology is 3D then 2D adaptation. The final 2D tree already contains earlier 3D code. The final snapshot comparison therefore measures integration changes to a repository containing both implementations, not a clean-room 2D-to-3D rewrite.

## Historical 3D to final 2D adaptation

| Status | Area | Reason |
| --- | --- | --- |
| Added | Eight Battle2D runtime components | 2D identity, movement, input, weapon, projectile, AI, camera and lifecycle |
| Added | Battle2D editor builders, profiles, prefab, sprite, scene and tests | Saved playable 2D configuration and validation |
| Modified | EditorBuildSettings.asset | Include Battle2D for loading/retry |
| Unchanged | Existing 16 core types | Keep the established 3D baseline intact |
| Deleted | None | Additive adaptation |

`cac76c4` adds the quit button after initial implementation `1e4d21a`. The historical fixed-core CCR remains 0/16 = 0%. Full tracked-file list: [adaptation-files.tsv](adaptation-files.tsv).

## Final 2D snapshot to earlier art snapshot (historical)

| Status | Area | Reason |
| --- | --- | --- |
| Modified core | Weapon | Iterate all configured turrets and display actual salvo capacity rather than three turrets / six shells |
| Modified presentation | CombatHUD, CombatEffects | Battery-aware feedback, visible prediction overlay and water effects |
| Added | NavalArt assets/tools and WaterIntegration assets/runtime/tools/tests | Ship and turret binding, environment geometry and water rendering |
| Added/modified scene configuration | Art scenes, prefabs, build and quality settings | Connect imported assets and scene-specific pipelines |
| Unchanged | Battle2D implementation and scene | Preserve final independent 2D gameplay |
| Unchanged core | Other 15 fixed inventory types | Keep movement, damage, AI decisions and result rules stable |

Full tracked-file statuses, including meta files and imported assets: [earlier-art-files.tsv](earlier-art-files.tsv). This list is generated with rename detection disabled so additions/deletions remain explicit. New third-party code and assets are integration inputs, not original team-authored classes.

## CCR scope and reproducibility

CCR = changed or deleted baseline types / fixed baseline type count. Newly added adapters, enums, UI, editor scripts, tests and imported water types are outside this historical inventory. A change anywhere in a type's source file is conservatively flagged for inspection; the final changed inventory file contains Weapon alone.

Inventory (16): IDamageable, HealthSystem, MovementSettings, ShipMovementMath, Ship, PlayerShip, EnemyShip, Destroyer, Battleship, Weapon, Projectile, CombatBallistics, GunneryMath, EnemyAIProfile, EnemyAILogic, BattleManager.

Earlier art endpoint `1e5fd09`: **Weapon modified; 15 unchanged; none deleted; CCR = 1/16 = 6.25%.** This is a scoped core-change metric, not code reuse percentage or proof that every game rule is identical. Eight new 2D adapters and seven directly shared core types must be reported alongside it.

```sh
git diff --no-renames --name-status b3c5b41 cac76c4
git diff --no-renames --name-status cac76c4 1e5fd09
git diff cac76c4 1e5fd09 -- Assets/_Game/ShipMovement/Runtime/Weapon.cs
git diff cac76c4 1e5fd09 -- Assets/_Game/Battle2D Assets/_Game/Scenes/Battle2D.unity
```

The last command returns no changes. See [ClassMaps.md](ClassMaps.md) for the principal relationships and architecture boundaries.
## Final accepted release: cac76c4 to 6274d85

**CCR = 2/16 = 12.5%**: Weapon and BattleManager changed; the other 14 inventory types are unchanged. BattleManager now exempts water presentation from component disabling at results. New adapters, tools, shaders, scene configuration, HUD and PredictionRegion remain outside the fixed inventory. Historical 0% and 6.25% use earlier endpoints and are not the final-release value.

| Status | Area | Purpose |
| --- | --- | --- |
| Modified core | Weapon; BattleManager | Configured battery capacity; keep water presentation enabled during result handling |
| Modified presentation / flow | CombatEffects, CombatHUD, PredictionRegion, GameFlowController | Water effects, configurable prediction overlay, preserve water during menu flow |
| Added | FinalIntegration builder, routes, reset prefabs, tests; FinalIntegratedGame scene | Assemble prior art and gameplay into the final menu-to-battle experience |
| Modified configuration | Build scenes, rendering/project settings and scene positions | Final entry and accepted scene configuration |
| Unchanged | Battle2D implementation and scene | Retain final playable 2D version |

[Complete final endpoint file statuses](final-snapshot-files.tsv), generated with `git diff --no-renames --name-status cac76c4 6274d85`. This is a source-snapshot comparison, not an assertion that all changes were made after 2D in chronological order. Final integration notes are in `Assets/_Game/FinalIntegration/README.md`.
