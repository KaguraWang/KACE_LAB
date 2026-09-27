# CIE 9701 AS + A2 Unity 化学反应仿真参考包

用途：给 Unity 化学反应仿真项目集中参考。

建议架构：
- ChemicalSpecies：描述化学物种本身
- ReactionRule：描述通用反应规则
- Condition：温度、溶剂、催化剂、加热/回流/蒸馏等条件
- Observation：颜色变化、沉淀、气体、溶解等实验现象

重要说明：
1. 这不是“所有可能在题目里出现的分子”的数学全集，因为 CIE 9701 会给陌生有机物和新实验情境。
2. 因此应采用“核心物种库 + 通用反应规则”，不要给每一个具体有机分子单独硬编码反应。
3. 建议至少支持 H–Kr 的元素信息，以及 AS/A2 常见离子、试剂、沉淀、配合物和有机官能团。
4. 本包基于当前对话整理，适合作为项目设计参考；正式考试范围仍以 Cambridge International 9701 syllabus 为准。

推荐文件：
- chemicals_core.csv：核心物种
- qualitative_analysis.csv：定性分析离子/现象
- organic_families.md：AS/A2 有机体系
- reaction_rules.md：建议的通用反应规则
- unity_schema.md：Unity 数据结构建议
- sources.md：参考来源
