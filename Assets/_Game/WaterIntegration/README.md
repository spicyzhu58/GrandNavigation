# 水体集成测试场景

打开 `Assets/_Game/Scenes/WaterIntegrationTest.unity`，进入 Play。控制方式沿用 TropicalNavalBattle。原场景保留。

## 来源和结构

- 外观：`E:\Unity\Stylized Water\Assets\Shader` 的 Water.shader、Water_Sea_Mat、4 张依赖贴图、SSR，以及 ShallowWater 局部交互求解器。
- FFT：`E:\Unity\Water Asset\Assets\FFTOcean\FFTOcean.compute`，由 NavalFFT 调度；不使用来源项目的另一套海面材质。
- 来源文件 SHA-256 和原始路径记录在 source-manifest.json；外部两个项目未修改。
- 保持 Unity 6000.3.27f1、URP 17.3.0。为较新来源工程的 API 做了适配，无包升级。
- 场景专用 WaterIntegration_RP / Renderer 开启深度、不透明颜色纹理、SSR 和浅水 RenderGraph Pass。WaterSceneContext 进入场景时切换，退出时恢复先前管线。

## 已接入的表现

水色、深浅过渡、岸边泡沫、折射、焦散、反射沿用来源海洋材质。Forward 和 SSR 共用同一个顶点位移函数；FFT 替代来源的 Gerstner 波形，浅水交互高度/法线叠加在其上。关闭 NavalFFT 的 Use FFT 可对比来源 Gerstner 外观。

海面网格跟随相机，波形与噪声使用世界坐标；岛屿周围和远处减弱 FFT 位移。局部交互范围为相机附近 384 × 384 米、512 × 512 分辨率，窗口移动时滚动复用历史纹理。船体使用不参与碰撞、不可见的简化轮廓产生扰动；重生的舰船自动重新接入。炮弹落水驱动来源的 AddImpulse，并附加扩散环，域外仍有可见落水表现。

## 不改变的游戏逻辑与当前边界

- 航速、加减速、转向、刚体约束、血量、弹速、散布、伤害、射程、装填及炮塔参数均沿用原场景；构建时逐船比对。
- 不启用来源浮力组件，不让波浪推动或摇晃船体。FFT 默认 Amplitude=0.04，适配当前缩比舰船。
- 弹道碰撞和 HUD 预测仍使用现有 Y=0 平均海平面。仅将落水表现投到异步读取的 FFT 高度；这不是逐波面物理碰撞，读回最多有约 0.1 秒采样间隔。
- 舰船扰动是局部高度场，没有新增流体阻力或完整船尾泡沫模型。域外不进行高分辨率浅水模拟。
- 岛屿附近的衰减使用现有三个岛屿的包围范围近似，不是海底深度驱动的波浪折射/破碎。
- 当前为 PC GPU 测试场景；性能预算和最终美术参数尚需在实际游玩视角下继续确定。

## 调整入口

- `Water Integration / FFT Wave Simulation`：Amplitude、Wind Speed、Choppiness、Use FFT。
- `Water Integration / Local Interaction Domain`：Size、Resolution、Damping、Max Height。
- `Materials/IntegratedSea.mat`：来源的颜色、岸边、法线、反射、焦散参数。
- `Settings/WaterIntegration_Renderer.asset`：SSR 最大距离、步数等。

## 后台验证与复现

GPU PlayMode 测试类为 WaterIntegrationTests，必须启用图形设备（可用 `-batchmode`，不可加 `-nographics`）。测试显式渲染相机，检查 shader、FFT 位移/法线、SSR、舰船接触、脉冲与移动窗口、实弹齐射、敌舰重生和退出场景后的配置恢复。报告和截图在忽略提交的 `Artifacts/WaterIntegration`。

`Tools/WaterIntegration/import_sources.py` 和 `adapt_sources.py` 是来源导入/适配记录，按顺序执行会覆盖 Imported 下的本地修改；日常调参不需要运行。构建入口 `Grand Navigation/Create Water Integration Test (new only)` 仅在场景不存在时创建，避免覆盖已完成地编。

下一步先在驾驶、瞄准、岸边三个实际视角确认颜色、反光、浪高和扰动强度，再决定是否把这些设置推广到正式海战场景。
