# ReactionRule 设计建议

## 无机通用规则
1. Acid + Base -> Salt + H2O
2. Acid + Carbonate -> Salt + CO2 + H2O
3. Reactive Metal + Acid -> Salt + H2
4. Group 2 carbonate --heat--> oxide + CO2
5. Group 2 nitrate --heat--> oxide + NO2 + O2
6. Halogen displacement: stronger halogen oxidises lower halide
7. Ag+ + halide -> AgX(s)
8. Ba2+ + SO4^2- -> BaSO4(s)
9. NH4+ + OH- + heat -> NH3 + H2O
10. Transition-metal ion + OH-/NH3 -> hydroxide/complex according to rule set
11. Ligand exchange controlled by ligand identity, concentration and equilibrium
12. Redox reactions should use oxidation-state/electron-balance rules where possible

## AS 有机通用规则
PrimaryAlcohol + [O] + Distillation -> Aldehyde
PrimaryAlcohol + [O] + Reflux -> CarboxylicAcid
SecondaryAlcohol + [O] -> Ketone
Aldehyde + [O] -> CarboxylicAcid
Carbonyl + NaBH4 -> Alcohol
Alkene + H2/Ni -> Alkane
Alkene + Br2 -> VicinalDibromide
Alkene + HX -> Halogenoalkane
Alkene + Steam/H3PO4 -> Alcohol
Halogenoalkane + OH-(aq) -> Alcohol
Halogenoalkane + OH-(ethanol) -> Alkene
Halogenoalkane + CN-(ethanol) -> Nitrile
Halogenoalkane + NH3(ethanol) -> Amine
Carbonyl + HCN/KCN -> Hydroxynitrile
CarboxylicAcid + Alcohol + H+ <-> Ester + H2O

## A2 有机通用规则
Benzene + NO2+ -> Nitroarene
Benzene + X+ -> Halogenoarene
Benzene + R+ -> Alkylbenzene
Benzene + RCO+ -> Acylbenzene
Nitroarene + Reduction -> Arylamine
Arylamine + HNO2/HCl (cold) -> DiazoniumSalt
DiazoniumSalt + Phenol/Phenoxide -> AzoCompound
AcylChloride + H2O -> CarboxylicAcid + HCl
AcylChloride + Alcohol -> Ester + HCl
AcylChloride + NH3/Amine -> Amide + HCl / ammonium salt
DioylCompound + Diol -> Polyester
DioylCompound + Diamine -> Polyamide
AminoAcid + AminoAcid -> Peptide + H2O

## Observation 字段建议
- gasProduced
- precipitateProduced
- precipitateColour
- solutionColourBefore
- solutionColourAfter
- effervescence
- temperatureChange
- dissolvesInExcess
- smell (建议仅作为文本，不作为交互要求)
