# FreeNavalBattle: environment and water integration

Open `Assets/_Game/Scenes/FreeNavalBattle.unity` and enter Play. Existing driving, aiming, locking and firing controls are retained. Unity remains 6000.3.27f1.

## Environment pass

The three island meshes derive from the supplied Tropical Environment demo Terrain heightmap and retain the existing battle map footprint. Steep faces now use the supplied rock texture. Terrain-grounded palm clusters contain 195 trees; 72 inland/coastal rock placements use Rock4/Rock6/Rock9 from the same package, with three mesh LODs and distant culling. Decorative rock meshes do not add gameplay colliders; the island's 24 convex sectors remain the navigation and shell-blocking geometry.

Water uses a scene-specific FreeBattleSea material. Reflection, refraction distortion, caustics and highlights were reduced for the sunny battlefield. The FFT island attenuation bounds were updated from the finished visible island bounds. Original source materials and the earlier water test scene retain their own settings. Physical buoyancy and wave-surface projectile collision remain out of scope; driving and ballistics still use the existing mean sea plane.

## Yellow prediction indicator

The existing prediction ellipse was drawn near the fixed sea plane and could disappear behind the displaced/transparent water. CombatHUD now draws a dashed yellow ellipse in screen space at the fire-control predicted intercept, after the world render. This is a tactical overlay: it requires a valid prediction and a visible locked target; it hides on occlusion, unlock, target loss or invalid range. The original world-space PredictionRegion component is disabled in FreeNavalBattle to avoid duplicate indicators. The green target frame and cyan actual-barrel impact markers remain unchanged.

## Build and verification

`FreeBattleFinish.Build` is an editor-only, one-time finishing script. It saves the previous scene to ignored `Artifacts/FreeBattle/before-finish.unity` and refuses to repeat a completed pass. It verifies serialized movement, weapon and health settings before/after environment changes.

`FreeBattleAcceptanceTests` exercises prediction visibility/cover/unlock and a 45-second AI/GPU soak. `FreeBattleTerrainTests` checks convex colliders, enemy movement and reset routes and captures the finished scene. Combat, occlusion and water suites provide regression coverage. Reports/screenshots are in ignored `Artifacts/FreeBattle`.

The render timing report measures explicit batch-mode Camera.Render CPU submission at 1280x720, not standalone game FPS or GPU frame time. Final hardware performance requires a player build on the intended target hardware. Island collision is intentionally simplified; further route tuning may be needed for larger ship counts or changed gameplay rules.

Final validation: 22/22 PlayMode tests passed (Artifacts/FreeBattle/final-acceptance.xml), including a 45-second AI/GPU run. The scene pipeline override now occurs only in Play mode, preventing editor scene saves from changing the global QualitySettings pipeline. Use Play mode to review full FFT/SSR effects.
