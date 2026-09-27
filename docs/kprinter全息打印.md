# kprinter 全息打印

控制台输入 `kprinter 1` 启用便携式建造装置，默认开启；输入 `kprinter 0` 恢复仪器架原来的即时生成和自动选中方式。只接受 `0` 或 `1`，非法参数保留原状态。设定按本次游戏进程生效，重新启动默认回到 `1`。

打印区固定在世界坐标 `(0, 0, -1.65)`，与相机位置无关。仪器架的十种单件仪器，以及蒸馏、回流套件均从同一区域打印。深色便携投影器旁的蓝青色定位圈启动后，全息仪器立即出现，表面细网格、蓝色光点和水平扫描带流动；原有玻璃、金属、塑料、刻度和商标材质从下往上显现。最后一道蓝光再次由下向上扫过，全息轮廓闪烁消散，成品留在原位。全程约 2.25 秒。

生成时相机会一次性构图，将整件仪器和投影器放在两侧仪器架、试剂架及底部记录栏之间；玩家仍可自由移动相机。打印期间临时收起悬浮物质及反应标签，完成后恢复。打印使用真实帧时间，`world_speed 0` 不会卡住建造过程。

打印尚未完成时禁止拾取、选中、作为吸附或默认倾倒目标；完成时恢复碰撞体、原材质属性及自动选中。再次点击会提示正在打印。打印区被仪器或检验道具占用时拒绝生成，并在提示栏及实验记录中说明先挪开现有仪器。判断依据是当前世界位置和仪器几何占地；移开或删除仪器即释放区域，蒸馏套件还检查接收瓶及冷凝器占用的空间。

打印过程中设为 `kprinter 0`，当前打印正常完成，后续点击立即使用原来的生成方式。

## 验证

Windows 隐藏播放器验收入口是 `-kprinterTest -windowTargetInput -noTut`。所有输入事件由测试进程内部注入 `WindowTargetInput`，不操控桌面鼠标、键盘，也不激活窗口。验收覆盖注册命令、非法参数、固定坐标、打印中锁定、忙碌拒绝、占位拒绝、成品拖动后再次打印、删除、切换开关、冻结实验时间、十种单件仪器、两种套件和投影资源清理。

```powershell
$root = 'D:\ChemLabUnity-vfx'
Start-Process -FilePath "$root\Build\ChemLab.exe" -WindowStyle Hidden -WorkingDirectory $root -ArgumentList "-batchmode -kprinterTest -windowTargetInput -noTut -noDefaultSolidDynamics -screen-width 1600 -screen-height 1000 -logFile $root\Logs\kprinter-player.log"
```

报告：`Logs/kprinter-audit.txt`。视觉检查截图：`Logs/window-target-kprinter-mid.png`、`Logs/window-target-kprinter-sweep.png`、`Logs/window-target-kprinter-finished.png`、`Logs/window-target-kprinter-burette.png`、`Logs/window-target-kprinter-kit-reflux.png`、`Logs/window-target-kprinter-kit-distillation.png`。

2026-09-28 验证结果：Windows 构建成功，零构建错误；隐藏播放器验收 68 项通过、0 项失败，进程正常退出，退出码为 0。已检查打印中、收尾扫光、成品、滴定管及套件截图。成品移开使用实际指针事件经进程内部注入完成，接收瓶区域占用另有拒绝断言。
