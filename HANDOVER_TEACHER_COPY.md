# KaceLab 教师端静态副本交接手册

> 用户最新执行范围：按最新汇报编号1（交互收尾）→3（全量英文/双语）→4（UI、美术及全仪器最终验收）完成后停下。2（物性反应扩充）后续慢补，5（学生同步/最终Demo）暂缓。完整定义和当前未验收修改见 CURRENT_GOAL.md 顶部，不再沿用旧编号混淆范围。

> 最新：Windows 定向输入管道退出停留已修复。无人连接、已连接空闲、半行未结束三组各143/143并自然退出0，见 docs/定向输入退出修复.md 与 Logs/pipe-shutdown-final-evidence。教师主目录Build已更新；下方退出需强杀的记录仅作历史，不代表当前版本。

> 教师主目录后续修复：GPU 固定步预算现随输入时间增长，解决3倍在60FPS下只能推进2倍的问题，仍使用90Hz固定步且不丢余时。基线82通过/3失败→GPU85/85；View11/11、控制台143/143、Player453/453，Build已更新。详见 docs/流体倍速调度修复.md。下方“GPU预算未改”已被本条覆盖，管道退出停留仍未修复。

> 教师端同步最终验证：Logs/teacher-sync-final-build.log 编译成功 0 错误；Logs/teacher-sync-final-assertions.txt 控制台 143/143（新增 Enter 提交超限值拒绝）；Logs/david-visual-audit.txt 残影像素 26/26，后者自然退出 0。控制台管道退出停留需清理自有测试进程的问题仍在。Build 与教师主目录一致，后续继续 GPU 倍速固定步调度的已知功能缺陷。

> 2026-09-27 最新工作区变更：用户现已授权同步教师端并继续未完成目标。29 个差异文件已从副本同步到 D:\ChemLabUnity，覆盖前文件及 SHA256 清单备份于 D:\ChemLabUnity_SyncBackup_20260927_193859。后续以教师主目录开发，副本不再自动同步；下方“仅在副本/停下”等历史工作边界由此覆盖。学生端和先前明确暂缓的发布任务仍不启动。

> 2026-09-27 最新覆盖：world_speed 范围已改为 0–3，超限红色报错、保持原值；控制台吞掉已有拖动松手事件已修复，包含 David 最后归位。编译 0 错误，控制台回归 141/141。GPU 液体每帧步数预算未改，倍速同步限制仍需区分；详见 docs/控制台审查与残影修复.md 最新小节。只更新副本及 Build，未同步源工程/学生端。

> 2026-09-27 审查与用户追加修复：液体包围盒被普通残影材质画成方块的问题已修复，现捕获独立 GPU 液面及固体裁剪数据。新像素审计26/26，控制台124/124断言通过，最终构建成功；管道专项的退出停留已如实记录。审查另外发现高倍速液体调度不匹配、控制台吞掉已有拖动的松开事件，尚未改动。详见 `docs/控制台审查与残影修复.md`；本轮仅改教师副本，源工程和学生端未同步。

更新：2026-09-26。供接手本项目的 agent 先读；历史过程与更细的实现记录仍在 `HANDOVER.md`、`CURRENT_GOAL.md` 和 `docs/`。这些历史文件按时间累积，早期条目（尤其“动态固体默认关闭”“控制台/演示包即将制作”）已被本手册的当前状态覆盖。

## 1. 工作边界与当前结论

- 教师工程的源路径为 `D:\ChemLabUnity`，本手册随整份工程复制到 `D:\KaceLab_TeacherCopy1`。副本是交接快照；**2026-09-26 用户明确指示本轮只在副本 `D:\KaceLab_TeacherCopy1` 继续**，源工程与学生端本机未改动（复核办法：`D:\ChemLabUnity` 下「01:30 之后被改动的文件数」应为 0）。回到源工程或做任何双向同步前必须先问用户。
- 本轮在副本里完成了开发者控制台接入与两条命令（`world_speed`、`david`），证据与限制见 `CURRENT_GOAL.md` 顶部与本手册第 7 节；控制台不再是“只有壳未接入”的状态。
- 用户上一轮的要求（跳过反应标签、完成第 3 项收尾、复制全工程并附交接手册后停止）已经执行完；本轮的新授权只覆盖控制台与两条命令，**仍未授权**打包新版 Demo、同步学生端或启动原编号 1/4/5/6。
- 用户明确接受目前液体边缘的少许拉丝或剪切平面，也接受高负载导致的卡顿；以后有单独性能优化指令再处理。不能反过来把卡顿说成不存在，也不能用性能指标遮掩功能缺陷。
- 用户不允许自动化接管桌面鼠标键盘或抢焦点。测试应使用隐藏进程、引擎内部测试入口或进程定向输入（`-windowTargetInput` 与 `tools/*_target_input_test.ps1`）。`HANDOVER.md` 中旧的 Win32/SendInput 前台脚本是历史材料，**不要运行**。只有用户明确要求打开程序时才显示窗口。

