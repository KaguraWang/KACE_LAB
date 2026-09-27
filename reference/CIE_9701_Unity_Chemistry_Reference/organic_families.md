# AS / A2 有机体系

## AS 官能团
- alkane
- alkene
- halogenoalkane
- alcohol
- aldehyde
- ketone
- carboxylic acid
- ester
- primary amine
- nitrile
- hydroxynitrile

## A2 扩展
- benzene / arene
- phenol
- aromatic amine
- nitro compound
- diazonium salt
- azo compound
- acyl chloride
- amide
- amino acid / zwitterion / peptide
- polyester
- polyamide

## 建模建议
不要把所有 C1–C6 异构体逐一写死。
推荐把每个有机物拆成：
- carbonSkeleton
- functionalGroups[]
- positions[]
- stereochemistry
- molecularFormula
- structuralFormula / SMILES-like internal representation

例如：
- primary alcohol + [O] + distillation -> aldehyde
- primary alcohol + [O] + reflux -> carboxylic acid
- secondary alcohol + [O] -> ketone
- aldehyde + Tollens -> carboxylate/acid + Ag
- carbonyl + NaBH4 -> alcohol
- halogenoalkane + aqueous OH- -> alcohol
- halogenoalkane + ethanolic OH- -> alkene
- halogenoalkane + CN- -> nitrile
- halogenoalkane + NH3 -> amine
- alkene + Br2 -> dibromoalkane
- alkene + steam/H3PO4 -> alcohol
- benzene + conc HNO3/conc H2SO4 -> nitrobenzene
- benzene + halogen/AlX3 -> halogenoarene
- acyl chloride + H2O -> carboxylic acid
- acyl chloride + alcohol -> ester
- acyl chloride + NH3/amine -> amide
