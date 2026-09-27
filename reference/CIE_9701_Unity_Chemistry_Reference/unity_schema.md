# Unity 数据结构建议

```csharp
[System.Serializable]
public class ChemicalSpecies
{
    public string id;
    public string displayName;
    public string formula;

    public Phase phase;
    public string colour;
    public float charge;

    public List<string> tags;
    public List<string> functionalGroups;
    public List<int> oxidationStates;

    public bool isAcid;
    public bool isBase;
    public bool isOxidisingAgent;
    public bool isReducingAgent;

    public string aqueousColour;
    public string precipitateColour;

    public float meltingPoint;
    public float boilingPoint;
}

[System.Serializable]
public class ReactionRule
{
    public List<ReactantPattern> reactants;
    public List<string> requiredTags;

    public string solvent;
    public string catalyst;
    public float minTemperature;
    public float maxTemperature;

    public bool requiresHeating;
    public bool requiresReflux;
    public bool requiresDistillation;

    public List<ProductDefinition> products;
    public Observation observation;
}

[System.Serializable]
public class Observation
{
    public string colourChange;
    public string precipitate;
    public string precipitateColour;
    public string gas;
    public bool effervescence;
    public bool dissolvesInExcess;
    public string note;
}
```

## 推荐 ID 规则
- species.h2
- species.hcl_aq
- ion.cu2_aq
- precipitate.agcl_s
- organic.ethanol
- complex.cu_nh3_4_h2o_2
- reagent.tollens

## 状态要区分
不要把 HCl(g)、HCl(aq)、Cl-(aq) 当成同一个实体。
建议：
- chemical identity
- physical state
- solution speciation
分开存。