## 2. 可运行状态和本轮验收

引擎为团结 `2022.3.62t15`，编辑器路径 `D:\Tuanjie\Editor\2022.3.62t15\Editor\Tuanjie.exe`。`Build\ChemLab.exe` 是本轮重建的 Windows 教师端（显示版本 `v0.0.2_SE_CESA`，平台数字版本 `0.0.2`）；它不是新桌面 Demo，也不是 Mac 构建。第 3 项收尾的构建记录是 `Logs\item3-final-build.log`：`Succeeded`、错误 0；**2026-09-26 控制台这一轮又重建过一次**（`Logs\console-build.log`，`Succeeded`、错误 0），`Build\` 里的二进制目前对应控制台版本的源码。编辑器启动时出现过 Licensing Client 的非致命噪声，以最终构建结果和 Player 退出码为准。

| 第 3 项收尾检查 | 结果 | 证据 |
| --- | --- | --- |
| CAIE 规范用例 | 199 通过，0 失败 | `Logs/item3-final-spec-results.json`、`Logs/item3-final-spec-editor.log` |
| 核心自检 | 175 通过，0 失败 | `Logs\selftest.txt`、`Logs\item3-final-self-editor.log` |
| 物态/物性审计 | 3374 通过，0 失败 | `Logs\phase-audit.txt`、`Logs\item3-final-phase-editor.log` |
| Windows Player 全功能回归 | 453 通过，0 失败 | `Logs\interaction-regression.txt`、`Logs\item3-final-interaction-player.log` |
| GPU 固体/液体障碍专项 | 69 通过，0 失败 | `Logs\gpu-solid-obstacle-audit.txt`、`Logs\item3-close-gpu-player.log` |
| 原始固体形状装填 | 29 通过，0 失败 | `Logs\solid-packing-audit.txt`、`Logs\item3-close-packing-player.log` |
| 真实器皿视图/融化/暂停/粉末床 | 11 通过，0 失败 | `Logs\solid-view-audit.txt`、`Logs\item3-close-view-player.log` |
| 原生固体接触/状态 | 79 通过，0 失败 | `Logs\solid-pair-contact-audit.txt`、`Logs\item3-final-contact-player.log` |
| 8 / 160 份种子各 600 实际帧 | 几何边界和原形重叠门槛通过，没有中止 | `Logs\solid-view-frame-audit.txt`、`Logs\solid-view-160-offscreen.png`、`Logs\item3-final-frames-player.log` |

160 份种子压力场景使用 250 mL 烧杯、50 mL 水和约 162 g 冰，**实际有 94 份在器皿内**，66 份因容量/装填限制摆在台面上；不能把它描述为“160 块全部装进杯中”。装入的 94 份没有测得器皿腔体越界或可见原形穿透（0.02 mm 验收门槛）。此场景实际帧 `p95=57.2606 ms`，最长 `629.2868 ms`；性能栏标记 `PERF`，不是 `PASS`。8 份场景 `p95=16.6812 ms`、最长 `17.2832 ms`。全功能回归的四器皿 600 帧 `p95=16.7127 ms`、最长 `171.4565 ms`，此机当时还有用户自己的高负载后台程序。未重跑所有历史三分辨率定向 UI 脚本或真实 Mac Player，不能把上述测试扩展成全平台全输入验收。

控制台这一轮的验收结果、文件和实测数字见第 7.5 节（结论：控制台专项 123/123、窗口管道三档各 27/27、Player 完整回归 453/453）。

## 3. 第 3 项具体实现和边界

- 普通新建容器现在默认开启动态固体与原生接触。实现入口在 `Assets/Scripts/Game/LabBootstrap.cs` 的 `VesselView.Create`，测试用 `-noDefaultSolidDynamics` 可以关掉默认值作负对照。此前 `EnableSolidDynamics` 仅显式开启的历史记录不再代表当前版本。
- `VesselView.AdvanceContents` 与 `SolidBodyBatch` 保持固体原始不规则网格的质量、体积、可见姿态、浮沉和液体位移；原生 PhysicsScene 处理壁、底、固体间接触。融化按同一物质/相保留存活块位姿和速度；完全融化后释放障碍。倾倒事务期间两个视图均暂停。粉末床是静态连续网格，不是假装逐粒动态粉末。
- `GpuFluid` / `Fluid3D.compute` 在 GPU 上处理液体粒子及固体同形障碍；`ChemLabFluidVolume.shader` 依实际网格遮罩避免液体界面进入固体，另保留连续粒子核插值。最新 GPU 专项包含真实渲染射线命中、液体边界、障碍清除、速度黏性近似等检查。没有在生产路径加入逐帧 CPU 粒子回读。
- 固体时间债务以秒累计、每次视图更新最多处理 0.25 s，未处理余量继续保留；这是防止单次长帧无限追算并尽量不丢模拟时间的折衷。GPU 流体也累计其未执行的固定步。持续低帧率可造成模拟落后，不能宣称恒实时。
- 这是 CIE 课堂尺度的宏观交互：浮力、位移、碰撞和同形障碍已接通，**并非**严格的双向 Navier–Stokes 固液耦合。流体湍动不会给固体施加逐粒压力扭矩，固体姿态暂锁定；粒子黏性强度不是按物质/温度标定的 Pa·s。用户允许这一级别的简化和局部液面瑕疵，本次不继续打磨液膜或帧率。
- `SolidViewAudit`、`InteractionRegression` 默认把帧率指标记录为 `PERF`；`-strictPerformance` 可恢复旧的 p95 / 最长帧严格门槛。密集审计在普通模式仍有 300 s 防挂安全上限，严格模式保留原 1 s 单帧 / 30 s 夹具预算。不要把 `PERF` 误记为未执行检查。

## 4. 架构索引

| 范围 | 主要文件或目录 |
| --- | --- |
| 数据、反应和库存 | `Assets/Scripts/Chem/ChemCore.cs`、`ChemData.g.cs`、`ChemEngine.cs`、`ChemSelfTest.cs`；完整物质表见根目录 `物质总清单.md` |
| 温度、物态、溶剂、饱和度 | `Assets/Scripts/Chem/PhaseProperties.cs`、`SolventProperties.cs`、`PureSolubility.cs`、`MixedSolubility.cs`、`VesselPhaseExchange.cs`、`ThermalMixing.cs` |
| 蒸馏、冷凝与量器 | `Assets/Scripts/Chem/CondenserHeatTransfer.cs`、`CondenserThermalStorage.cs`、`DistillationAssembly.cs`、`MeteredTransfer.cs`；专门说明见 `docs/冷凝传热模型.md` 等 |
| 主场景、视图、相机/交互 | `Assets/Scripts/Game/LabBootstrap.cs`、`VesselMesh.cs`、`NativeSolidWorld.cs`、`LabPick.cs`、`LabUI.cs`、`ReagentDrag.cs`、`ReagentDosing.cs`、`StagedPourUI.cs` |
| GPU 流体与固体形状 | `Assets/Scripts/Game/GpuFluid.cs`、`SolidBodyBatch.cs`、`SolidGeometry.cs`、`SolidMeshBvh.cs`、`PowderBed.cs`；`Assets/Shaders/`、`Assets/Resources/`；详见 `docs/GPU固体网格障碍.md`、`docs/固体初始装填.md` |
| 试剂图片、商标、标签 | `Assets/Scripts/Game/ReagentArtCatalog.cs`、`GlasswareBranding.cs`、`EquationTag.cs`；原图 `ArtExports/kazewa/_reagents/_v1` |
| 开发控制台与两条命令 | `Assets/Scripts/Game/DeveloperConsole.cs`（界面/日志/命令分发，已接入）、`WorldClock.cs`（`world_speed` 的唯一实验时钟闸门）、`DavidMotion.cs`（`david` 突进与残影）、`Assets/Resources/ChemLab/Ghost.shader`（残影着色器）、`Assets/Scripts/Game/ConsoleCommandAudit.cs`（隐藏 Player 验收）、`tools/console_target_input_test.ps1`（窗口管道验收） |
| 编辑器命令和测试 | `Assets/Editor/ChemLabTools.cs`、`PhaseAudit.cs`、`SpecRunner.cs`；Player 回归入口在 `Assets/Scripts/Game/InteractionRegression.cs`、`SolidPairContactAudit.cs` 等 |

场景 `Assets/Scenes/Lab.unity` 主要挂载 `LabBootstrap`；大量几何、UI、材质在运行时程序化创建。`ProjectSettings`、`Packages`、`Library`、`UserSettings` 是可直接由编辑器打开此快照的工程组成；`Build` 是教师端 Windows Player；`Logs` 是验收记录和失败样本，**不要只复制 Assets 当完整交接**。`Assets/Resources/ai_config.json` 可能包含教师端本地 AI 配置；整份教师备份包含它，但公开 Demo 或学生版不得直接复制该配置或泄露内容。

## 5. 安全复测指引

所有下列命令都应在 `D:\ChemLabUnity`（或明确选择副本作为工作项目后将路径整体替换）运行。Windows PowerShell 中使用 `Start-Process -WindowStyle Hidden -PassThru -Wait`，不操控桌面输入；进程退出码必须检查。不要同时启动两个编辑器实例打开同一工程。

```powershell
$editor = 'D:\Tuanjie\Editor\2022.3.62t15\Editor\Tuanjie.exe'
$p = Start-Process -FilePath $editor -ArgumentList @('-batchmode','-quit','-nographics','-projectPath','D:\ChemLabUnity','-logFile','D:\ChemLabUnity\Logs\verify-build.log','-executeMethod','ChemLab.Editor.ChemLabTools.BuildPlayer') -WindowStyle Hidden -PassThru -Wait
$p.ExitCode
```

编辑器检查分别换用 `ChemLab.Editor.ChemLabTools.RunTests`、`ChemLab.Editor.PhaseAudit.Run`。CAIE 规范回归使用 `ChemLab.Editor.SpecRunner.Run`，并传 `-specIn=D:\ChemLabUnity\caie\spec\cases_all.json` 与自选绝对 `-specOut=...json`；不可漏掉 `-specIn` 后把未运行用例当作通过。Player 的最终功能回归是 `Build\ChemLab.exe -batchmode -interactionTest -noTut -logFile <绝对路径>`；GPU、装填、真实视图、接触专项分别换标志 `-solidGpuTest`、`-solidPackingTest`、`-solidViewTest`、`-solidContactTest`。600 帧压力使用 `-solidViewFrames -nativeSolid -longSolidFrames -noTut`。不要给这些需要 GPU 的 Player 加 `-nographics`。历史前台 Win32 自动化脚本禁止再用。控制台与两条命令的专项复测命令见第 7.6 节。

## 6. 后续工作仅作交接，不授权立即执行

1. 用户当前要求是：控制台与两条命令已交付，**停下等待下一步**。不要因为先前的路线图而自行开始 Demo 打包或编号 1/4/5/6。
2. 控制台已接入：`~` 开关、CLI 日志、焦点隔离（`LabInput` 在 `InputCaptured` 时整体中性化）与两条命令 `world_speed 0–5`、`david 0/1 [秒]` 都已实现并验收（见第 7 节）。仍**未实现**的是后续可能追加的业务命令；不要再把 `DeveloperConsole.cs` 当“只有壳”。
3. 新桌面 Demo 标签预定 `v0.0.2_SE_CESA`；本轮**没有**重新打包 Windows 或 Mac Apple Silicon，桌面历史包不是本轮代码。学生版也按用户要求等所有功能完成后统一同步；不要把旧学生包当成当前教师端。
4. 更完整的全温度/非水溶剂活度、所有物种定量溶解度、封闭容器复杂热力和全部有机反应覆盖不是这次 453 项 Player 检查可证明的。气体课堂模型以理想气体 `PV=nRT` 为准；具体缺数据保护和既有范围见 `CURRENT_GOAL.md`、`docs/`。
5. 反应标签由用户确认已无问题，本轮没有再追加修补。若将来出现新回归，`ReactionEvent.Sequence` 和 `EquationTag` 的有界历史游标逻辑见 `ChemEngine.cs`、`LabBootstrap.cs` 与 `InteractionRegression.cs`，不要恢复按历史列表索引消费的旧写法。

## 7. 开发者控制台与两条命令（2026-09-26，只在副本 D:\KaceLab_TeacherCopy1）

用户在本轮直接授权：**把已写好的控制台接入主界面/输入，并实现两条命令 `world_speed` 与 `david`**，随后又按视觉反馈追加了四轮修正（观感、节奏、参数）。源工程 `D:\ChemLabUnity` 与学生端未改动。全程没有接管桌面鼠标键盘：所有测试都用隐藏进程 + 命名管道或引擎内自测入口。

### 7.1 产物与职责

| 文件 | 状态 | 作用 |
| --- | --- | --- |
| `Assets/Scripts/Game/DeveloperConsole.cs` | 已有，小改 | 控制台外壳（界面、日志镜像、`Submit`/Enter 提交、`~` 开关本来就是它自己的）。本轮只加了测试用只读访问点 `TestInputField`。 |
| `Assets/Scripts/Game/WorldClock.cs` | 新增 | `world_speed` 的唯一实验时钟闸门：`Rate`、`Scale/DeltaTime`、`TryParse/TrySet`（越界拒绝而非钳制）、`ScaledSeconds`、`ResetForTest`。 |
| `Assets/Scripts/Game/DavidMotion.cs` | 新增 | `DavidMode`（命令解析 `0/1 [秒]`、默认值与范围）+ `DavidDriver`（突进状态机、退休排程、残影池、每段网格拷贝、审计访问点）。 |
| `Assets/Resources/ChemLab/Ghost.shader` | 新增 | 残影着色器：扁平高饱和实色 + 掠射角灰黑描边（普通 alpha 混合）。放 Resources 下以保证进包。 |
| `Assets/Scripts/Game/ConsoleCommandAudit.cs` | 新增 | 隐藏 Player 专项审计，由 `-consoleTest` 启动（必须同时给 `-windowTargetInput`）；进程内注入与外部管道相同的协议行，跑真实输入路径。 |
| `Assets/Scripts/Game/LabBootstrap.cs` | 改 | 创建控制台与 `DavidDriver`、注册两条命令；`Update`/`SyncViews` 改用缩放后的实验时间；`HandleInput` 顶部隔离控制台输入，拖动拆成“鼠标意图 / 突进视觉”两条路，松手可延后执行；新增 `BeginDavidVesselDrag/ApparatusDrag/TestItemDrag`、`ResolveDragRelease`、`DragIntentPosition` 与只读入口 `TestFocusPoint`/`TestDragTargetId`；`VesselView` 内新增 `TestSolidPositions`/`TestSolidSolverActive`。 |
| `Assets/Scripts/Game/WindowTargetInput.cs` | 改 | `LabInput` 在 `DeveloperConsole.InputCaptured` 时对所有取键/取鼠标/滚轮返回中性值；新增 `InjectForTest`（进程内注入同一协议）与 `CONSOLE`、`CONSOLETEXT`、`DAVID` 三个测试查询命令。 |
| `Assets/Scripts/Game/EnthalpyScene.cs` | 改 | 控制台打开时不再跟随鼠标画待连的线。 |
| `tools/console_target_input_test.ps1` | 新增 | 三档分辨率窗口管道验收（故意只用 ASCII，自带隐藏 Player 启停与报告解析）。 |
| `CURRENT_GOAL.md`、`HANDOVER_TEACHER_COPY.md` | 改 | 本轮状态与本手册。 |

### 7.2 命令行为（按用户最终要求，已实现并验收）

**`world_speed <0–5>`**
- 支持小数（`0.25`/`2.5`/`4.75` 等）；非法、非数字、非有限（`NaN`/`Infinity`）、越界（`-1`/`6`/`5.5`）**一律拒绝**并给出 `[ERR]` 原因，**不静默钳制**。
- 受它控制的实验时钟（**审计时逐个检查过**）：`Lab.Tick`、`ApparatusSet.Tick`、`TestSet.Tick`、`LabContacts.Tick`、`VesselView.SyncMeteredFlow`、`VesselView.AdvanceContents`（固体求解 + GPU 流体）、粒子/液面视觉时钟 `FxTime`。速率为 0 时这些整段跳过 → 反应、热力、固体/液体模拟、计量流动全部冻结。
- **不受影响**：相机移动、菜单、标签、阿侬、控制台自己（都走真实帧时间）。没有改 `Time.timeScale`；`world_speed` 不代表帧率或渲染开销变化。
- 新建固体求解器时“首次自动更新只计已存在时长”的折算也乘了同一速率（`Time.timeScale * WorldClock.Rate`）。

**`david <0|1> [秒]`**
- `david 1`：开启；两次 dash 之间的间隔取默认 **0.2 s**。`david 1 0.1`：间隔 0.1 s。间隔范围 **0–10 s**。
- 非法输入（`abc`、`-1`、`20`、`0.2 0.3`、`1.5x` 等）**拒绝**且**不改变**当前模式与间隔；`david 2`、`david yes`、`david 1 foo` 同样拒绝。
- `david 0`：关闭并立即清空残影、把仪器放回指针位置。`david 0 <秒>` 也接受（只校验，不生效）。
- 间隔可以在运行中改：驱动每帧读 `DavidMode.PauseSeconds`，下一条 dash 就按新值。
- 覆盖对象：普通器皿与量器、挂在同一 `VesselView` 下的蒸馏/回流组件、器材（本生灯/三脚架）、检验道具；内容物（液体/沉淀网格）随容器一起进快照。

### 7.3 残影观感与节奏（用户逐条指定后的最终规则）

- **视觉**：扁平、高饱和的实色剪影 + **约 70% 不透明度的灰黑描边**；普通 alpha 混合（不是加法混合）。色相加深为青 `(0.02,0.55,0.95)` → 绿 `(0.05,0.85,0.20)`，沿每段 dash 从蓝渐变到绿；填充不透明度 0.52，光泽度 1.0，饱和度 1.25。描边在同一 pass 里用掠射角边缘实现（反面壳会把中空玻璃器皿的内部整片涂黑，还要给每个残影多开资源）。
- **间距**：沿路径按「物体横向尺寸 × 0.7」采样（烧杯实测 0.490 世界单位），相邻剪影保持重叠但可数。
- **dash 中不设数量上限**：沿整条路径连续生成，**仪器前方不留空挡**（这是用户明确要求，之前“每段最多 5 个”的做法会在路径中段停住，被否掉）。
- **停顿里按从老到新依次退休**：每段 dash 结束时，给当时在场的每个剪影按生成顺序排一个退休时刻，均匀铺满整个间隔，各自用 0.12 s 淡出；因此下一段 dash 开始时画面已经清空，不会带着上一段残留起跳。
- **松手/关闭立刻清空**：`david 0` 与松手后的最后一段直线结束时残影即时清除（不排队退休）。
- **不要回退的版本**（都被用户否掉过）：① 加法混合 + 菲涅尔发光（重叠处叠成白色，看着像不透明白块）；② 每段限制残影数量的做法（仪器前出现空挡）；③ 固定 0.5 s 寿命、跨段继续残留的写法（停顿结束仍在画面里）。

### 7.4 实现要点与可调常量

- **时间闸门**：除 7.2 列出的入口外，没有其它地方消费实验时间；新增子系统时必须过 `WorldClock`，否则 `world_speed` 会漏掉它。
- **输入隔离两层**：`LabInput` 整体中性化（覆盖所有既有消费者，关闭时行为与原来完全一致）+ `HandleInput` 顶部早退（省掉射线并且更明确）+ `EnthalpyScene.Update` 停止跟随鼠标。
- **拖动拆分**：`HandleInput` 用鼠标算出「意图落点」（含原有吸附对齐），普通模式直接写模型，`david` 模式把意图交给 `DavidDriver.SetIntent`，视觉位置由突进而来的偏移决定；松手时 `BeginFinish` 先跑完最后一段直线到意图落点，再执行原来的 `ResolveDragRelease`（吸附/放下/倾倒一行没改）。突进期间的吸附判定用意图落点（`DragIntentPosition`），不用视觉位置。
- **残影生命周期**：每个 dash 段开始时拷贝一次当时的可见网格（排除 `Fx`/`Glow`/`GlowRing`/`Flame`/描边），该段所有剪影共用这份拷贝；拷贝按段引用计数，最后一个剪影退休时释放。剪影对象池化复用。**没有**在生产路径加入逐帧网格拷贝或化学推进。
- **可调常量**（改观感只动这几处，重建约 20 秒）：
  - `DavidMotion.cs` 顶部：`DashStartColor` / `DashEndColor` / `BaseAlpha`（填充不透明度）/ `RetireFadeSeconds`（退休淡出时长）/ `DashStartColor` 渐变范围；
  - `DavidMode`：`PauseSeconds` 默认值（`DefaultPauseSeconds = 0.2`）、`MinimumDashUnits`/`MaximumDashUnits`（0.6–5 世界单位 = 最远 50 cm）、`DashSpeed`、间隔范围 `MaximumPauseSeconds = 10`；
  - 采样间距：`CaptureVisual()` 里的 `extent * 0.7`；
  - 描边：`Ghost.shader` 的 `_OutlineColor`（alpha 0.7 = 70%）/ `_OutlineStart`（越小描边越宽）/ `_OutlineFalloff`。
- **审计访问点**（给测试用，不在玩法路径）：`IntentForTest`、`VisualForTest`、`OffsetMagnitudeForTest`、`PhaseForTest`、`SegmentIndexForTest`、`GhostsForTest()`、`SegmentsForTest`、`GhostSpacingForTest`、`ScheduledGhostCountForTest`、`LiveGhostCountForTest`、`CopiedMeshCountForTest`、`TestSeed`、`TestSetNextSegmentLength`、`TestClearSegmentLog`、`Dispose`。

### 7.5 本轮验收结果（结论都在这张表，全部 0 失败）

| 检查 | 结果 | 证据 |
| --- | --- | --- |
| 独立构建 | Succeeded，0 错误 | `Logs/console-build.log` |
| 核心自检 | 175 / 175 | `Logs/console-selftest-editor.log` |
| CAIE 规范用例 | 199 / 199 | `Logs/console-spec-results.json` |
| Player 完整回归 | 453 / 453 | `Logs/interaction-regression.txt`、`Logs/console-interaction-player.log` |
| 控制台+两条命令专项（隐藏 Player，进程内注入真实输入路径） | **123 / 123** | `Logs/console-audit.txt`、`Logs/console-audit-player.log` |
| 窗口管道验收 800×600 / 1280×800 / 1920×1080 | 各 **27 / 27** | `tools/console_target_input_test.ps1` |
| GPU / 装填 / 真实视图 / 接触专项复测 | 69 / 29 / 11 / 79 | `Logs/gpu-solid-obstacle-audit.txt`、`solid-packing-audit.txt`、`solid-view-audit.txt`、`solid-pair-contact-audit.txt` |

专项审计里的关键实测数字（`Logs/console-audit.txt`）：

- `world_speed`：10 类非法输入全部拒绝且速率不变；`0/0.25/1/2.5/4.75/5` 全部按原样生效；速率 0 时时钟、温度、库存、反应数、固体位姿、GPU 流体场重建全部冻结，恢复 1 后重新推进；0.5/2/5 三档的“时钟推进/墙钟秒”比值与“独立耗水率”比值一致（实测 0.5000 / 2.0000 / 5.0001 与 0.5000 / 2.0000 / 5.0000）；冻结状态下控制台仍可打开并恢复速率。
- `david`：一段拖拽内 7 段突进，最短 0.619 单位/1 个剪影、最长 **5.000 单位/10 个剪影**，**最大间隔 0.490 = 间距 0.490**（无空挡、无数量上限）；相邻剪影间距 0.490 对物体横向 0.700；6 次停顿全部满足“老→新退休”“下一段前清空”“退休时淡出”，同时在世剪影最多 10；颜色严格落在蓝绿渐变上、半透明且保持饱和度；剪影无 `VesselView`/`Collider`/`Rigidbody`，拖拽期间成分数与反应数不变；松手落点误差 0.00000、落到另一容器仍进原倾倒流程且取消可复原；`david 0` 立即恢复；池与拷贝网格全部回收（`cleanup` 计数相等）。`david 1 0.45` 实测段间隔中位数 **0.465 s**，之后 `david 1` 回到 0.2 s 默认。
- 拖拽期间帧时：`p50 16.70 / p95 20.49 / max 24.99 ms`；四器皿 600 帧这一轮 `p95 33.57 ms / max 56.0 ms`，**只记 PERF，不作门槛**，不当成通过或失败。

### 7.6 复测命令（Windows PowerShell 7 `pwsh`，隐藏进程，不接管桌面输入）

```powershell
$root='D:\KaceLab_TeacherCopy1'
$editor='D:\Tuanjie\Editor\2022.3.62t15\Editor\Tuanjie.exe'

