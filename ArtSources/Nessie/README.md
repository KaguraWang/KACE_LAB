# 指定小水怪模型

用户于 2026-09-28 指定 `D:/downloads/小水怪_已贴图.blend` 为项目以后使用的“小水怪模型”。不要用旧版 `小水怪.blend` 或自动生成替代模型。

- `小水怪_已贴图.blend`：原件的逐字节副本，保留不修改。
- `LittleNessie_Packed.blend`：将材质使用的外部贴图打包后的工作副本。
- `../../Assets/Art/Nessie/LittleNessie.prefab`：项目可用预制件。
- `../../Assets/Art/Nessie/Nessie.fbx`：派生网格，排除拍摄地面，保留身体、眼睛和绒毛。
- `../../Assets/Art/Nessie/Textures`：八张原始材质贴图。

导出脚本为 `tools/export_nessie.py`，使用本机 Blender 后台运行；`ChemLab.Editor.NessieAssetTools.Prepare` 配置贴图并生成预制件。不自动放入实验场景，不改变现有 AI 助手。
