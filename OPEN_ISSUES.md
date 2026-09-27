# ChemLab 验收记录

> 历史记录提示（2026-09-28）：下文提及的 OS 输入/抢前台旧测试方法现已禁止使用。当前验收只使用隐藏 Player 的定向输入，最新记录见 docs/交互阶段最终验收.md、docs/双语验收进度.md 和 docs/刻度与外观验收.md。旧结果不可直接视为最新版本复验。

> 全部由 `tools/ui.ps1`（注入真实鼠标/键盘）+ 窗口测试实测得到。
> 每一条都有「游戏自报状态」或「截图」作为证据，不是编译通过或坐标推算的结论。

## 验收条件逐条核对

| | 条件 | 状态 | 证据 |
|---|---|---|---|
| **A** | 容器标签永久存在 | ✅ | `-tagAudit` 26 秒后仍报「存活标签 4 个」（反应标签存活期只有 10 秒）；重叠对数 = 0；四个标签屏幕 X = 1626/1230/884/478 各不相同；ExtraLift 全 0 |
| **B** | 方程式标签排容器标签上方 | ✅ | 按 `VesselTag.TopWorldY` 定位；自检断言「Born-Haber 循环的标注不压线也不互压」 |
| **C** | 聚焦轮廓高亮 / 透明染色 | ✅ | `点器皿后 glow 数 = 1 → 点空白后 = 0`；截图 `_glow_full.png` 显示轮廓一圈青色、内部透明；自检断言检查玻璃材质带 `_HighlightAmt` |
| **D** | 拖动无边界 / 防 z-fighting | ✅ | `HandleInput` 里 `Mathf.Clamp` 已删；器皿/器材/道具各抬 3mm |
| **E** | 3D 交互 | ✅ | 已还原成「点器皿拖动、点空白转视角、滚轮缩放」 |
| **F** | 试剂架分类+搜索、器材架八钮 | ✅ | `REAGENT\|cat\|metal→oxide`、`visible\|17→25`、`query\|CuSO4`、`visible\|25→2`；器材架截图文字完整 |
| **G** | 计算器全部功能 | ✅ | 见下表；手动输入对话框 `STATE\|dlg\|open` → 输入 `-123.5` → `STATE\|edge\|C->CO₂\|-124\|Manual` |
| **H** | 阿侬 | ✅ | 见下表 |
| **I** | 33 步 / 15 步教程 | ✅ | 33 步 `0/33→1/33→3/33→4/33`、跳过解锁 AI；15 步 `1/15→2/15`、跳过 `calcDone=yes` |

## 窗口测试（三套全绿）

| 测试 | 结果 | 覆盖 |
|---|---|---|
| `tools/uitest.ps1`（实验台） | **10/10** | 分类过滤、搜索过滤、器材架、聚焦高亮与取消、计算器开关、节点拖动 |
| `tools/calctest.ps1`（计算器） | **10/10** | 求解 +283、右键选单开关、拉线连线与取消、右键连线删除 |
| `tools/pettest.ps1`（阿侬与教程） | **10/10** | 教程启动/推进/跳过、解锁 AI、拖动阿侬、右键选单 |
| `tools/dlgtest2.ps1`（手动输入对话框） | ✅ | 控件坐标链路 + 对话框可用性单独验证 |

## 静态检查

```
Unity 自检    168 / 168
CAIE 规范用例  199 / 199
Web 自检       26 / 26
教师端/学生端   各 90.3 MB，0 错误
```

## 本轮（Round 9）修掉的

### 手动输入焓变对话框的按钮跑到面板外面 ✓
`MkBtn` 把锚点**硬编码成 (1,1)** —— 那是顶栏的用法。
对话框传 `pos = (-14, 12)` 期望的是右下角 (1,0)，
结果按钮被摆到面板上方 12px 的外面（面板屏幕 y 722..884，按钮在 935）。
改成用调用方传的 `pivot` 当锚点后回到面板内（y 735）。

### `STATE|vessel|` 的插入没写进文件 ✓
脚本报成功、括号也平衡，但代码不在文件里 —— 这正是纪律要求警告的
「只看改动处数」的陷阱。重新插入并在脚本里**当场打印核对**后发现生效。

### 聚焦高亮改用状态判定 ✓
原来靠像素判据，菲涅尔辉光太淡，实测在 1.4~4.5 之间跳、时过时不过。
改成读 `PET|glow`（聚焦高亮强度）后稳定：`0 → 1 → 0`。

### `Warm-Up $h` 少了参数且位置在拿到句柄之前 ✓
报了一堆 IntPtr 转换错误（功能不受影响）。修完报错归零。

## 所有已根治的 bug（含根因）