# 构建（编辑器批处理，-nographics 可以；不要开两个编辑器实例）
Start-Process $editor -WindowStyle Hidden -Wait -PassThru -ArgumentList @('-batchmode','-quit','-nographics',
  '-projectPath',$root,'-logFile',"$root\Logs\console-build.log",
  '-executeMethod','ChemLab.Editor.ChemLabTools.BuildPlayer') | % ExitCode

# 自检 / CAIE 规范
Start-Process $editor -WindowStyle Hidden -Wait -PassThru -ArgumentList @('-batchmode','-quit','-nographics',
  '-projectPath',$root,'-logFile',"$root\Logs\console-selftest-editor.log",
  '-executeMethod','ChemLab.Editor.ChemLabTools.RunTests') | % ExitCode
Start-Process $editor -WindowStyle Hidden -Wait -PassThru -ArgumentList @('-batchmode','-quit','-nographics',
  '-projectPath',$root,'-logFile',"$root\Logs\console-spec-editor.log",
  '-executeMethod','ChemLab.Editor.SpecRunner.Run',
  "-specIn=$root/caie/spec/cases_all.json","-specOut=$root/Logs/console-spec-results.json") | % ExitCode

# 控制台+两条命令专项：必须同时给 -windowTargetInput（审计要用管道协议注入输入）
Remove-Item "$root\Logs\console-audit.txt" -Force -ErrorAction SilentlyContinue
$p = Start-Process "$root\Build\ChemLab.exe" -WorkingDirectory $root -WindowStyle Hidden -PassThru -ArgumentList @(
  '-consoleTest','-windowTargetInput','-noTut','-screen-fullscreen','0',
  '-screen-width','1280','-screen-height','800','-logFile',"$root\Logs\console-audit-player.log")
