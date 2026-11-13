# Asset sources — Group 4

### Current asset source register

On 13 November 2026, yekaixin113 (G2607926G, Group 4) confirmed that the following three works were used as references and bases for modifications during production of the existing art. This source relationship is contributor-confirmed; the local import scripts record collection/package paths and do not independently establish a file-level match to the linked downloads. The references below are retained alongside that import history.

| Source asset / original page | Author | Listed license | Use in current project / modifications | Evidence |
| --- | --- | --- | --- | --- |
| [German Bismarck Battleship](https://sketchfab.com/3d-models/german-bismarck-battleship-96fbc0484c0d4f028f1745c6c1a8a242) | Donnacha Fallon (Lycan1795) | CC Attribution (CC BY); exact version pending original license file | Battleship reference and adaptation basis; project ship processing includes turret separation, pivots, muzzle binding and material adaptation by yekaixin113 | Contributor-confirmed source relationship; original-file correspondence not independently verified |
| [Samidare destroyer](https://sketchfab.com/3d-models/samidare-destroyer-b37939147c854e61857f5b248f9efd29) | everlasting17th (everlastinggrey) | CC Attribution (CC BY); exact version pending original license file | Destroyer reference and adaptation basis, confirmed by yekaixin113; project assets use the name Gearing, while this source depicts Samidare | Contributor-confirmed source relationship; exact reused parts not itemized |
| [Pirate Kit](https://kenney.nl/assets/pirate-kit) | Kenney | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | Environment reference and adaptation basis; project-specific layout and collision configuration authored by yekaixin113 | Contributor-confirmed source relationship; imported Tropical Environment package is separately recorded below |

Credit: battleship source by Donnacha Fallon; destroyer source by everlasting17th; environment source by Kenney. Art adaptation and original project-specific turret binding, scene layout, collision configuration and water integration by yekaixin113 (G2607926G). AI assistance is disclosed separately. Underlying third-party meshes, textures, heightmaps and shader code retain their source attribution.

The two Sketchfab pages were listed as CC Attribution in the web search; their exact license versions remain pending. Kenney's official page states CC0. These references do not resolve the separate water/FFT source licenses or automatically cover all files from the imported collections.

## Local import provenance

| Asset | Verified local provenance | Attribution status | Project adaptation (yekaixin113 for turret binding, layout, collision and water integration) |
| --- | --- | --- | --- |
| Bismarck and Gearing | Tools/NavalArt/prepare_assets.py reads the supplied B301 364艘战舰3D模型_已解压 collection | Contributor-reported art sources listed above; collection-to-source mapping remains unverified | Mesh conversion, turret/barrel separation, pivot and muzzle setup, material adaptation |
| Tropical environment | Tools/NavalArt/extract_environment.py extracts Tropical Enviroment Pack.unitypackage | Contributor-reported art sources listed above; collection-to-source mapping remains unverified | Scene layout, terrain-derived meshes, placement, collision simplification, LOD integration |
| Water shader / SSR / shallow water | WaterIntegration/source-manifest.json records local Stylized Water source files and hashes | Upstream author and license unknown | API adaptation and integration |
| FFT compute | WaterIntegration notes identify local Water Asset/FFTOcean source | Upstream author and license unknown | Scheduling, scene integration and effects |


Remaining records: exact CC BY license versions and source-to-file correspondence; water shader, SSR, shallow-water and FFT upstream authors/licenses. Contributor confirmation is recorded as such and does not replace original download/license evidence.
