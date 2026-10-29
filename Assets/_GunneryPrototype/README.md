# Grand Navigation — 火控与炮术校准白盒

打开 Scenes/GunneryPrototype.unity，点击 Play。现有场景增量修改，保留原 PlayerShip、TargetShip、Projectile Prefab、六炮齐射和固定初速弹道。

## 设计意图
依据本项目“系统策划”对话：黄色区域只是上一次观测的参考；玩家观察航速、航向及上一轮弹着，主动修正青色准星。
扩大区域本身不会解决“中心即答案”。本版同时降低观测分辨率、增加观测与传递延迟，并提供可读的变速/转向目标。
不强制首轮打偏，不给炮弹加追踪，不随机偏移预测中心，也不扩大主炮散布。恒速目标仍然可以通过校正稳定命中，中心偶尔命中是正常现象。

设计参考：
- World of Warships Gunnery and Aiming：飞行时间、目标运动与提前量判断。https://wiki.worldofwarships.com/Ship%3AGunnery_and_Aiming
- War Thunder 2017 年海战瞄准开发日志：测距、瞄准与射击校正思路。历史参考，不声称复刻当前版本。https://warthunder.com/en/news/4871-fleet-naval-test-new-aiming-system-hydrodynamics-and-other-changes-

## 操作
- 左键点击橙色舰船锁定；Tab 循环选择仍存活的 A/B/C，切换时退出瞄准。
- 首次锁定后约 2 秒收到第一份位置观测，约 6.5 秒建立第一份航速估计。显示 RANGEFINDING 时等待，再右键进入瞄准。
- 瞄准中鼠标调整世界海面落点；左键一次齐射，最多六发；右键回普通视角；滚轮缩放；Esc 退出并解锁。
- R 重置全部靶舰、弹丸、装填、火控历史和玩家校正。
- 旧开发快捷键 1/2/3/4 保留，作用于当前目标（无锁定时 A）：静止/横向8/斜向12/旧式机动10。重新进入 Play 恢复三舰默认测试配置。
- 黄色椭圆不显示中心十字，且不是必然覆盖目标的概率区间。青色十字才是本轮实际落点中心。
- HUD 显示采样航速、观测年龄和传递延迟；STALE 提醒信息已老化。目标锁定框仍跟随真实位置。
- 观察水柱、落水泡沫与敌舰运动，修正后再次齐射。无试射、长按连发、玩家驾驶、敌舰攻击。

## 当前场景 Inspector 配置
以下是场景保存值；新增组件的零延迟实验室默认值用于兼容旧测试，并非本场景实战配置。

| 参数 | 当前值 |
|---|---|
| fireControlUpdateInterval | 4.5 s |
| observationLatency | 2 s |
| useOpticalObservations | true |
| rangeResolution / bearingResolutionDegrees | 8 m / 2° |
| baseLongitudinalRadius / baseLateralRadius | 18 m / 12 m（半径） |
| minRadius / maxRadius | 6 m / 36 m |
| ageGrowthPerSecond / regionResponse | 1.2 / 2 |
| distanceGrowthPerMetre / speedGrowth / turnGrowthPerDegree | .015 / .1 / .06，沿用原值 |
| aimFov / aimOffset / impactFraming | 44° / (0,42,-78) / .25 |
| 水柱 / 落水泡沫持续 | 默认 1.5 s / 至少 4 s |
| 初速 / 重力 / 最大水平射程 | 65 m/s / 9.81 m/s² / 250 m，未改 |
| 主炮散布 / 装填 / 单发伤害 | 3 m / 5 s / 40，未改 |

## 三艘同时存在的靶舰
沿用同一个 TargetShip Prefab，场景实例覆盖运动参数，HP 均为 600。

| 目标 | 运动 | 初始位置 |
|---|---|---|
| A / CONSTANT | PatrolLinear，8 m/s 匀速直线，边界折返 | (-70,0,115) |
| B / THROTTLE | VariableSpeed，5→14→7→12 m/s，阶段4.2 s，变化率2.5 m/s² | (-45,0,160) |
| C / EVASIVE | VariableManeuver，同样变速，阶段错开1.6 s；直航2.8 s，转向2.4 s；循环 -30°,+45°,-20°,+15° | (5,0,205) |