do { Start-Sleep -Seconds 1 } while (-not (Select-String -Path "$root\Logs\console-audit.txt" -Pattern '^TOTAL' -Quiet))
Get-Process ChemLab -ErrorAction SilentlyContinue | Stop-Process -Force   # 见 7.7 最后一条
Get-Content "$root\Logs\console-audit.txt" -Tail 5

# 三档窗口管道验收（脚本自己开关隐藏 Player，只写命名管道）
& "$root\tools\console_target_input_test.ps1" -ProjectRoot $root -Width 1280 -Height 800

# Player 完整回归 + 四个固体专项（不要加 -nographics）
Start-Process "$root\Build\ChemLab.exe" -WorkingDirectory $root -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
  '-batchmode','-interactionTest','-noTut','-logFile',"$root\Logs\console-interaction-player.log") | % ExitCode
# 专项标志：-solidGpuTest / -solidPackingTest / -solidViewTest / -solidContactTest
```

### 7.7 必须知道的边界与坑

- **只在副本工作**：源工程 `D:\ChemLabUnity` 本机未改；要回去先问用户。
- `world_speed` 只缩放实验时间（0 冻结全部实验事件），不改变帧率或渲染开销，不能当作性能提升。
- `david` 是开发者调试效果：本体只保留单一清晰姿态，残影是静态网格快照（不推进化学、不新增物质）；突进期间按需求穿过其他物品、不做碰撞阻挡。
- **没有验证硬件键盘逐字输入**：管道协议只传按键码，命令文本由测试专用 `CONSOLETEXT` 注入；真实键盘由窗口版与既有工具覆盖。
- **打开控制台时它的全屏遮罩会挡住所有 UI 射线**：任何要拾取 3D 物件的测试都必须**在控制台关闭时**先取好落点（`FINDPICK`），否则会得到“全部被 UI 挡住”的假失败（本轮踩过两次）。
- **旧中文 `.ps1` 在 pwsh 下会因缺 BOM 解析失败**（`tools/object_menu_target_input_test.ps1` 等历史脚本就是这样）；新的 `console_target_input_test.ps1` 故意只用 ASCII，不要往里加中文。
- **隐藏 Player 偶尔在写完报告后不退出**（`Application.Quit` 未生效，本轮遇到两次）：用 `-Wait` 串联多步的脚本会被它卡住，也会留下进程抢占 CPU 并覆盖同一批日志。轮询到 `Logs\console-audit.txt` 出现 `TOTAL` 后自己 `Stop-Process`，不要假设 `-Wait` 一定返回。
- **`PERF` 不等于通过也不等于未执行**：帧率栏是记录项；功能失败必须单独列出，不要用性能数字遮掩。
- 面板布局与分辨率相关：3D 物件的可拾取落点可能落在侧栏/底部面板里（本轮 `v1`、演示场景的本生灯都遇到过），审计里已改为“先找第一个可拾取对象/换位置再失败”。

### 7.8 本轮明确未做的事（等用户指令）

1. 没有打包新的 Windows/Mac Demo，没有同步学生端，没有启动原编号 1/4/5/6。
2. 没有给控制台加更多业务命令（用户只授权了 `world_speed` 与 `david`）。
3. 没有做硬件键盘、手柄或 Mac 平台的输入验收；没有做多人/联网相关验证。
4. 观感参数（颜色/不透明度/描边/间距/间隔）目前是按用户当轮反馈调定的，若继续微调，改 7.4 列出的常量并重跑 7.6 的控制台专项即可。
