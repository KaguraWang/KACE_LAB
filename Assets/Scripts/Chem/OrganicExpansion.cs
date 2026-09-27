namespace ChemLab.Chem {
// Local source-controlled additions are separate from generated legacy tables.
public static partial class ChemData {
    // Hydrogenation: one H2 per C=C, nickel is a catalyst, never consumed.
    // Teaching setup: Ni at ~150 C; no kinetic/equilibrium rate claim.
    // https://www.chemguide.co.uk/organicprops/alkenes/hydrogenation.html
    // https://openstax.org/books/organic-chemistry/pages/8-6-reduction-of-alkenes-hydrogenation
    public static void AddExpansionRules() {
        string[] substrates={"but_1_ene","pent_1_ene","hex_1_ene","hept_1_ene","oct_1_ene"};
        string[] products={"c4h10","n_pentane","n_hexane","n_heptane","n_octane"};
        for(int i=0;i<substrates.Length;i++){
            string from=substrates[i],to=products[i];
            var rule=new Rules.Rule{Eq=Sub[from].Formula+" + H2 -> "+Sub[to].Formula,
                Cond="nickel_hydrogenation",Obs=Sub[from].Name+"在镍催化加热条件下与氢气加成，生成"+Sub[to].Name,
                Organic=true,Parsed=true};
            // Use structure IDs: C6H12 alone also identifies cyclohexane.
            rule.Reactants.Add(new Rules.Species{Id=from,Coef=1,Raw=Sub[from].Formula});
            rule.Reactants.Add(new Rules.Species{Id="h2",Coef=1,Raw="H2"});
            rule.Products.Add(new Rules.Species{Id=to,Coef=1,Raw=Sub[to].Formula});
            Rules.All.Add(rule);
        }
        string[] alcohols={"butan_1_ol","pentan_1_ol","hexan_1_ol","heptan_1_ol","octan_1_ol"};
        string[] aldehydes={"butanal","pentanal","hexanal","heptanal","octanal"};
        string[] acids={"butanoic_acid","pentanoic_acid","hexanoic_acid","heptanoic_acid","octanoic_acid"};
        for(int i=0;i<5;i++){
            Oxidation(alcohols[i],aldehydes[i],1,7,"organic_cr_distil");
            Oxidation(alcohols[i],acids[i],2,11,"organic_cr_reflux");
            Oxidation(aldehydes[i],acids[i],1,4,"organic_cr_reflux");
        }
    }
    // Acidified dichromate half-equation: Cr2O7^2- + 14H+ + 6e- -> 2Cr3+ + 7H2O.
    // Combined stoichiometry, not the legacy unbalanced substrate -> product shortcut.
    // https://www.chemguide.co.uk/organicprops/alcohols/oxidation.html
    static void Oxidation(string from,string to,int dichromate,int water,string condition){
        var r=new Rules.Rule{Cond=condition,Organic=true,Parsed=true,
            Obs=Sub[from].Name+"被酸性重铬酸盐氧化，生成"+Sub[to].Name+"，铬(VI)还原为铬(III)"};
        Rules.Species S(string id,int n)=>new Rules.Species{Id=id,Coef=n,Raw=Sub[id].Formula};
        r.Reactants.Add(S(from,3));r.Reactants.Add(S("Cr2O7^2-",dichromate));r.Reactants.Add(S("H+",8*dichromate));
        r.Products.Add(S(to,3));r.Products.Add(S("Cr3+",2*dichromate));r.Products.Add(S("h2o",water));
        r.Eq="3"+Sub[from].Formula+" + "+dichromate+"Cr2O7^2- + "+(8*dichromate)+"H+ -> 3"+Sub[to].Formula+" + "+(2*dichromate)+"Cr3+ + "+water+"H2O";
        Rules.All.Add(r);
    }
    const string TblOrganicExpansion = @"
nabo2	偏硼酸钠	NaBO2	aq			1	k	other	硼氢化钠还原羰基后的教学净反应产物
butanoic_acid	丁酸	C4H8O2	l			0.964	s	acid	直链羧酸；NIST相变及Supelco密度；酸碱平衡与反应覆盖待审计
pentanoic_acid	戊酸	C5H10O2	l			0.94	s	acid	直链羧酸；ICSC 0346；酸碱平衡与反应覆盖待审计
hexanoic_acid	己酸	C6H12O2	l			0.93	s	acid	直链羧酸；ICSC 1167；酸碱平衡与反应覆盖待审计
heptanoic_acid	庚酸	C7H14O2	l			0.91	s	acid	直链羧酸；Thermo AC164170010；酸碱平衡与反应覆盖待审计
octanoic_acid	辛酸	C8H16O2	l			0.91	s	acid	直链羧酸；Sigma O3907；酸碱平衡与反应覆盖待审计
butanal	丁醛	C4H8O	l			0.801	s	organic	直链醛；Wako密度范围中点；定量互溶及反应覆盖待审计
pentanal	戊醛	C5H10O	l			0.8	s	organic	直链醛；ICSC 1417；定量互溶及反应覆盖待审计
hexanal	己醛	C6H12O	l			0.815	s	organic	直链醛；Wako密度范围中点；定量互溶及反应覆盖待审计
heptanal	庚醛	C7H14O	l			0.820	s	organic	直链醛；Thermo SDS 2025；定量互溶及反应覆盖待审计
octanal	辛醛	C8H16O	l			0.82	s	organic	直链醛；相变NIST、密度Merck；定量互溶及反应覆盖待审计
but_1_ene	丁-1-烯	C4H8	g			0.002293	n	organic	直链烯烃；Sigma 744042；气体密度按25℃一大气压PV=nRT近似；反应覆盖待审计
pent_1_ene	戊-1-烯	C5H10	l			0.64	n	organic	直链烯烃；Thermo AC268661000；反应覆盖待审计
hex_1_ene	己-1-烯	C6H12	l			0.66848	n	organic	直链烯烃；Chevron Phillips 2019；25℃密度；反应覆盖待审计
hept_1_ene	庚-1-烯	C7H14	l			0.697	n	organic	直链烯烃；Thermo AC120390250；反应覆盖待审计
oct_1_ene	辛-1-烯	C8H16	l			0.71085	n	organic	直链烯烃；Chevron Phillips 2019；25℃密度；反应覆盖待审计
n_pentane	正戊烷	C5H12	l			0.63	n	organic	直链烷烃；物性来源 ICSC 0534；相平衡及反应覆盖仍在审计
n_hexane	正己烷	C6H14	l			0.66	n	organic	直链烷烃；物性来源 IPCS EHC 122；相平衡及反应覆盖仍在审计
n_heptane	正庚烷	C7H16	l			0.68	n	organic	直链烷烃；物性来源 ICSC 0657；相平衡及反应覆盖仍在审计
n_octane	正辛烷	C8H18	l			0.70	n	organic	直链烷烃；物性来源 ICSC 0933；相平衡及反应覆盖仍在审计
butan_1_ol	丁-1-醇	C4H10O	l			0.8	s	organic	直链伯醇；IPCS HSG 3；定量互溶及反应覆盖仍在审计
pentan_1_ol	戊-1-醇	C5H12O	l			0.8	s	organic	直链伯醇；ICSC 0535；定量互溶及反应覆盖仍在审计
hexan_1_ol	己-1-醇	C6H14O	l			0.82	s	organic	直链伯醇；ICSC 1084；定量互溶及反应覆盖仍在审计
heptan_1_ol	庚-1-醇	C7H16O	l			0.822	s	organic	直链伯醇；密度 Merck 820624；熔沸点 Wako；定量互溶及反应覆盖仍在审计
octan_1_ol	辛-1-醇	C8H18O	l			0.83	s	organic	直链伯醇；ICSC 1030；定量互溶及反应覆盖仍在审计
";
}
}