X 巡航区间均为 [-95,95]；Z 航区分别为 [100,130]、[145,180]、[190,225]。边界折返用于维持白盒交战范围，不是战术 AI。速度报告使用实际每固定步位移，与移动一致。

## 火控实现
FireControlObservationBuffer 按目标保存待送达观测队列。采样时将距离/方位按分辨率量化，航速由连续两次位置读数的差分得到；光学模式完全不调用真实速度 getter。
延迟到期才将观测交给既有 FireControlTrack，时间戳始终保留采集时间。每帧用旧观测恒速外推，再调用原 BallisticSolver 迭代求交会点，不冻结预测点。
首份观测的速度假设为零，HUD 提示正在建立运动轨迹。切换视角和快速重新锁定不会刷新缓存。暂停及 timeScale 同时作用于移动、采集、送达和年龄。
失效目标清除缓存。ManualAimController、WeaponManager、ProjectileController、BallisticSolver 和 Health 未改动；原六炮共用开火瞬间中心、各自解算散布落点。
预测区域沿估计航向，按距离、估算速度、年龄和已观测转向扩大，平滑过渡。世界网格由相机正常投影/裁切，并对舰体做深度遮挡。镜头适当兼顾目标和手动落点，不能改变准星坐标。
HUD 复用原组件，显示当前选择目标的名字及 HP；新增三目标标签与切换。泡沫延长便于装填期间观察上一轮落点。

## 已执行验证（2026-10-29）
- Unity CLI / Pipeline 重新编译：成功，0 errors / 0 warnings。
- EditMode：27/27，通过原弹道、采样、独立瞄准、区域测试及新增光学观测测试。
- 完整 PlayMode：16/16，通过；最后 HUD 文案/标签修正后，真实场景 CombatCalibrationTests 2/2 再次通过。
- 旧回归通过 LaboratoryProfile 显式恢复旧精确观测、单目标和旧运动参数；真实场景测试另外覆盖实际 4.5+2 秒光学配置与三个 NPC，二者不混淆。
- 真实场景测试覆盖首次获取、重锁不能加速、采样不拖动准星、两轮共12发、装填限制、65 m/s 初速、三目标死亡/重置、航区约束及运动差异。
- 纯逻辑匀速光学观测测试（尚未边界折返）：中心对实际拦截位置误差均值7.94 m、最大14.55 m。该确定性样本不是命中率统计。
- 32 秒实机场景轨迹测试包含边界折返：A/B/C 中心预测误差均值约29/47/30 m；折返会显著放大旧航向误差。这些数值不代表所有战斗，也不能解释为射击命中率。
- 实机 Play Mode 视觉复核：三舰总览、瞄准区域、青色准星及六发水柱；一次实际齐射记录6发、6次落水。截图保存在忽略的 Temp/calibration-*.png。
- 原高初速连续碰撞、一次伤害结算、真实炮口、原输入、装填、死亡与复活回归通过。
- Prefab、Packages、Unity 版本、重力、初速、弹道和原武器/伤害逻辑均未修改。未验证独立 Player Build。

## 自审与剩余调优
- 延迟和位置量化造成自然估计偏差，不保证每轮都偏；静止/稳定直航被熟练玩家打中符合目标。
- 海域边缘为简化反向，反向后数秒旧预测可能严重失效；黄色区域不保证包含未来位置。实战调优应特别关注此阶段是否过难，后续可改为更大的连续巡航航路。
- 本次完成工程验证和自动化观察，不等于多人盲测。下一轮优先比较玩家首轮/第三轮命中率、校正方向与过时提示理解度，再调观测分辨率/延迟；不要用散布代替火控设计。
- 英文 HUD 沿用现有字体，避免引入字体/Package。无远程仓库或推送。

## 测试命令
    unity command test run --mode editor --filter GrandNavigation.Gunnery.Tests --project-path D:\UnityHub\GrandNavigation
    unity command test run --mode playmode --filter GrandNavigation.Gunnery.Tests --async_tests true --project-path D:\UnityHub\GrandNavigation
    unity command test status --project-path D:\UnityHub\GrandNavigation

等待测试退出 Play Mode 后再打开场景。不要运行旧的白盒重建菜单，否则会重新生成旧版场景参数。