| bug | 根因 |
|---|---|
| 容器标签看不见 | `_panel.anchoredPosition = Vector2.zero` 把 `ct.position = anchor` 撤销了（`_panel` 就是 `ct`）；画布从 ScreenSpaceOverlay 改成 WorldSpace 后定位代码没跟着改 |
| 容器标签会消失 | 「有方程式标签时让位」这个前提本身就是错的 —— 容器标签就该永久在 |
| 透明器皿泛青 | 反向壳在透明物体上必然被透出来（描边在 Opaque 队列、玻璃在 Transparent 队列后画）→ 改玻璃着色器的菲涅尔辉光 |
| 搜索框点不进 | `Label()` 建的 Text 默认 `raycastTarget = true`，盖住了输入框 |
| 按键到不了程序 | `SendKeys` / `keybd_event` 都不行 → `SendInput` 的 Unicode 路径 + `ForceForeground` |
| 选单关不掉 / 连线点不到 | `MkNode` 的 `alpha > 0.001f` 判定，写 `0.001f` 时不建 Image → 没有 raycast 目标（三处各咬一次） |
| 连线热区点不到 | `e.Hit` 赋值后从没被用过，热区一直停在父节点原点的默认 100×100 |
| 右键菜单在拖动后打不开 | `_moved` 拖过之后不复位 |
| 15 步计算器教程没有任何代码触发 | 注释写着「首次打开计算器时触发」，但没有任何代码实现；`FinishCalc()` 也从没被调用 |
| 手动输入对话框调不到 | `AskManualValue()` 定义了但没有任何调用点 |
| 求解对话框按钮在面板外 | `MkBtn` 硬编码锚点 (1,1) |
| 计算器曲线不实时 | `NodeDrag.OnDrag` 少了 `scene.NodeMoved()` |
| 标注显示 `0C` | 示例场景把 `AddNode(pos, coef, formula)` 的参数传反，`TryParse` 失败把 out 置 0 |
| 粒子不跟器皿 | `pos(i,t) - origin`：`pos` 返回的已经是局部坐标，又减了一次世界坐标 |
| 方程标签用快照位置 | 用的是 `Ev.Pos`（反应发生那一刻），改成读容器当前位置 |
| 计算器打开时实验台还能点 | EventSystem 挂在 LabUI 下，`SetActive(false)` 把它一起禁用了 |

## 检查手段

| 手段 | 位置 |
|---|---|
| 窗口级 UI 自动化 | `tools/ui.ps1` + `tools/Win32UI.cs` |
| 三套窗口测试 | `uitest.ps1` / `calctest.ps1` / `pettest.ps1` |
| 控件坐标自报 | 实验台 `-dumpLayout`（`AUDITSTART`/`AUDITEND` 包起来）；计算器 `-calcDemo` |
| 状态自报 | `[CalcState]` `[TagAudit]` `[PetState]`（含 `PET\|glow`、`STATE\|vessel`）`[ReagentState]` `[SolveResult]` `[MenuDbg]` `[ClickDbg]` `[KeyDbg]` `[SearchDbg]` `STATE\|dlg` |
| 调试开关 | `-calcDemo` `-hessDemo` `-focusDemo` `-tagAudit` `-petAudit` `-calcTut` `-noTut` `-dlgDemo` `-dumpLayout` |

### 踩过的坑（16 条）
1. 按窗口标题找窗口会匹配到浏览器 → 按**进程**找
2. `CopyFromScreen` 抓屏幕最上层 → 用 **`PrintWindow`** 并裁掉边框
3. PowerShell 进程 DPI 不感知 → 坐标被虚拟化，必须调 `SetProcessDpiAwareness`
4. **键盘注入**：用 **`SendInput` 的 Unicode 路径**
5. **抢前台**：先 `AttachThreadInput` 再抢，单靠 `SetForegroundWindow` 会静默失败
6. 启动后第一次点击被 Windows 拿走 → 加**预热点击**（且要在拿到窗口句柄之后调）
7. 游戏每 1.2 秒才打一次状态 → 点完要等刷新再读
8. 右键在「拉线中」是**取消拉线**（设计如此）
9. 连完一条线会弹「焓变从哪来」选单，会挡住后续点击
10. **首次打开计算器的自动教程会 `ClearAll()`** → 测试用 `-noTut`
11. 控件坐标**只能取最后一块审计** —— 审计每行都以 `[CalcAudit]` 开头，`LastIndexOf` 找到的是最后一行
12. 器皿坐标列表里可能混着已在面板下面、点不中的 → 要逐个试
13. PowerShell 里 `$T` 这个名字有问题，换 `$Coords`
14. `.ps1` 必须存成 **UTF-8 with BOM**
15. PowerShell 的 `.Replace()` 是 .NET 单字符版 → 写 Node 脚本改文件
16. 补丁脚本报「成功」不等于写进去了 —— 每次都要**当场打印改动处核对**
