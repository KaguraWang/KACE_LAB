/* =====================================================================
   化学数据（自动生成）
   208 种物质 / 53 个离子 / 电离表 / 溶解度 / 沉淀 / 焰色
   —— 本文件由 tools/gen_cs.js 从 D:\ChemLab\js\*.js 自动生成，请勿手改
   ===================================================================== */

using System.Collections.Generic;

namespace ChemLab.Chem {

public static partial class ChemData {

/* ---------- 物质：id 名 式 态 固色 液色 密度 溶解性 类别 备注 ---------- */
public const string TblSubstances = @"
na	钠	Na	s	#d8d8d8		0.97	r	metal	极活泼，遇水剧烈反应并熔成小球，煤油保存
k	钾	K	s	#d8d8d8		0.86	r	metal	遇水剧烈反应甚至燃烧，紫色焰色
mg	镁	Mg	s	#c8c8c8		1.74	r	metal	燃烧发出耀眼白光
al	铝	Al	s	#c0c4c8		2.7	r	metal	两性金属，表面致密氧化膜
zn	锌	Zn	s	#b8bcc0		7.14	r	metal	与稀酸反应放出氢气
fe	铁	Fe	s	#8a8a8a		7.87	r	metal	常见还原剂，铁粉为黑色
cu	铜	Cu	s	#b5651d		8.96	-	metal	紫红色，不与非氧化性酸反应
ag	银	Ag	s	#e8e8ec		10.49	-	metal	
pb	铅	Pb	s	#7a7a86		11.34	-	metal	重金属，有毒
sn	锡	Sn	s	#c0c0c8		7.31	-	metal	
ca	钙	Ca	s	#d0d0d0		1.55	r	metal	遇水反应放氢，砖红色焰色
ba	钡	Ba	s	#cccccc		3.51	r	metal	有毒，黄绿色焰色
ni	镍	Ni	s	#b0b4b8		8.9	-	metal	
hg	汞	Hg	l	#c0c0c8		13.53	-	metal	常温液态金属，剧毒，易挥发
au	金	Au	s	#ffd24a		19.3	-	metal	化学性质极稳定
pt	铂	Pt	s	#e0e0e4		21.45	-	metal	催化剂
cuo_powder	氧化铜粉	CuO	s	#151515		6.31	n	oxide	黑色粉末
c	碳	C	s	#1a1a1a		2.25	-	nonmetal	石墨，导电，还原剂
s	硫	S	s	#e8d84a		2.07	-	nonmetal	淡黄色，燃烧呈蓝紫色火焰
p_red	红磷	P	s	#8a2a1a		2.34	-	nonmetal	
p_white	白磷	P	s	#f0e8c0		1.82	-	nonmetal	剧毒，自燃，必须水中保存
i2	碘	I2	s	#2a1a4a	#8a5a2a	4.93	s	nonmetal	升华产生紫色蒸气，遇淀粉变蓝
br2	溴	Br2	l	#6a1a10	#a0341a	3.12	s	nonmetal	深红棕色液体，易挥发，强腐蚀
cl2	氯气	Cl2	g	#c8e04a		0.0032	-	nonmetal	黄绿色，刺激性气味，有毒
h2	氢气	H2	g			0.00009	-	nonmetal	无色无味，可燃，点燃有爆鸣声
o2	氧气	O2	g			0.0014	-	nonmetal	无色无味，助燃
n2	氮气	N2	g			0.00125	-	nonmetal	化学性质稳定
na2o	氧化钠	Na2O	s	#f0f0f0		2.27	r	oxide	遇水生成氢氧化钠
cao	氧化钙	CaO	s	#f5f5f5		3.34	r	oxide	生石灰，遇水放出大量热
mgo	氧化镁	MgO	s	#f8f8f8		3.58	n	oxide	白色疏松粉末
cuo	氧化铜	CuO	s	#151515		6.31	n	oxide	黑色粉末
fe2o3	氧化铁	Fe2O3	s	#8a2a12		5.24	n	oxide	红棕色，铁锈主要成分
fe3o4	四氧化三铁	Fe3O4	s	#151515		5.17	n	oxide	黑色，有磁性
al2o3	氧化铝	Al2O3	s	#f0f0f0		3.95	n	oxide	两性氧化物
zno	氧化锌	ZnO	s	#f8f8f8		5.61	n	oxide	两性氧化物
mno2	二氧化锰	MnO2	s	#151515		5.03	n	oxide	催化剂，也是氧化剂
sio2	二氧化硅	SiO2	s	#e8e8e8		2.65	n	oxide	沙子，能与氢氟酸反应
so2	二氧化硫	SO2	g			0.0029	-	oxide	无色有刺激性气味，有漂白性
co2	二氧化碳	CO2	g			0.00198	-	oxide	无色无味，使澄清石灰水变浑浊
co	一氧化碳	CO	g			0.00125	-	oxide	无色无味，剧毒，可燃
no2	二氧化氮	NO2	g	#a0341a		0.0033	-	oxide	红棕色，有刺激性气味，有毒
no	一氧化氮	NO	g			0.00134	-	oxide	无色，遇空气立即变红棕色
p2o5	五氧化二磷	P2O5	s	#f8f8f8		2.39	r	oxide	白色固体，强吸水性
cr2o3	三氧化二铬	Cr2O3	s	#2a5a2a		5.22	n	oxide	绿色颜料
pbo	氧化铅	PbO	s	#e8d84a		9.53	n	oxide	黄色，有毒
hcl	稀盐酸	HCl	aq		#eef4f8	1.05	k	acid	强酸，易挥发，有刺激性气味
hcl_conc	浓盐酸	HCl	aq		#eef4f8	1.18	k	acid	浓盐酸，瓶口有白雾
h2so4	稀硫酸	H2SO4	aq		#eef4f8	1.2	k	acid	强酸，不挥发
h2so4_conc	浓硫酸	H2SO4	l		#f2f0e8	1.84	k	acid	强吸水性和脱水性，稀释时剧烈放热
hno3	稀硝酸	HNO3	aq		#eef4f8	1.15	k	acid	具有强氧化性
hno3_conc	浓硝酸	HNO3	l		#f4f0dc	1.51	k	acid	浓硝酸，见光分解，瓶口有白雾
h3po4	磷酸	H3PO4	aq		#eef4f8	1.69	k	acid	中强酸，不挥发
h2co3	碳酸	H2CO3	aq		#eef4f8	1	k	acid	弱酸，极不稳定易分解
ch3cooh	乙酸	CH3COOH	aq		#eef4f8	1.05	k	acid	弱酸，有刺激性酸味
ch3cooh_glacial	冰醋酸	CH3COOH	l		#eef4f8	1.05	k	acid	无水乙酸，17℃以下凝结成冰状晶体
h2s	氢硫酸	H2S	aq		#eef4f8	1	k	acid	有臭鸡蛋气味，具有还原性
hbr	氢溴酸	HBr	aq		#eef4f8	1.49	k	acid	强酸
hi	氢碘酸	HI	aq		#eef4f8	1.7	k	acid	强酸，有强还原性
h2c2o4	草酸	H2C2O4	aq		#eef4f8	1.65	k	acid	二元弱酸，有还原性
hf	氢氟酸	HF	aq		#eef4f8	1.15	k	acid	弱酸但腐蚀玻璃，剧毒
hclo	次氯酸	HClO	aq		#eef4f8	1	k	acid	弱酸，强氧化性，有漂白作用
naoh	氢氧化钠	NaOH	s	#f8f8f8	#eef4f8	2.13	k	base	强碱，易潮解，腐蚀性强
koh	氢氧化钾	KOH	s	#f8f8f8	#eef4f8	2.04	k	base	强碱，易潮解
caoh2	氢氧化钙	Ca(OH)2	s	#f8f8f8	#eef4f8	2.21	s	base	微溶，澄清溶液即石灰水
baoh2	氢氧化钡	Ba(OH)2	s	#f8f8f8	#eef4f8	3.74	k	base	可溶强碱，有毒
nh3	氨水	NH3	aq		#eef4f8	0.91	k	base	有刺激性气味，弱碱，易挥发
cuoh2	氢氧化铜	Cu(OH)2	s	#3a8ec8		3.37	n	base	蓝色絮状沉淀
feoh3	氢氧化铁	Fe(OH)3	s	#8a3a12		3.4	n	base	红褐色絮状沉淀
feoh2	氢氧化亚铁	Fe(OH)2	s	#d8e8d8		3.4	n	base	白色沉淀，迅速变灰绿最终变红褐
aloh3	氢氧化铝	Al(OH)3	s	#f8f8f8		2.42	n	base	白色胶状沉淀，两性
mgoh2	氢氧化镁	Mg(OH)2	s	#f8f8f8		2.34	n	base	白色沉淀
znoh2	氢氧化锌	Zn(OH)2	s	#f8f8f8		3.05	n	base	白色沉淀，两性
nacl	氯化钠	NaCl	s	#f8f8f8	#eef4f8	2.16	k	salt	食盐，焰色黄色
kcl	氯化钾	KCl	s	#f8f8f8	#eef4f8	1.98	k	salt	焰色紫色（透过蓝色钴玻璃）
cacl2	氯化钙	CaCl2	s	#f8f8f8	#eef4f8	2.15	k	salt	常用干燥剂
bacl2	氯化钡	BaCl2	s	#f8f8f8	#eef4f8	3.86	k	salt	有毒，用于检验硫酸根
mgcl2	氯化镁	MgCl2	s	#f8f8f8	#eef4f8	2.32	k	salt	
alcl3	氯化铝	AlCl3	s	#f0f0f0	#eef4f8	2.48	k	salt	易水解
fecl2	氯化亚铁	FeCl2	s	#a8c8a8	#a8d8b0	3.16	k	salt	浅绿色溶液
fecl3	氯化铁	FeCl3	s	#8a4a1a	#c08a3a	2.9	k	salt	棕黄色溶液
cucl2	氯化铜	CuCl2	s	#3a7a5a	#3ab8a0	3.39	k	salt	蓝绿色溶液
zncl2	氯化锌	ZnCl2	s	#f8f8f8	#eef4f8	2.91	k	salt	
agno3	硝酸银	AgNO3	s	#f8f8f8	#eef4f8	4.35	k	salt	见光分解，检验氯离子
kno3	硝酸钾	KNO3	s	#f8f8f8	#eef4f8	2.11	k	salt	氧化剂，焰色紫色
nano3	硝酸钠	NaNO3	s	#f8f8f8	#eef4f8	2.26	k	salt	焰色黄色
pbno32	硝酸铅	Pb(NO3)2	s	#f8f8f8	#eef4f8	4.53	k	salt	有毒
cuno32	硝酸铜	Cu(NO3)2	s	#2a6a8a	#2a8ac8	2.32	k	salt	蓝色溶液
na2co3	碳酸钠	Na2CO3	s	#f8f8f8	#eef4f8	2.54	k	salt	纯碱，水解显碱性
nahco3	碳酸氢钠	NaHCO3	s	#f8f8f8	#eef4f8	2.2	k	salt	小苏打，受热分解放二氧化碳
k2co3	碳酸钾	K2CO3	s	#f8f8f8	#eef4f8	2.43	k	salt	
caco3	碳酸钙	CaCO3	s	#f8f8f8		2.71	n	salt	大理石、石灰石，不溶于水
baco3	碳酸钡	BaCO3	s	#f8f8f8		4.29	n	salt	白色沉淀，可溶于酸
na2so4	硫酸钠	Na2SO4	s	#f8f8f8	#eef4f8	2.66	k	salt	
cuso4	硫酸铜	CuSO4	s	#e8e8e8	#2a8ac8	3.6	k	salt	白色粉末，溶于水显蓝色
cuso45h2o	胆矾	CuSO4·5H2O	s	#1f6bc8	#2a8ac8	2.28	k	salt	蓝色晶体，加热失水变白
feso4	硫酸亚铁	FeSO4	s	#a8c8a8	#a8d8b0	2.84	k	salt	浅绿色，易被氧化变黄
fe2so43	硫酸铁	Fe2(SO4)3	s	#c8b878	#c8a850	3.1	k	salt	黄色溶液
znso4	硫酸锌	ZnSO4	s	#f8f8f8	#eef4f8	3.54	k	salt	
mgso4	硫酸镁	MgSO4	s	#f8f8f8	#eef4f8	2.66	k	salt	泻盐
baso4	硫酸钡	BaSO4	s	#f8f8f8		4.49	n	salt	白色沉淀，不溶于稀酸，钡餐
pbso4	硫酸铅	PbSO4	s	#f8f8f8		6.29	n	salt	白色沉淀
caso4	硫酸钙	CaSO4	s	#f8f8f8		2.96	s	salt	微溶，石膏
ki	碘化钾	KI	s	#f8f8f8	#eef4f8	3.13	k	salt	
kbr	溴化钾	KBr	s	#f8f8f8	#eef4f8	2.75	k	salt	
kmno4	高锰酸钾	KMnO4	s	#3a1a3a	#a02a6a	2.7	k	salt	紫黑色，强氧化剂，溶液紫红色
k2cr2o7	重铬酸钾	K2Cr2O7	s	#d84a1a	#e8721a	2.68	k	salt	橙色，强氧化剂（酸性条件下）
nh4cl	氯化铵	NH4Cl	s	#f8f8f8	#eef4f8	1.53	k	salt	受热易分解，遇碱放氨
nh42so4	硫酸铵	(NH4)2SO4	s	#f8f8f8	#eef4f8	1.77	k	salt	氮肥
na3po4	磷酸钠	Na3PO4	s	#f8f8f8	#eef4f8	2.54	k	salt	水解显强碱性
na2s	硫化钠	Na2S	s	#f0f0d8	#eef4f8	1.86	k	salt	有臭味，遇酸放硫化氢
fes	硫化亚铁	FeS	s	#151515		4.84	n	salt	黑色块状，遇酸放硫化氢
cus	硫化铜	CuS	s	#151515		4.6	n	salt	黑色沉淀，不溶于稀酸
zns	硫化锌	ZnS	s	#f8f8f8		4.09	n	salt	白色沉淀
agcl	氯化银	AgCl	s	#f8f8f8		5.56	n	salt	白色沉淀，不溶于稀硝酸，感光变黑
agbr	溴化银	AgBr	s	#f0e8c8		6.47	n	salt	淡黄色沉淀，感光
agi	碘化银	AgI	s	#e8d84a		5.67	n	salt	黄色沉淀，用于人工降雨
pbi2	碘化铅	PbI2	s	#e8c81a		6.16	n	salt	金黄色沉淀，溶于热水
cuco3	碱式碳酸铜	Cu2(OH)2CO3	s	#2f8a5a		3.9	n	salt	铜绿，孔雀石主要成分
na2sio3	硅酸钠	Na2SiO3	s	#f8f8f8	#eef4f8	2.4	k	salt	水玻璃，矿物胶
naclo	次氯酸钠	NaClO	s	#f0f0e8	#eef4f8	1.1	k	salt	漂白液有效成分
kscn	硫氰化钾	KSCN	s	#f8f8f8	#eef4f8	1.89	k	salt	检验铁离子
fes2	二硫化亚铁	FeS2	s	#c8a83a		5	n	salt	黄铁矿，工业制硫酸原料
al2so43	硫酸铝	Al2(SO4)3	s	#f8f8f8	#eef4f8	2.71	k	salt	净水剂
nabr	溴化钠	NaBr	s	#f8f8f8	#eef4f8	3.21	k	salt	
nai	碘化钠	NaI	s	#f8f8f8	#eef4f8	3.67	k	salt	易被氧化析出碘
mgno32	硝酸镁	Mg(NO3)2	s	#f8f8f8	#eef4f8	2.3	k	salt	
nh4no3	硝酸铵	NH4NO3	s	#f8f8f8	#eef4f8	1.72	k	salt	受热分解甚至爆炸
ch4	甲烷	CH4	g			0.00072	-	organic	天然气主要成分，可燃
c2h6	乙烷	C2H6	g			0.00136	-	organic	
c2h4	乙烯	C2H4	g			0.00126	-	organic	催熟剂，能使溴水褪色
c2h2	乙炔	C2H2	g			0.00117	-	organic	氧炔焰可达 3000℃
c3h8	丙烷	C3H8	g			0.00201	-	organic	液化石油气主要成分
c4h10	丁烷	C4H10	g			0.00248	-	organic	打火机燃料
c6h6	苯	C6H6	l			0.879	n	organic	无色有特殊气味的液体，有毒，不溶于水
c7h8	甲苯	C7H8	l			0.867	n	organic	
ch3oh	甲醇	CH3OH	l			0.792	k	organic	有毒，误饮致盲，可燃
c2h5oh	乙醇	C2H5OH	l			0.789	k	organic	酒精，可燃，与钠反应放氢
c3h8o3	甘油	C3H8O3	l			1.261	k	organic	丙三醇，粘稠，吸湿
phoh	苯酚	C6H5OH	s	#f0d0c0	#e8c8b8	1.07	s	organic	有腐蚀性，遇氯化铁显紫色
ch2o	甲醛	HCHO	g		#eef4f8	0.815	k	organic	刺激性气味，水溶液即福尔马林
ch3cho	乙醛	CH3CHO	l		#eef4f8	0.788	k	organic	易挥发，有刺激性气味
acetone	丙酮	CH3COCH3	l		#eef4f8	0.791	k	organic	常见有机溶剂
hcooh	甲酸	HCOOH	l		#eef4f8	1.22	k	organic	蚁酸，有腐蚀性
c17h35cooh	硬脂酸	C17H35COOH	s	#f8f8f8		0.94	n	organic	高级脂肪酸
ch3cooc2h5	乙酸乙酯	CH3COOC2H5	l			0.902	s	organic	有果香味，微溶于水
glucose	葡萄糖	C6H12O6	s	#f8f8f8	#eef4f8	1.54	k	organic	还原性糖，可发生银镜反应
sucrose	蔗糖	C12H22O11	s	#f8f8f8	#eef4f8	1.59	k	organic	非还原性糖，水解生成葡萄糖和果糖
starch	淀粉	(C6H10O5)n	s	#f8f8f8	#eef4f8	1.5	s	organic	遇碘变蓝，水解最终生成葡萄糖
cellulose	纤维素	(C6H10O5)n	s	#f0ead8		1.5	n	organic	水解最终生成葡萄糖
urea	尿素	CO(NH2)2	s	#f8f8f8	#eef4f8	1.34	k	organic	第一个人工合成的有机物
aniline	苯胺	C6H5NH2	l	#e8e0c8	#e8e0c8	1.02	s	organic	有毒，易被氧化变深
ch3nh2	甲胺	CH3NH2	g			0.7	k	organic	有鱼腥味，碱性
chcl3	三氯甲烷	CHCl3	l			1.49	n	organic	氯仿，不溶于水，密度大于水
ccl4	四氯化碳	CCl4	l			1.594	n	organic	不燃，曾用作灭火剂
c2h5br	溴乙烷	C2H5Br	l			1.46	n	organic	密度大于水，可发生水解
ch3i	碘甲烷	CH3I	l			2.28	n	organic	
glycine	甘氨酸	NH2CH2COOH	s	#f8f8f8	#eef4f8	1.16	k	organic	最简单的氨基酸，两性
oil	植物油	(C17H33COO)3C3H5	l	#e8d84a	#e8d84a	0.92	n	organic	不溶于水，密度小于水，可发生皂化
naoh_s	氢氧化钠固体	NaOH	s	#f8f8f8		2.13	k	base	
vitc	维生素C	C6H8O6	s	#f8f8f8	#eef4f8	1.65	k	organic	强还原性，能使碘水褪色
h2o	水	H2O	l		#e6f2ff	1	k	other	溶剂
h2o2	过氧化氢	H2O2	l		#e6f2ff	1.45	k	other	双氧水，见光分解，可放出氧气
litmus	石蕊溶液		aq		#7a4fc0	1	k	indicator	酸性变红，碱性变蓝
phenolphthalein	酚酞溶液	C20H14O4	aq			1	k	indicator	遇碱变红，酸性和中性无色
methylorange	甲基橙溶液		aq		#e8a01a	1	k	indicator	酸性变红，碱性变黄
iodine_sol	碘水	I2(aq)	aq		#4a2a1a	1	k	indicator	遇淀粉变蓝
limewater	澄清石灰水	Ca(OH)2(aq)	aq		#eef4f8	1	k	other	用于检验二氧化碳
fe_metal	铁钉	Fe	s	#8a8a8a		7.87	-	metal	
sif4	四氟化硅	SiF4	g			0.0047	-	other	无色刺激性气体，遇水水解
baso3	亚硫酸钡	BaSO3	s	#f8f8f8		4.44	n	salt	白色沉淀，可溶于稀盐酸
cu2o	氧化亚铜	Cu2O	s	#a83a1a		6	n	oxide	砖红色沉淀，检验醛基
h2sio3	硅酸	H2SiO3	s	#f0f0f0		2.1	n	acid	白色胶状沉淀，加热失水成二氧化硅
kclo3	氯酸钾	KClO3	s	#f8f8f8	#eef4f8	2.32	k	salt	强氧化剂，与可燃物混合易爆
na2o2	过氧化钠	Na2O2	s	#f0e8b0		2.81	r	oxide	淡黄色固体，与水或二氧化碳反应放出氧气
cahco32	碳酸氢钙	Ca(HCO3)2	aq		#eef4f8	1	k	salt	只存在于溶液中，加热分解
al2s3	硫化铝	Al2S3	s	#6a6a5a		2.02	r	salt	遇水完全水解，放出硫化氢
fescn3	硫氰合铁	Fe(SCN)3	aq		#c01a1a	1	k	other	血红色，检验铁离子
cunh34	四氨合铜离子	[Cu(NH3)4]2+	aq		#1a3ac8	1	k	other	深蓝色，铜离子与过量氨水的特征产物
agnh32	银氨配离子	[Ag(NH3)2]+	aq		#eef4f8	1	k	other	银氨溶液，用于银镜反应
c6h6o6	脱氢抗坏血酸	C6H6O6	aq		#f0e8d0	1	k	organic	维生素C被氧化后的产物
c6h12o7	葡萄糖酸	C6H12O7	aq		#eef4f8	1	k	organic	葡萄糖被氧化的产物
c6h5br	溴苯	C6H5Br	l			1.495	n	organic	无色油状液体，密度大于水，有特殊气味
c6h5no2	硝基苯	C6H5NO2	l	#f0e8b0		1.204	n	organic	苦杏仁气味，淡黄色油状液体，有毒
c2h4br2	1,2-二溴乙烷	C2H4Br2	l			2.18	n	organic	无色液体，密度大于水
c2h2br4	1,1,2,2-四溴乙烷	C2H2Br4	l			2.97	n	organic	
ch3cl	一氯甲烷	CH3Cl	g			0.0023	-	organic	无色气体，光照下甲烷氯代的产物
c17h33coona	硬脂酸钠	C17H33COONa	s	#f8f8f8	#eef4f8	1	k	organic	肥皂的主要成分，水溶液显碱性
c2h5ona	乙醇钠	C2H5ONa	s	#f8f8f8		0.87	r	organic	遇水强烈水解生成乙醇和氢氧化钠
c6h2br3oh	三溴苯酚	C6H2Br3OH	s	#f8f8f8		2.9	n	organic	白色沉淀，苯酚的特征反应产物
c6h5ona	苯酚钠	C6H5ONa	s	#f8f8f8	#eef4f8	1	k	organic	溶于水显碱性
so3	三氧化硫	SO3	g			0.0029	-	oxide	无色气体，遇水生成硫酸
k2mno4	锰酸钾	K2MnO4	s	#2a6a3a	#2a8a4a	2.78	k	salt	绿色，高锰酸钾受热分解产物
pbs	硫化铅	PbS	s	#151515		7.6	n	salt	黑色沉淀
ag2s	硫化银	Ag2S	s	#151515		7.23	n	salt	黑色沉淀
fec6h5o3	苯酚铁	Fe(C6H5O)3	aq		#7a3ac8	1	k	other	紫色，苯酚与铁离子的特征产物
c6h5oh_aq	苯酚溶液	C6H5OH(aq)	aq		#f0e8e0	1	k	organic	弱酸性，能使石蕊不变色
pboh2	氢氧化铅	Pb(OH)2	s	#f8f8f8		7.41	n	base	白色沉淀，两性
mgco3	碳酸镁	MgCO3	s	#f8f8f8		2.96	n	salt	白色沉淀
znco3	碳酸锌	ZnCO3	s	#f8f8f8		4.4	n	salt	白色沉淀
feco3	碳酸亚铁	FeCO3	s	#c8d8c0		3.9	n	salt	灰白色沉淀
pbco3	碳酸铅	PbCO3	s	#f8f8f8		6.58	n	salt	白色沉淀
ag2co3	碳酸银	Ag2CO3	s	#f0e8b0		6.08	n	salt	淡黄色沉淀
caso3	亚硫酸钙	CaSO3	s	#f8f8f8		3.33	n	salt	白色沉淀
casio3	硅酸钙	CaSiO3	s	#f8f8f8		2.9	n	salt	白色沉淀
basio3	硅酸钡	BaSiO3	s	#f8f8f8		5.4	n	salt	白色沉淀
mnoh2	氢氧化锰	Mn(OH)2	s	#e8d8c8		3.26	n	base	白色沉淀，易被氧化变褐
croh3	氢氧化铬	Cr(OH)3	s	#5a8a6a		3.11	n	base	灰绿色胶状沉淀，两性
alco3	碳酸铝	Al2(CO3)3	s	#f8f8f8		2.6	r	salt	不存在于溶液中，立即水解
mns	硫化锰	MnS	s	#d8c8a8		3.99	n	salt	肉色沉淀
hgs	硫化汞	HgS	s	#1a1a1a		8.1	n	salt	黑色沉淀
pbcro4	铬酸铅	PbCrO4	s	#e8c81a		6.12	n	salt	黄色沉淀
bacro4	铬酸钡	BaCrO4	s	#e8d84a		4.5	n	salt	黄色沉淀
ag2cro4	铬酸银	Ag2CrO4	s	#a83a2a		5.63	n	salt	砖红色沉淀
hno2	亚硝酸	HNO2	aq		#eef4f8	1	k	acid	弱酸，不稳定，用于重氮化
nano2	亚硝酸钠	NaNO2	s	#f8f8f8	#eef4f8	2.17	k	salt	与酸生成亚硝酸
na2s2o3	硫代硫酸钠	Na2S2O3	s	#f8f8f8	#eef4f8	1.67	k	salt	碘量法滴定剂，遇酸析硫
k2s2o8	过二硫酸钾	K2S2O8	s	#f8f8f8	#eef4f8	2.48	k	salt	强氧化剂，碘钟反应用
pcl3	三氯化磷	PCl3	l			1.574	r	other	遇水剧烈水解
pcl5	五氯化磷	PCl5	s	#f0e8b8		2.12	r	other	氯化剂，遇水剧烈水解
socl2	亚硫酰氯	SOCl2	l			1.638	r	other	把羧酸变成酰氯，副产物全是气体
nabh4	硼氢化钠	NaBH4	s	#f8f8f8		1.07	r	other	温和还原剂，还原羰基成醇
lialh4	氢化铝锂	LiAlH4	s	#e8e8e8		0.92	r	other	强还原剂，遇水剧烈反应
hcn	氰化氢	HCN	g			0.0009	k	acid	剧毒，苦杏仁味
kcn	氰化钾	KCN	s	#f8f8f8	#eef4f8	1.52	k	salt	剧毒，提供 CN-
sicl4	四氯化硅	SiCl4	l			1.483	r	other	遇水水解生成硅酸与氯化氢
cano32	硝酸钙	Ca(NO3)2	s	#f8f8f8	#eef4f8	2.5	k	salt	受热分解
bano32	硝酸钡	Ba(NO3)2	s	#f8f8f8	#eef4f8	3.24	k	salt	受热分解
p4o10	十氧化四磷	P4O10	s	#f8f8f8		2.39	r	oxide	强干燥剂
ch3coona	乙酸钠	CH3COONa	s	#f8f8f8	#eef4f8	1.53	k	salt	水解显碱性
c2h5cl	氯乙烷	C2H5Cl	l			0.921	n	organic	卤代烃，密度小于水
c2h5i	碘乙烷	C2H5I	l			1.95	n	organic	卤代烃
ch3ch2ch2oh	丙-1-醇	CH3CH2CH2OH	l			0.803	k	organic	伯醇
ch3chohch3	丙-2-醇	CH3CHOHCH3	l			0.786	k	organic	仲醇
c2h5cho	丙醛	C2H5CHO	l		#eef4f8	0.807	k	organic	醛
ch3cooch3	乙酸甲酯	CH3COOCH3	l			0.934	s	organic	酯，有果香
c2h5nh2	乙胺	C2H5NH2	l			0.68	k	organic	伯胺，碱性
ch3cn	乙腈	CH3CN	l			0.786	k	organic	腈
c2h5cn	丙腈	C2H5CN	l			0.782	k	organic	腈
ch3ch2cooh	丙酸	CH3CH2COOH	l		#eef4f8	0.992	k	acid	羧酸
ch3conh2	乙酰胺	CH3CONH2	s	#f8f8f8	#eef4f8	1.16	k	organic	酰胺
c3h6	丙烯	C3H6	g			0.0018	-	organic	烯烃，加成反应
c6h5ch3	甲苯	C6H5CH3	l			0.867	n	organic	甲基苯，侧链可被氧化
c6h5cl	氯苯	C6H5Cl	l			1.106	n	organic	卤代芳烃
c6h5cooh	苯甲酸	C6H5COOH	s	#f8f8f8	#eef4f8	1.32	s	acid	芳香羧酸
ch3cocl	乙酰氯	CH3COCl	l			1.104	r	organic	酰氯，遇水剧烈水解
c6h5cocl	苯甲酰氯	C6H5COCl	l			1.212	r	organic	酰氯
c6h5n2cl	氯化重氮苯	C6H5N2Cl	aq		#e8e8c8	1	k	other	重氮盐，5℃以下稳定
azo_benzene	偶氮苯	C6H5N=NC6H5	s	#e8721a		1.2	n	organic	橙色偶氮染料
peptide_gly	二肽(甘氨酰甘氨酸)	NH2CH2CONHCH2COOH	s	#f8f8f8	#eef4f8	1.3	k	organic	肽键 -CONH-
[Al(OH)4]-	四羟基合铝酸根	[Al(OH)4]-	aq			1	k	ion	铝的两性产物
[CuCl4]2-	四氯合铜酸根	[CuCl4]2-	aq		#e8d84a	1	k	ion	浓盐酸中呈黄绿色
[Cu(H2O)6]2+	六水合铜离子	[Cu(H2O)6]2+	aq		#2a8ac8	1	k	ion	水合铜离子，蓝色
[Co(H2O)6]2+	六水合钴离子	[Co(H2O)6]2+	aq		#e87a9a	1	k	ion	粉红色
[CoCl4]2-	四氯合钴酸根	[CoCl4]2-	aq		#3a6ac8	1	k	ion	蓝色，浓盐酸中生成
scn-	硫氰酸根	SCN-	aq			1	k	ion	与铁离子显血红色
n2o4	四氧化二氮	N2O4	g	#f0e8d8		1	n	nonmetal	NO2 二聚的产物，无色，与红棕色 NO2 成可逆平衡
nh42co3	碳酸铵	(NH4)2CO3	s	#f8f8f8	#eef4f8	1.5	k	salt	受热分解，遇酸放出 CO2，遇碱放出 NH3
bacl22h2o	二水合氯化钡	BaCl2·2H2O	s	#f8f8f8	#eef4f8	2.44	k	salt	9701/34/M/J/26 的 FB6，受热只失结晶水
CH3CH(OH)COO-	乳酸根	CH3CH(OH)COO-	aq			0.89	k	ion	2-溴丙酸热碱水解的产物
CH3CHBrCOO-	2-溴丙酸根	CH3CHBrCOO-	aq			1.53	k	ion	2-溴丙酸的共轭碱
ch3chbrcooh	2-溴丙酸	CH3CHBrCOOH	aq		#f4f4f4	1.53	k	acid	一元溴代羧酸，9701/33/M/J/26 的 FA4
cr2so43	硫酸铬(III)	Cr2(SO4)3	s	#5a6a8a	#2f6a3f	3.92	k	salt	绿色，铬(III)盐
znco3znoh2	碱式碳酸锌	ZnCO3·Zn(OH)2	s	#f8f8f8		4.4	n	salt	9701/34/M/J/24 的 FB1，受热分解为 ZnO + CO2 + H2O
Cr(OH)4-	四羟基合铬酸根	[Cr(OH)4]-	aq		#2f6a3f	1	k	ion	铬(III)溶于过量碱的产物，深绿色
[Zn(NH3)4]2+	四氨合锌离子	[Zn(NH3)4]2+	aq			1	k	ion	锌氨配合物，溶于过量氨水
S	硫	S	s	#e8d84a		2.07	n	nonmetal	淡黄色固体
NO2-	亚硝酸根	NO2-	aq			1	k	ion	还原剂，遇酸生成氮氧化物
crcl3	氯化铬(III)	CrCl3	s	#4a6a3a	#2f6a3f	2.87	k	salt	绿色，铬(III)盐
mnso4	硫酸锰(II)	MnSO4	s	#f4f4f0	#f0f0ea	3.25	k	salt	浅粉色，锰(II)盐
na2so3	亚硫酸钠	Na2SO3	s	#f8f8f8	#eef4f8	2.63	k	salt	还原剂，遇酸放出 SO2
nh4br	溴化铵	NH4Br	s	#f8f8f8	#eef4f8	2.43	k	salt	溶于水显著吸热，P3 热化学题常用
kio3	碘酸钾	KIO3	s	#f8f8f8	#eef4f8	3.89	k	salt	碘量法基准物质
IO3-	碘酸根	IO3-	aq			1	k	ion	氧化剂，与 I- 生成 I2
S4O6^2-	连四硫酸根	S4O6^2-	aq			1	k	ion	硫代硫酸根被碘氧化的产物
na2s4o6	连四硫酸钠	Na2S4O6	aq		#eef4f8	1	k	salt	碘量滴定产物
s2o8^2-	过二硫酸根	S2O8^2-	aq			1	k	ion	强氧化剂，A2 动力学均相催化用
na2s2o8	过二硫酸钠	Na2S2O8	s	#f8f8f8	#eef4f8	2.38	k	salt	提供 S2O8^2-
pcl3	三氯化磷	PCl3	l		#eef4f8	1.37	r	other	遇水剧烈水解，制酰氯
pcl5	五氯化磷	PCl5	s	#f4f4f0		2.08	r	other	遇水剧烈水解，制酰氯
socl2	氯化亚砜	SOCl2	l			1.19	r	other	制酰氯，副产物全是气体，易提纯
poCl3	三氯氧磷	POCl3	l			1.53	r	other	PCl5 与羧酸反应的副产物
h3po3	亚磷酸	H3PO3	s	#f8f8f8		0.82	k	acid	PCl3 水解产物
bromobenzene	溴苯	C6H5Br	l		#f0e8d0	1.49	n	organic	不溶于水，密度大于水
chlorobenzene	氯苯	C6H5Cl	l			1.11	n	organic	卤代芳烃，不易亲核取代
nitrobenzene	硝基苯	C6H5NO2	l		#f4e8b0	1.2	n	organic	淡黄色油状，有苦杏仁味
methylbenzene	甲苯	C6H5CH3	l			0.87	n	organic	苯的同系物，侧链可被氧化
phenylethanone	苯乙酮	C6H5COCH3	l			1.03	n	organic	芳香酮，酰基化产物
benzoic_acid	苯甲酸	C6H5COOH	s	#f8f8f8		1.32	s	acid	芳香酸，侧链氧化的产物
cyclohexane	环己烷	C6H12	l			0.78	n	organic	苯加成的产物
benzenediazonium	苯重氮正离子	C6H5N2+	aq		#f8f8f0	1	k	ion	重氮盐，0–5 ℃ 才稳定
phenyl_ester	苯酯	ArCOOR	l			1.05	n	organic	酚与酰氯/酸酐生成的酯
phenoxide	苯酚钠	C6H5O-	aq		#f4f4f4	1	k	ion	苯酚显酸性的证据
cui	碘化亚铜	CuI	s	#f4f4e8	#e8e0c0	5.62	n	salt	白色沉淀，Cu2+ 与 I- 反应的产物
cu2o	氧化亚铜	Cu2O	s	#c04a2a		6	n	oxide	砖红色沉淀
ch3cocl	乙酰氯	CH3COCl	l			1.1	r	organic	最简单酰氯，有刺激性气味
ch3cl	氯甲烷	CH3Cl	g	#f0f0f0	#eef4f8	2.31	n	organic	气体卤代烃
chcl3	三氯甲烷	CHCl3	l			1.49	n	organic	氯仿
chi3	三碘甲烷	CHI3	s	#e8d84a		8	n	organic	碘仿，黄色沉淀，碘仿反应的产物
ch3cho	乙醛	CH3CHO	aq			0.78	k	organic	醛，可被氧化也可被还原
hcooh	甲酸	HCOOH	l			1.22	k	acid	唯一能发生银镜反应的羧酸
c2o4^2-	乙二酸根	C2O4^2-	aq			1	k	ion	草酸根，可被高锰酸根氧化
h2c2o4	乙二酸	H2C2O4	s	#f8f8f8		1.9	k	acid	草酸，二元酸
hydroxynitrile	羟基腈	RCH(OH)CN	aq			1.1	k	organic	醛酮与 HCN 加成的产物
dibromoalkane	二溴代烷	RCHBrCH2Br	l			1.8	n	organic	烯烃加溴的产物
vicinaldiol	邻二醇	RCH(OH)CH2OH	aq			1.1	k	organic	烯烃被冷稀高锰酸钾氧化的产物
dnph_hydrazone	2,4-二硝基苯腙	C6H3(NO2)2NHN=CR2	s	#e05a1a		8	n	organic	橙红色沉淀，鉴别羰基
rono2	硝酸酯	RONO2	l			1.2	n	organic	
rona	醇钠	RONa	aq			1	k	organic	醇与钠反应的产物
rch2oh	伯醇	RCH2OH	aq			1	k	organic	
srco3	碳酸锶	SrCO3	s	#f8f8f8		3.7	n	salt	受热分解生成 SrO
sro	氧化锶	SrO	s	#f8f8f8		3.5	n	oxide	
bao	氧化钡	BaO	s	#f8f8f8		5.72	n	oxide	
ca(no3)2	硝酸钙	Ca(NO3)2	s	#f8f8f8		2.5	k	salt	受热分解为 CaO + NO2 + O2
Co2+	钴(II) 离子	Co2+	aq		#e88a9a	1	k	ion	粉红色水合离子
Co(OH)2	氢氧化钴(II)	Co(OH)2	s	#4a6ac8		1	n	base	先蓝后粉红
[Co(NH3)6]2+	六氨合钴(II) 离子	[Co(NH3)6]2+	aq		#a08040	1	k	ion	黄棕色
[CoCl4]2-	四氯合钴(II) 离子	[CoCl4]2-	aq		#2a4ac8	1	k	ion	蓝色四面体配离子
c6h5ch2cl	苄基氯	C6H5CH2Cl	l			1.1	n	organic	侧链氯代产物
c6h2br3oh	2,4,6-三溴苯酚	C6H2Br3OH	s	#f8f8f0		8	n	organic	白色沉淀
c6h2br3nh2	2,4,6-三溴苯胺	C6H2Br3NH2	s	#f8f8f0		8	n	organic	白色沉淀
azo_dye	对羟基偶氮苯	C6H5N=NC6H4OH	s	#e05a1a		8	n	organic	橙红色偶氮染料
ch3cooc6h5	乙酸苯酯	CH3COOC6H5	l			1.08	n	organic	有芳香气味
aniline	苯胺	C6H5NH2	l		#f0f0d8	1.02	n	base	无色油状，易被氧化变褐
c2h5nh2	乙胺	C2H5NH2	aq			0.9	k	base	有鱼腥味的碱性气体
C2H5NH3+	乙基铵离子	C2H5NH3+	aq			1	k	ion	乙胺质子化产物
glycine	甘氨酸	H2NCH2COOH	s	#f8f8f8		1.61	k	organic	最简单的氨基酸，两性
glycinate	甘氨酸根	H2NCH2COO-	aq			1	k	ion	
glycine_h	质子化甘氨酸	[H3NCH2COOH]+	aq			1	k	ion	
gly_gly	甘氨酰甘氨酸	H2NCH2CONHCH2COOH	s	#f8f8f8		1.5	k	organic	二肽，含肽键
c6h5n2cl	氯化苯重氮盐	C6H5N2Cl	aq		#f8f8f0	1	k	salt	0–5 ℃ 稳定
n2	氮气	N2	g	#f0f0f0		1.25	n	nonmetal	无色无味
nascent_o	新生态氧	[O]	aq			1	k	other	氧化剂示意写法
cao	氧化钙	CaO	s	#f8f8f8		3.34	n	oxide	生石灰
hno2	亚硝酸	HNO2	aq			1	k	acid	重氮化反应中生成，不稳定
";

/* ---------- 离子：id 电荷 颜色 备注 ---------- */
public const string TblIons = @"
H+	1		
Na+	1		
K+	1		
NH4+	1		
C6H5NH3+	1		
Mg2+	2		
Ca2+	2		
Ba2+	2		
Sr2+	2		
Al3+	3		
Zn2+	2		
Fe2+	2	#a8d8b0	浅绿色
Fe3+	3	#c08a3a	棕黄色
Cu2+	2	#2a8ac8	蓝色
Ag+	1		
Pb2+	2		
Mn2+	2	#f0c8d8	极浅粉色
Cr3+	3	#3a6a3a	绿色
Ni2+	2	#3aa87a	绿色
Sn2+	2		
Hg2+	2		
OH-	-1		
Cl-	-1		
Br-	-1		
I-	-1		
NO3-	-1		
SO4^2-	-2		
CO3^2-	-2		
HCO3-	-1		
PO4^3-	-3		
CH3COO-	-1		
MnO4-	-1	#a02a6a	紫红色
Cr2O7^2-	-2	#e8721a	橙色
CrO4^2-	-2	#e8d84a	黄色
S2-	-2		
ClO-	-1		
SiO3^2-	-2		
SCN-	-1		
C2O4^2-	-2		
AlO2-	-1		
ZnO2^2-	-2		
HCOO-	-1		
ClO3-	-1		
S2O3^2-	-2		
F-	-1		
HSO3-	-1		
SO3^2-	-2		
C6H5O-	-1		
Li+	1		
Sr2+	2		
[Cu(NH3)4]2+	2	#1a3ac8	深蓝色
[Ag(NH3)2]+	1		
C6H6O6	0		
[Zn(NH3)4]2+	2		
";

/* ---------- 电离表：id 离子(名=系数;) 弱电解质 电离度 不电离 分子别名 ---------- */
public const string TblDissociation = @"
nh4br	NH4+=1;Br-=1	0		0	
crcl3	Cr3+=1;Cl-=3	0		0	
ch3chbrcooh	H+=1;CH3CHBrCOO-=1	0		0	
cr2so43	Cr3+=2;SO4^2-=3	0		0	
mnso4	Mn2+=1;SO4^2-=1	0		0	
na2so3	Na+=2;SO3^2-=1	0		0	
nh42co3	NH4+=2;CO3^2-=1	0		0	
cacl2	Ca2+=1;Cl-=2	0		0	
kio3	K+=1;IO3-=1	0		0	
na2s2o3	Na+=2;S2O3^2-=1	0		0	
na2s4o6	Na+=2;S4O6^2-=1	0		0	
hcl	H+=1;Cl-=1	0		0	
hcl_conc	H+=1;Cl-=1	0		0	
h2so4	H+=2;SO4^2-=1	0		0	
h2so4_conc	H+=2;SO4^2-=1	0		0	
hno3	H+=1;NO3-=1	0		0	
hno3_conc	H+=1;NO3-=1	0		0	
hbr	H+=1;Br-=1	0		0	
hi	H+=1;I-=1	0		0	
h3po4	H+=1;PO4^3-=1	1	0.1	0	
h2co3	H+=1;HCO3-=1	1	0.004	0	
ch3cooh	CH3COO-=1;H+=1	1	0.013	0	
ch3cooh_glacial	CH3COO-=1;H+=1	1	0.013	0	ch3cooh
h2s	H+=1;S2-=1	1	0.001	0	
h2c2o4	H+=2;C2O4^2-=1	1	0.06	0	
hf	H+=1;F-=1	1	0.08	0	
hclo	H+=1;ClO-=1	1	0.003	0	
h2sio3		1	0	0	
naoh	Na+=1;OH-=1	0		0	
koh	K+=1;OH-=1	0		0	
baoh2	Ba2+=1;OH-=2	0		0	
caoh2	Ca2+=1;OH-=2	0		0	
nh3	NH4+=1;OH-=1	1	0.013	0	
nacl	Na+=1;Cl-=1	0		0	
kcl	K+=1;Cl-=1	0		0	
bacl2	Ba2+=1;Cl-=2	0		0	
mgcl2	Mg2+=1;Cl-=2	0		0	
alcl3	Al3+=1;Cl-=3	0		0	
fecl2	Fe2+=1;Cl-=2	0		0	
fecl3	Fe3+=1;Cl-=3	0		0	
cucl2	Cu2+=1;Cl-=2	0		0	
zncl2	Zn2+=1;Cl-=2	0		0	
agno3	Ag+=1;NO3-=1	0		0	
kno3	K+=1;NO3-=1	0		0	
nano3	Na+=1;NO3-=1	0		0	
pbno32	Pb2+=1;NO3-=2	0		0	
cuno32	Cu2+=1;NO3-=2	0		0	
mgno32	Mg2+=1;NO3-=2	0		0	
na2co3	Na+=2;CO3^2-=1	0		0	
k2co3	K+=2;CO3^2-=1	0		0	
nahco3	Na+=1;HCO3-=1	0		0	
na2so4	Na+=2;SO4^2-=1	0		0	
cuso4	Cu2+=1;SO4^2-=1	0		0	
cuso45h2o	Cu2+=1;SO4^2-=1	0		0	
feso4	Fe2+=1;SO4^2-=1	0		0	
fe2so43	Fe3+=2;SO4^2-=3	0		0	
znso4	Zn2+=1;SO4^2-=1	0		0	
mgso4	Mg2+=1;SO4^2-=1	0		0	
al2so43	Al3+=2;SO4^2-=3	0		0	
ki	K+=1;I-=1	0		0	
kbr	K+=1;Br-=1	0		0	
nabr	Na+=1;Br-=1	0		0	
nai	Na+=1;I-=1	0		0	
kmno4	K+=1;MnO4-=1	0		0	
k2cr2o7	K+=2;Cr2O7^2-=1	0		0	
nh4cl	NH4+=1;Cl-=1	0		0	
nh42so4	NH4+=2;SO4^2-=1	0		0	
nh4no3	NH4+=1;NO3-=1	0		0	
na3po4	Na+=3;PO4^3-=1	0		0	
na2s	Na+=2;S2-=1	0		0	
na2sio3	Na+=2;SiO3^2-=1	0		0	
naclo	Na+=1;ClO-=1	0		0	
kscn	K+=1;SCN-=1	0		0	
limewater	Ca2+=1;OH-=2	0		0	
fescn3	Fe(SCN)3=1	0		0	
cunh34	[Cu(NH3)4]2+=1	0		0	
agnh32	[Ag(NH3)2]+=1	0		0	
c6h5ona	C6H5O-=1;Na+=1	0		0	
c17h33coona	Na+=1	0		0	
cahco32	Ca2+=1;HCO3-=2	0		0	
c2h5oh		0		1	
ch3oh		0		1	
c3h8o3		0		1	
glucose		0		1	
sucrose		0		1	
urea		0		1	
acetone		0		1	
glycine		0		1	
h2o2		0		1	
ch3cho		0		1	
hcooh	HCOO-=1;H+=1	1	0.05	0	
vitc		0		1	
ch3cooc2h5		0		1	
";

/* ---------- 沉淀外观：化学式 颜色 描述 溶于酸 溶于氨水 ---------- */
public const string TblPrecipitates = @"
AgCl	#f4f4f4	白色絮状	0	1
AgBr	#efe6c4	淡黄色	0	0
AgI	#e8d84a	黄色	0	0
BaSO4	#f8f8f8	白色	0	0
PbSO4	#f8f8f8	白色	0	0
CaSO4	#f8f8f8	白色微溶	0	0
Ca(OH)2	#f8f8f8	白色	1	0
CaCO3	#f8f8f8	白色	1	0
BaCO3	#f8f8f8	白色	1	0
CuCO3	#3a9a6a	蓝绿色	1	0
Cu(OH)2	#3a8ec8	蓝色絮状	1	1
Fe(OH)2	#dfe8df	白色（迅速变灰绿）	1	0
Fe(OH)3	#8a3a12	红褐色絮状	1	0
Al(OH)3	#f8f8f8	白色胶状	1	0
Mg(OH)2	#f8f8f8	白色	1	0
Zn(OH)2	#f8f8f8	白色	1	1
Pb(OH)2	#f8f8f8	白色	1	0
PbI2	#e8c81a	金黄色	0	0
PbCrO4	#e8c81a	黄色	0	0
BaCrO4	#e8d84a	黄色	1	0
Ag2CrO4	#a83a2a	砖红色	1	0
CuS	#151515	黑色	0	0
FeS	#151515	黑色	1	0
ZnS	#f8f8f8	白色	1	0
PbS	#151515	黑色	1	0
Ag2S	#151515	黑色	0	0
Ba3(PO4)2	#f8f8f8	白色	1	0
Ca3(PO4)2	#f8f8f8	白色	1	0
Ag3PO4	#e8d84a	黄色	1	0
H2SiO3	#f0f0f0	白色胶状	0	0
Cu2(OH)2CO3	#2f8a5a	绿色	1	0
MgCO3	#f8f8f8	白色	1	0
ZnCO3	#f8f8f8	白色	1	0
FeCO3	#d8e0d0	灰白色	1	0
PbCO3	#f8f8f8	白色	1	0
Ag2CO3	#f0e8b0	淡黄色	1	0
CaSO3	#f8f8f8	白色	1	0
CaSiO3	#f8f8f8	白色	1	0
BaSiO3	#f8f8f8	白色	1	0
Mn(OH)2	#e8d8c8	白色	1	0
Cr(OH)3	#5a8a6a	灰绿色	1	0
MnS	#d8c8a8	肉色	1	0
HgS	#1a1a1a	黑色	0	0
Cu2O	#a83a1a	砖红色	1	0
BaSO3	#f8f8f8	白色	1	0
C6H2Br3OH	#f8f8f8	白色	0	0
MnO2	#151515	棕黑色	0	0
";

/* ---------- 沉淀生成：阳离子 阴离子 沉淀id 阳系数 阴系数 ---------- */
public const string TblPrecipRules = @"
Ag+	Cl-	agcl	1	1
Ag+	Br-	agbr	1	1
Ag+	I-	agi	1	1
Ag+	S2-	ag2s	2	1
Ag+	CO3^2-	ag2co3	2	1
Ba2+	SO4^2-	baso4	1	1
Ba2+	CO3^2-	baco3	1	1
Ba2+	SO3^2-	baso3	1	1
Ba2+	SiO3^2-	basio3	1	1
Pb2+	SO4^2-	pbso4	1	1
Pb2+	I-	pbi2	1	2
Pb2+	S2-	pbs	1	1
Pb2+	CO3^2-	pbco3	1	1
Pb2+	CrO4^2-	pbcro4	1	1
Ca2+	CO3^2-	caco3	1	1
Ca2+	SO4^2-	caso4	1	1
Ca2+	SiO3^2-	casio3	1	1
Ca2+	OH-	caoh2	1	2
Cu2+	S2-	cus	1	1
Cu2+	CO3^2-	cuco3	1	1
Cu2+	OH-	cuoh2	1	2
Fe2+	S2-	fes	1	1
Fe2+	CO3^2-	feco3	1	1
Fe2+	OH-	feoh2	1	2
Fe3+	OH-	feoh3	1	3
Al3+	OH-	aloh3	1	3
Al3+	CO3^2-	alco3	2	3
Mg2+	OH-	mgoh2	1	2
Mg2+	CO3^2-	mgco3	1	1
Zn2+	S2-	zns	1	1
Zn2+	CO3^2-	znco3	1	1
Zn2+	OH-	znoh2	1	2
Mn2+	S2-	mns	1	1
Mn2+	OH-	mnoh2	1	2
Cr3+	OH-	croh3	1	3
Hg2+	S2-	hgs	1	1
";

/* ---------- 溶解度通则：阴离子 总是可溶 难溶阳离子 微溶阳离子 可溶阳离子 其余难溶 ---------- */
public const string TblSolubility = @"
NO3-	1				0
CH3COO-	1				0
ClO3-	1				0
Cl-	0	Ag+;Pb2+;Hg2+			0
Br-	0	Ag+;Pb2+;Hg2+			0
I-	0	Ag+;Pb2+;Hg2+			0
F-	0	Mg2+;Ca2+;Ba2+;Sr2+;Pb2+			0
SO4^2-	0	Ba2+;Pb2+;Sr2+	Ca2+;Ag+		0
CO3^2-	0			Na+;K+;NH4+	1
PO4^3-	0			Na+;K+;NH4+	1
S2-	0		Ca2+;Ba2+	Na+;K+;NH4+;Ca2+;Ba2+;Sr2+;Mg2+	1
OH-	0		Ca2+	Na+;K+;NH4+;Ba2+;Sr2+	1
SiO3^2-	0			Na+;K+	1
";


/* ---------- 易挥发物种：物种id 蒸气对应气体id ---------- */
public const string TblVolatile = @"
nh3	nh3
so2	so2
co2	co2
h2s	h2s
HSO3-	so2
hso3-	so2
HCO3-	co2
hco3-	co2
H2CO3	co2
h2co3	co2
S2-	h2s
s2-	h2s
no2	no2
cl2	cl2
hbr	hbr
hi	hi
ch3nh2	ch3nh2
c2h5nh2	c2h5nh2
";

/* ---------- 键能：原子A 原子B 键级 键能kJ/mol ---------- */
public const string TblBondEnergy = @"
H	H	1	436
D	D	1	442
N	N	3	944
O	O	2	496
P	P	3	485
S	S	2	425
F	F	1	158
Cl	Cl	1	242
Br	Br	1	193
I	I	1	151
H	F	1	562
H	Cl	1	431
H	Br	1	366
H	I	1	299
C	O	3	1077
C	C	1	350
C	C	2	610
C	C	3	840
C	C	1.5	520
C	H	1	410
C	Cl	1	340
C	Br	1	280
C	I	1	240
C	N	1	305
C	N	2	610
C	N	3	890
C	O	1	360
C	O	2	740
C	O	2.5	805
N	N	1	160
N	N	2	410
N	H	1	390
N	Cl	1	310
O	O	1	150
O	H	1	460
Si	Si	1	222
Si	Cl	1	359
Si	H	1	320
Si	O	1	460
Si	O	2	640
P	P	1	200
P	H	1	320
P	Cl	1	330
P	O	1	340
P	O	2	540
S	S	1	264
S	H	1	347
S	Cl	1	250
S	O	1	360
S	O	2	500
";

/* ---------- 标准生成焓：物种id kJ/mol ---------- */
public const string TblFormationH = @"
h2	0
o2	0
n2	0
cl2	0
br2	0
i2	0
f2	0
c	0
s	0
p	0
na	0
k	0
mg	0
ca	0
al	0
fe	0
cu	0
zn	0
ag	0
ba	0
li	0
pb	0
sn	0
ni	0
cr	0
mn	0
co	-111
si	0
sr	0
cs	0
rb	0
he	0
ne	0
ar	0
h2o	-286
h2o_g	-242
co2	-394
ch4	-75
c2h6	-85
c2h4	52
c2h2	227
c3h8	-104
c4h10	-126
ch3oh	-239
c2h5oh	-278
c6h12o6	-1273
ch3cooh	-485
hcooh	-425
h2c2o4	-827
nh3	-46
n2h4	50
no	90
no2	33
n2o	82
hno3	-174
hno2	-79
so2	-297
so3	-396
h2so4	-814
h2s	-21
hcl	-92
hbr	-36
hi	26
hf	-271
nacl	-411
kcl	-437
licl	-409
cacl2	-796
mgcl2	-641
bacl2	-859
fecl2	-342
fecl3	-399
alcl3	-704
cucl2	-206
zncl2	-415
agcl	-127
naoh	-425
koh	-425
caoh2	-987
baoh2	-946
mgoh2	-925
aloh3	-1276
feoh2	-570
feoh3	-824
cuoh2	-450
znoh2	-642
caco3	-1207
baco3	-1216
mgco3	-1096
na2co3	-1131
k2co3	-1151
cao	-635
mgo	-602
bao	-548
cuo	-156
zno	-348
fe2o3	-824
al2o3	-1676
fe3o4	-1118
sio2	-911
cuso4	-771
znso4	-982
mgso4	-1285
feso4	-929
na2so4	-1387
agno3	-124
kno3	-494
nano3	-467
nh4cl	-314
nh4no3	-366
nh42so4	-1181
ki	-328
kbr	-393
nai	-288
nabr	-360
kmno4	-837
k2cr2o7	-2061
mno2	-520
cro3	-1141
agbr	-100
agi	-62
baso4	-1473
pbso4	-920
caso4	-1434
c6h6	49
c6h5ch3	12
c6h12	-156
c6h5oh	-165
c6h5nh2	31
ch3cho	-192
ch3coch3	-248
ch3cooc2h5	-479
ch3cl	-82
ch3br	-36
ch3i	15
ch2cl2	-95
chcl3	-103
ccl4	-96
hcn	135
cs2	88
nh4br	-271
nh4i	-201
cubr2	-105
cui	-67
cu2o	-168
ag2o	-31
hgo	-90
";

/* ---------- 原子化焓：元素 kJ/mol ---------- */
public const string TblAtomisation = @"
K	89
Na	108
Li	159
Mg	147
Ca	178
Ba	180
Al	326
Fe	416
Cu	339
Zn	131
C	715
Si	450
H	218
N	473
O	249
F	79
Cl	121
Br	112
I	107
S	223
Ra	159
";

/* ---------- 电子亲和能：物种 kJ/mol ---------- */
public const string TblElectronAffinity = @"
F	-328
Cl	-349
Br	-324
I	-295
O	-141
O-	798
Br_q	-325
F_q	-348
S	-200
S-	532
H	-72
N	7
";

/* ---------- 反应焓：方程式 kJ/mol 说明 ---------- */
public const string TblReactionH = @"
CH4 + 2O2 -> CO2 + 2H2O	-890	甲烷燃烧焓
C2H5OH + 3O2 -> 2CO2 + 3H2O	-1367	乙醇燃烧焓
C + O2 -> CO2	-394	碳完全燃烧
2C + O2 -> 2CO	-222	碳不完全燃烧
2H2 + O2 -> 2H2O	-572	氢气燃烧
C6H12O6 + 6O2 -> 6CO2 + 6H2O	-2801	葡萄糖燃烧
CH3OH + 1.5O2 -> CO2 + 2H2O	-726	甲醇燃烧
H2 + 0.5O2 -> H2O	-286	水的生成焓
N2 + 3H2 -> 2NH3	-92	合成氨
2SO2 + O2 -> 2SO3	-197	接触氧化
H+ + OH- -> H2O	-57.3	强酸强碱中和焓
NH4Br -> NH4+ + Br-	16.8	溴化铵溶解焓
NH4Cl -> NH4+ + Cl-	14.8	氯化铵溶解焓
NaOH -> Na+ + OH-	-44.5	氢氧化钠溶解焓
2H2O2 -> 2H2O + O2	-196.4	过氧化氢分解
CaCO3 -> CaO + CO2	178	碳酸钙分解
H2 + Cl2 -> 2HCl	-184	氢氯化合
H2 + F2 -> 2HF	-537	氢氟化合
H2 + Br2 -> 2HBr	-103	氢溴化合
H2 + I2 -> 2HI	53	碘化氢生成
N2 + O2 -> 2NO	180	氮的固定
2NO + O2 -> 2NO2	-114	一氧化氮氧化
";

/* ---------- 气体的酸碱性（石蕊检验的依据） ---------- */
public const string TblGasNature = @"
nh3	base
ch3nh2	base
ch3nhch3	base
c2h5nh2	base
(ch3)3n	base
n2h4	base
co2	acid
so2	acid
so3	acid
hcl	acid
hbr	acid
hi	acid
h2s	acid
no2	acid
hf	acid
hcn	acid
cl2	bleach
so2_bleach	bleach
h2	neut
o2	neut
n2	neut
co	neut
no	neut
ch4	neut
c2h4	neut
c2h2	neut
c2h6	neut
he	neut
ar	neut
n2o	neut
";

/* ---------- 气体对木条的作用（燃烧/复燃/爆鸣） ---------- */
public const string TblGasSplint = @"
h2	pop	pop_weak	发出尖锐的爆鸣声，木条上的火焰随之熄灭
o2	brighter	relight	带火星的木条立即复燃，燃烧更旺
co2	out	out	木条立即熄灭
n2	out	out	木条立即熄灭
ar	out	out	木条立即熄灭
he	out	out	木条立即熄灭
ch4	quiet	out	气体安静地燃烧，火焰呈淡蓝色，没有爆鸣声
co	quiet	out	气体安静地燃烧，火焰呈蓝色，没有爆鸣声
c2h6	quiet	out	气体安静地燃烧，没有爆鸣声
c2h4	sooty	out	燃烧时火焰明亮并伴有黑烟
c2h2	sooty	out	燃烧时火焰明亮并伴有浓黑烟
h2s	quiet	out	气体燃烧，火焰呈淡蓝色，有刺激性气味
cl2	out	out	木条熄灭
no2	out	out	木条熄灭
so2	out	out	木条熄灭
nh3	out	out	木条熄灭
no	out	out	木条熄灭
n2o	brighter	relight	带火星的木条复燃（一氧化二氮受热分解放出氧气）
";

/* ---------- 焰色：离子 颜色 ---------- */
public const string TblFlameColors = @"
Na+	#ffcc33
K+	#c060ff
Ca2+	#c85a2a
Ba2+	#a8e04a
Sr2+	#e83a3a
Cu2+	#4ae07a
Li+	#e83a5a
Pb2+	#88bbff
";

/* ---------- 水溶液储备液浓度 mol/L ---------- */
public const string TblMolarity = @"
hcl	1
h2so4	1
hno3	1
hbr	1
hi	1
h3po4	1
ch3cooh	1
h2c2o4	0.5
h2s	0.1
hf	1
hclo	0.5
naoh	1
koh	1
baoh2	0.5
caoh2	0.02
nh3	1
limewater	0.02
hcl_conc	12
hno3_conc	16
kmno4	0.05
k2cr2o7	0.1
ki	0.5
kbr	0.5
nacl	1
agno3	0.2
bacl2	0.2
cuso4	0.5
pbno32	0.2
kscn	0.2
na2s	0.5
iodine_sol	0.05
phenolphthalein	0.01
litmus	0.02
methylorange	0.01
h2o2	1
c2h5oh	1
ch3cho	1
ch3cooh_glacial	17
h2so4_conc	18
";

/* ---------- 遇水剧烈反应的物质 ---------- */
public const string TblWaterReactive = @"
na
k
ca
na2o
cao
na2o2
al2s3
c2h5ona
bao
";

/* ---------- 反应规则：方程 现象 cond gas bubble heat smoke flame precip mirror explode tint clearTint deposit float odor ---------- */
public const string TblRules = @"
H+ + OH- -> H2O	酸碱中和，溶液温度升高								0	0		0		0		0
H+ + NH3 -> NH4+	氨水的刺激性气味明显减弱								0	0		0		0	-1	0
CH3COOH + OH- -> CH3COO- + H2O	中和反应，无明显外观变化				2				0	0		0		0		0
CO3^2- + 2H+ -> H2O + CO2	立即产生大量气泡，逸出无色无味气体		co2	3					0	0		0		0		0
HCO3- + H+ -> H2O + CO2	迅速产生大量气泡，气体使澄清石灰水变浑浊		co2	3					0	0		0		0		0
S2- + 2H+ -> H2S	产生有臭鸡蛋气味的气体		h2s	2					0	0		0		0	1	0
CaCO3 + 2H+ -> Ca2+ + H2O + CO2	固体表面立即产生大量气泡，固体逐渐溶解		co2	3					0	0		0		0		0
BaCO3 + 2H+ -> Ba2+ + H2O + CO2	产生大量气泡，白色固体溶解		co2	3					0	0		0		0		0
MgCO3 + 2H+ -> Mg2+ + H2O + CO2	产生大量气泡		co2	3					0	0		0		0		0
ZnCO3 + 2H+ -> Zn2+ + H2O + CO2	产生大量气泡		co2	3					0	0		0		0		0
FeCO3 + 2H+ -> Fe2+ + H2O + CO2	产生大量气泡，溶液呈浅绿色		co2	3					0	0		0		0		0
Cu2(OH)2CO3 + 4H+ -> 2Cu2+ + 3H2O + CO2	绿色固体溶解，产生气泡，溶液变蓝		co2	2					0	0		0		0		0
CaSO3 + 2H+ -> Ca2+ + H2O + SO2	产生有刺激性气味的气体		so2	2					0	0		0		0	2	0
BaSO3 + 2H+ -> Ba2+ + H2O + SO2	白色沉淀溶解，产生刺激性气体		so2	2					0	0		0		0	2	0
Na2SiO3 + 2H+ -> H2SiO3 + 2Na+	产生白色胶状沉淀							H2SiO3	0	0		0		0		0
CaCO3 + 2CH3COOH -> Ca2+ + 2CH3COO- + H2O + CO2	缓慢产生气泡（醋酸酸性弱于盐酸）		co2	2					0	0		0		0		0
SiO3^2- + 2H+ -> H2SiO3	产生白色胶状沉淀，久置逐渐凝聚							H2SiO3	0	0		0		0		0
AlO2- + H+ + H2O -> Al(OH)3	产生白色胶状沉淀，继续加酸沉淀溶解							Al(OH)3	0	0		0		0		0
ZnO2^2- + 2H+ -> Zn(OH)2	产生白色沉淀							Zn(OH)2	0	0		0		0		0
ClO- + 2H+ + Cl- -> Cl2 + H2O	产生黄绿色有刺激性气味的气体		cl2	2					0	0		0		0	1	0
Zn + 2H+ -> Zn2+ + H2	锌粒表面产生大量气泡，锌逐渐溶解，溶液温度升高		h2	3	4				0	0		0		0		0
Mg + 2H+ -> Mg2+ + H2	镁条剧烈反应，产生大量气泡并迅速溶解，试管烫手		h2	3	9				0	0		0		0		0
Fe + 2H+ -> Fe2+ + H2	铁表面产生气泡，溶液逐渐变为浅绿色		h2	2	3				0	0		0		0		0
2Al + 6H+ -> 2Al3+ + 3H2	铝表面氧化膜溶解后开始剧烈冒气泡		h2	3	6				0	0		0		0		0
Ca + 2H+ -> Ca2+ + H2	剧烈反应，产生大量气泡		h2	3	8				0	0		0		0		0
Pb + 2H+ -> Pb2+ + H2	反应较缓慢，缓慢产生气泡		h2	1	1				0	0		0		0		0
2Na + 2H2O -> 2Na+ + 2OH- + H2	钠浮在水面上，熔成闪亮的小球四处游动，发出嘶嘶声，滴入酚酞溶液变红		h2	3	10				0	0		0		1		0
2K + 2H2O -> 2K+ + 2OH- + H2	钾与水剧烈反应，立即燃烧并发生轻微爆炸，火焰呈紫色		h2	3	14		#c060ff		0	1		0		0		0
Ca + 2H2O -> Ca2+ + 2OH- + H2	钙粒表面产生气泡，逐渐溶解，溶液变浑浊		h2	2	6				0	0		0		0		0
Mg + 2H2O -> Mg2+ + 2OH- + H2	镁条表面缓慢产生细小气泡（加热时明显加快）	heat	h2	1					0	0		0		0		0
Zn + Cu2+ -> Zn2+ + Cu	锌表面覆盖一层红色的铜，蓝色溶液逐渐变浅直至褪色								0	0		0	#b5651d	0		0
Fe + Cu2+ -> Fe2+ + Cu	铁钉表面附着红色物质，蓝色溶液变为浅绿色								0	0		0	#b5651d	0		0
2Al + 3Cu2+ -> 2Al3+ + 3Cu	铝表面析出红色铜，蓝色褪去								0	0		0	#b5651d	0		0
Mg + Cu2+ -> Mg2+ + Cu	镁条表面迅速变红，溶液褪色								0	0		0	#b5651d	0		0
Cu + 2Ag+ -> Cu2+ + 2Ag	铜丝表面析出银白色晶体，溶液变蓝								0	0		0	#e8e8ec	0		0
Fe + 2Ag+ -> Fe2+ + 2Ag	铁表面析出银白色金属，溶液变浅绿色								0	0		0	#e8e8ec	0		0
Zn + 2Ag+ -> Zn2+ + 2Ag	锌表面析出银白色金属								0	0		0	#e8e8ec	0		0
Fe + 2Fe3+ -> 3Fe2+	棕黄色溶液逐渐变为浅绿色								0	0		0		0		0
Cu + 2Fe3+ -> Cu2+ + 2Fe2+	铜溶解，溶液由棕黄变为蓝绿色								0	0		0		0		0
Pb + Cu2+ -> Pb2+ + Cu	铅表面附着红色物质								0	0		0	#b5651d	0		0
Cl2 + 2I- -> 2Cl- + I2	溶液变为棕黄色，加入淀粉溶液后变蓝								0	0		0		0		0
Cl2 + 2Br- -> 2Cl- + Br2	溶液变为橙黄色								0	0		0		0		0
Br2 + 2I- -> 2Br- + I2	溶液由橙色变为棕黄色								0	0		0		0		0
CuO + 2H+ -> Cu2+ + H2O	黑色粉末逐渐溶解，溶液变为蓝色				1				0	0		0		0		0
Fe2O3 + 6H+ -> 2Fe3+ + 3H2O	红棕色粉末溶解，溶液变成棕黄色								0	0		0		0		0
Fe3O4 + 8H+ -> Fe2+ + 2Fe3+ + 4H2O	黑色固体溶解，溶液呈黄绿色								0	0		0		0		0
MgO + 2H+ -> Mg2+ + H2O	白色固体溶解，溶液温度上升				5				0	0		0		0		0
CaO + 2H+ -> Ca2+ + H2O	固体溶解并放热				6				0	0		0		0		0
ZnO + 2H+ -> Zn2+ + H2O	白色固体逐渐溶解								0	0		0		0		0
Al2O3 + 6H+ -> 2Al3+ + 3H2O	白色固体缓慢溶解								0	0		0		0		0
PbO + 2H+ -> Pb2+ + H2O	黄色固体溶解								0	0		0		0		0
MnO2 + 4H+ + 2Cl- -> Mn2+ + Cl2 + 2H2O	加热后产生黄绿色刺激性气体，溶液变为浅粉色	conc	cl2	2	4				0	0		0		0	1	0
ZnO + 2OH- -> ZnO2^2- + H2O	白色固体溶解，得到无色溶液								0	0		0		0		0
Al2O3 + 2OH- -> 2AlO2- + H2O	白色固体溶解								0	0		0		0		0
SiO2 + 4HF -> SiF4 + 2H2O	玻璃表面被腐蚀变毛，产生气体		sif4	1					0	0		0		0		0
Cr2O3 + 6H+ -> 2Cr3+ + 3H2O	绿色固体溶解，溶液呈绿色								0	0		0		0		0
Na2O + H2O -> 2Na+ + 2OH-	固体剧烈水解放热，溶液显强碱性				8				0	0		0		0		0
CaO + H2O -> Ca2+ + 2OH-	与水反应放出大量热，水沸腾冒白雾，块状固体崩解成粉末				15	2			0	0		0		0		0
SO2 + H2O -> H+ + HSO3-	气体溶解，溶液显酸性		so2						0	0		0		0		0
CO2 + H2O -> H2CO3	气体溶解，溶液显弱酸性	cool	co2						0	0		0		0		0
Al(OH)3 + OH- -> AlO2- + 2H2O	白色沉淀逐渐溶解，溶液变澄清								0	0		0		0		0
Zn(OH)2 + 2OH- -> ZnO2^2- + 2H2O	白色沉淀溶解								0	0		0		0		0
3NO3- + 8Al + 5OH- + 2H2O -> 3NH3 + 8AlO2-	铝箔表面产生气泡，加热后有刺激性气味气体放出，能使湿润红色石蕊试纸变蓝	heat	nh3	3					0	0		0		0		0
2Al + 2OH- + 2H2O -> 2AlO2- + 3H2	铝表面产生大量气泡，说明铝具有两性		h2	3	5				0	0		0		0		0
Al(OH)3 + 3H+ -> Al3+ + 3H2O	白色沉淀溶解								0	0		0		0		0
Zn(OH)2 + 2H+ -> Zn2+ + 2H2O	白色沉淀溶解								0	0		0		0		0
Cu(OH)2 + 2H+ -> Cu2+ + 2H2O	蓝色沉淀溶解，溶液变蓝								0	0		0		0		0
Fe(OH)3 + 3H+ -> Fe3+ + 3H2O	红褐色沉淀溶解，溶液呈棕黄色								0	0		0		0		0
Mg(OH)2 + 2H+ -> Mg2+ + 2H2O	白色沉淀溶解								0	0		0		0		0
Pb(OH)2 + 2H+ -> Pb2+ + 2H2O	白色沉淀溶解								0	0		0		0		0
NH4+ + OH- -> NH3 + H2O	产生有刺激性气味的气体，湿润的红色石蕊试纸变蓝	heat	nh3	3	2				0	0		0		0	2	0
NH4+ + OH- -> NH3 + H2O	加热时放出刺激性气体，试纸变蓝	heat	nh3	4					0	0		0		0	2	0
2MnO4- + 16H+ + 10Cl- -> 2Mn2+ + 5Cl2 + 8H2O	紫红色褪去，产生黄绿色有刺激性气味的气体（氯离子被氧化，干扰滴定）	acid	cl2						0	0		0		0		0
MnO4- + 8H+ + 5Fe2+ -> Mn2+ + 5Fe3+ + 4H2O	紫红色立即褪去，溶液由浅绿色变为棕黄色	acid							0	0		0		0		0
MnO4- + 2H2O + 3Fe2+ -> MnO2 + 3Fe3+ + 4OH-	紫红色褪去，产生棕黑色沉淀（缺酸时还原产物是 MnO2 而不是 Mn2+）	neutral						MnO2	0	0		0		0		0
2MnO4- + 5H2O2 + 6H+ -> 2Mn2+ + 5O2 + 8H2O	紫红色迅速褪去，产生大量气泡，溶液变为近无色		o2	3	4				0	0		0		0		0
2MnO4- + 5C2O4^2- + 16H+ -> 2Mn2+ + 10CO2 + 8H2O	紫红色褪去，产生气泡（加热后反应加快）	heat	co2	2					0	0		0		0		0
2MnO4- + 3SO2 + 2H2O -> 2MnO2 + 3SO4^2- + 4H+	紫红色褪去，产生棕黑色沉淀							MnO2	0	0		0		0		0
Cr2O7^2- + 3H2O2 + 8H+ -> 2Cr3+ + 3O2 + 7H2O	橙色变为绿色，同时产生气泡		o2	2					0	0		0		0		0
Cr2O7^2- + 3SO2 + 2H+ -> 2Cr3+ + 3SO4^2- + H2O	橙色变为绿色								0	0		0		0		0
2Fe2+ + Cl2 -> 2Fe3+ + 2Cl-	浅绿色溶液变为棕黄色								0	0		0		0		0
2Fe3+ + 2I- -> 2Fe2+ + I2	溶液变为棕黄色，滴加淀粉变蓝								0	0		0		0		0
H2O2 + 2I- + 2H+ -> I2 + 2H2O	溶液变棕黄，加入淀粉后呈蓝色								0	0		0		0		0
2H2O2 -> 2H2O + O2	产生大量气泡，带火星的木条复燃	catal	o2	3					0	0		0		0		0
Cu + 2H2SO4 -> Cu2+ + SO4^2- + SO2 + 2H2O	加热后铜丝溶解，产生刺激性气味气体，溶液变蓝	conc	so2	2	5				0	0		0		0	2	0
Cu + 4H+ + 2NO3- -> Cu2+ + 2NO2 + 2H2O	铜剧烈溶解，产生红棕色气体，溶液变蓝绿色	conc	no2	3	6				0	0		0		0	2	0
3Cu + 8H+ + 2NO3- -> 3Cu2+ + 2NO + 4H2O	铜溶解，产生无色气体，遇空气立即变为红棕色，溶液变蓝	conc	no	2	3				0	0		0		0		0
Ag + 2H+ + NO3- -> Ag+ + NO2 + H2O	银溶解，产生红棕色气体	conc	no2	2	3				0	0		0		0	2	0
2SO2 + O2 -> 2SO3	需在催化剂和加热条件下进行	catal							0	0		0		0		0
2NO + O2 -> 2NO2	无色气体立即变为红棕色		no2						0	0		0		0		0
SO2 + 2H2S -> 3S + 2H2O	产生淡黄色浑浊，析出硫单质								0	0		0		0		0
H2S + 2Fe3+ -> S + 2Fe2+ + 2H+	产生淡黄色浑浊								0	0		0		0		0
C + 2H2SO4 -> CO2 + 2SO2 + 2H2O	加热时木炭与浓硫酸剧烈反应，放出刺激性气体	conc	so2	2					0	0		0		0	2	0
H2C2O4 -> CO2 + CO + H2O	加热分解，放出气体	heat	co2	2					0	0		0		0		0
C6H8O6 + I2 -> C6H6O6 + 2H+ + 2I-	碘水的棕黄色迅速褪去								0	0		0		0		0
Cu2+ + 4NH3 -> [Cu(NH3)4]2+	氨水过量后蓝色沉淀溶解，溶液变为深蓝色（四氨合铜配离子）								0	0		0		0		0
Cu(OH)2 + 4NH3 -> [Cu(NH3)4]2+ + 2OH-	蓝色沉淀逐渐溶解，溶液变成深蓝色								0	0		0		0		0
Zn(OH)2 + 4NH3 -> [Zn(NH3)4]2+ + 2OH-	白色沉淀溶解，得到无色溶液								0	0		0		0		0
Ag+ + 2NH3 -> [Ag(NH3)2]+	先出现白色浑浊，氨水过量后溶解，得到无色银氨溶液								0	0		0		0		0
AgCl + 2NH3 -> [Ag(NH3)2]+ + Cl-	白色沉淀溶解，得到无色溶液								0	0		0		0		0
Fe3+ + 3SCN- -> Fe(SCN)3	溶液立即变为血红色								0	0		0		0		0
Fe3+ + 3C6H5O- -> Fe(C6H5O)3	溶液显紫色，这是苯酚的特征反应								0	0		0		0		0
AgBr + 2NH3 -> [Ag(NH3)2]+ + Br-	淡黄色沉淀缓慢溶解（比氯化银难溶得多）	conc_nh3							0	0		0		0		0
2H2 + O2 -> 2H2O	氢气安静燃烧，产生淡蓝色火焰，管壁出现水雾	ignite			12		#a8d8ff		0	0		0		0		0
CH4 + 2O2 -> CO2 + 2H2O	甲烷安静燃烧，产生明亮的淡蓝色火焰	ignite			12		#8ac8ff		0	0		0		0		0
2C2H2 + 5O2 -> 4CO2 + 2H2O	乙炔燃烧产生明亮火焰并伴有黑烟	ignite			14	2	#ffd070		0	0		0		0		0
C + O2 -> CO2	木炭在氧气中剧烈燃烧，发出白光，放出大量热	ignite			12		#ffd070		0	0		0		0		0
S + O2 -> SO2	硫在氧气中燃烧，发出明亮的蓝紫色火焰，产生刺激性气味气体	ignite			8		#8060ff		0	0		0		0	2	0
4P + 5O2 -> 2P2O5	红磷燃烧产生大量白烟，放出大量热	ignite			12	4	#ffd070		0	0		0		0		0
2Mg + O2 -> 2MgO	镁条剧烈燃烧，发出耀眼白光，生成白色固体	ignite			14		#ffffff		0	0		0		0		0
3Fe + 2O2 -> Fe3O4	铁丝在氧气中剧烈燃烧，火星四射，生成黑色固体	ignite			12		#ffb040		0	0		0		0		0
2Cu + O2 -> 2CuO	铜丝表面逐渐变黑	heat			3				0	0		0		0		0
4Na + O2 -> 2Na2O	钠表面迅速变暗，生成白色固体				4				0	0		0		0		0
2Na + O2 -> Na2O2	钠在氧气中燃烧，产生黄色火焰，生成淡黄色固体	ignite			10		#ffcc33		0	0		0		0		0
2CO + O2 -> 2CO2	一氧化碳燃烧产生蓝色火焰	ignite			10		#80a8ff		0	0		0		0		0
Fe + S -> FeS	加热后混合物红热，生成黑色固体	heat			8				0	0		0		0		0
2Al + 3S -> Al2S3	剧烈反应，生成灰黑色固体	heat			10				0	0		0		0		0
2H2S + 3O2 -> 2SO2 + 2H2O	燃烧产生淡蓝色火焰	ignite					#a0c8ff		0	0		0		0		0
ZnCO3·Zn(OH)2 + 4HCl -> 2ZnCl2 + CO2 + 3H2O	固体溶解并产生大量气泡，得到无色溶液		co2	3					0	0		0		0		0
ZnCO3·Zn(OH)2 + 4H+ -> 2Zn2+ + CO2 + 3H2O	固体溶解并产生大量气泡，得到无色溶液		co2	3					0	0		0		0		0
BaCl2·2H2O -> BaCl2 + 2H2O	固体受热失去结晶水，质量减轻，得到白色无水盐	heat							0	0		0		0		0
ZnCO3·Zn(OH)2 -> 2ZnO + CO2 + H2O	固体受热分解，放出气体和水蒸气，残留白色氧化锌	hot	co2	2					0	0		0		0		0
CaCO3 -> CaO + CO2	高温下分解，剩余固体变为白色疏松状	heat	co2	2					0	0		0		0		0
CuSO4·5H2O -> CuSO4 + 5H2O	蓝色晶体逐渐变为白色粉末，试管口有水珠	heat			-2				0	0		0		0		0
2NaHCO3 -> Na2CO3 + H2O + CO2	加热放出气体，试管口有水珠	heat	co2	2					0	0		0		0		0
Cu(OH)2 -> CuO + H2O	蓝色沉淀变为黑色固体	heat							0	0		0		0		0
2Fe(OH)3 -> Fe2O3 + 3H2O	红褐色沉淀变为红棕色固体	heat							0	0		0		0		0
2Al(OH)3 -> Al2O3 + 3H2O	白色沉淀变为白色固体	heat							0	0		0		0		0
Mg(OH)2 -> MgO + H2O	白色沉淀变为白色固体	heat							0	0		0		0		0
NH4Cl -> NH3 + HCl	固体逐渐减少，试管口有白色晶体附着	heat	nh3			2			0	0		0		0	2	0
2KMnO4 -> K2MnO4 + MnO2 + O2	紫黑色固体变为黑色，放出能使带火星木条复燃的气体	heat	o2	2					0	0		0		0		0
2KClO3 -> 2KCl + 3O2	需要二氧化锰作催化剂，加热放出氧气	catal	o2	2					0	0		0		0		0
Ca(HCO3)2 -> CaCO3 + H2O + CO2	加热后溶液变浑浊，产生气泡	heat	co2					CaCO3	0	0		0		0		0
H2CO3 -> H2O + CO2	碳酸不稳定，立即分解放出气体		co2	1					0	0		0		0		0
4HNO3 -> 4NO2 + O2 + 2H2O	浓硝酸见光分解，产生红棕色气体	light	no2	1					0	0		0		0	2	0
2AgNO3 -> 2Ag + 2NO2 + O2	见光逐渐分解，固体变黑	light	no2						0	0		0		0		0
CH3COOH + C2H5OH -> CH3COOC2H5 + H2O	加热后产生有果香味的油状液体，浮在液面上	catal_warm							0	0		0		0		0
2C2H5OH + 2Na -> 2C2H5ONa + H2	乙醇中投入钠粒，缓慢产生气泡，钠沉底		h2	2	3				0	0		0		0		0
C2H5OH -> C2H4 + H2O	浓硫酸催化下加热到 170℃，产生能使溴水褪色的气体	catal_hot	c2h4	2					0	0		0		0		0
2CH3CHO + O2 -> 2CH3COOH	乙醛被氧化，溶液酸性增强	catal			3				0	0		0		0		0
CH3CHO + 2[Ag(NH3)2]+ + 3OH- -> CH3COO- + 2Ag + 4NH3 + 2H2O	试管内壁附着一层光亮如镜的银，形成银镜				4				1	0		0		0		0
CH3CHO + 2Cu(OH)2 + OH- -> CH3COO- + Cu2O + 3H2O	出现砖红色沉淀	heat						Cu2O	0	0		0		0		0
C6H12O6 + 2Cu(OH)2 -> C6H12O7 + Cu2O + 2H2O	出现砖红色沉淀，说明葡萄糖有还原性	heat						Cu2O	0	0		0		0		0
C6H12O6 + 2[Ag(NH3)2]+ + 2OH- -> C6H12O7 + 2Ag + 4NH3 + H2O	试管内壁形成光亮的银镜	heat							1	0		0		0		0
C6H6 + Br2 -> C6H5Br + HBr	在铁粉催化下发生取代，产生白雾，液体分层	catal				1			0	0		0		0		0
C6H6 + HNO3 -> C6H5NO2 + H2O	浓硫酸催化下加热，生成有苦杏仁气味的淡黄色油状液体	catal							0	0		0		0	1	0
C2H4 + Br2 -> C2H4Br2	溴的四氯化碳溶液迅速褪色								0	0		0		0		0
C2H2 + 2Br2 -> C2H2Br4	溴水迅速褪色								0	0		0		0		0
C2H4 + H2O -> C2H5OH	催化剂加热条件下发生加成	catal							0	0		0		0		0
C2H4 + H2 -> C2H6	催化剂加热下与氢气加成	catal							0	0		0		0		0
CH4 + Cl2 -> CH3Cl + HCl	黄绿色逐渐褪去，管壁出现油状液滴，产生白雾	light				1			0	0		0		0		0
C2H5Br + OH- -> C2H5OH + Br-	加热后溴乙烷层逐渐减少，生成乙醇	heat							0	0		0		0		0
(C17H33COO)3C3H5 + 3NaOH -> 3C17H33COONa + C3H8O3	加热搅拌后混合物变稠，不再分层，发生皂化反应	heat			3				0	0		0		0		0
C6H5OH + 3Br2 -> C6H2Br3OH + 3HBr	立即产生白色沉淀（三溴苯酚）							C6H2Br3OH	0	0		0		0		0
2C6H5OH + 2Na -> 2C6H5ONa + H2	缓慢产生气泡		h2	1					0	0		0		0		0
CH3COOH + NaHCO3 -> CH3COO- + Na+ + H2O + CO2	产生大量气泡，说明乙酸酸性强于碳酸		co2	3					0	0		0		0		0
HCOOH + 2[Ag(NH3)2]+ + 4OH- -> CO3^2- + 2Ag + 4NH3 + 3H2O	甲酸也能发生银镜反应	heat							1	0		0		0		0
H2S + Cu2+ -> CuS + 2H+	立即产生黑色沉淀							CuS	0	0		0		0		0
H2S + Zn2+ -> ZnS + 2H+	产生白色沉淀							ZnS	0	0		0		0		0
H2S + Pb2+ -> PbS + 2H+	产生黑色沉淀							PbS	0	0		0		0		0
H2S + 2Ag+ -> Ag2S + 2H+	产生黑色沉淀							Ag2S	0	0		0		0		0
CO2 + Ca2+ + 2OH- -> CaCO3 + H2O	澄清石灰水变浑浊，产生白色沉淀							CaCO3	0	0		0		0		0
CO2 + Ba2+ + 2OH- -> BaCO3 + H2O	产生白色沉淀							BaCO3	0	0		0		0		0
SO2 + Ba2+ + 2OH- -> BaSO3 + H2O	产生白色沉淀							BaSO3	0	0		0		0		0
2CrO4^2- + 2H+ -> Cr2O7^2- + H2O	黄色溶液变为橙色								0	0		0		0		0
Cr2O7^2- + 2OH- -> 2CrO4^2- + H2O	橙色溶液变为黄色								0	0		0		0		0
3NO3- + 8Al + 5OH- + 2H2O -> 3NH3 + 8AlO2-	铝箔表面产生气泡，加热后放出氨气	heat	nh3	3					0	0		0		0		0
CH3CHBrCOOH + NaOH -> CH3CHBrCOO- + Na+ + H2O	酸碱中和，酚酞/百里酚酞变色指示终点								0	0		0		0		0
CH3CHBrCOO- + OH- -> CH3CH(OH)COO- + Br-	加热后溴被羟基取代，得到含溴离子的溶液	heat							0	0		0		0		0
CH3CHBrCOOH + 2OH- -> CH3CH(OH)COO- + Br- + H2O	加热后卤素被羟基取代，溶液中出现溴离子	heat							0	0		0		0		0
N2 + 3H2 -> 2NH3	化合生成氨，反应可逆，产率受压强与温度影响	catal_hot	nh3		6				0	0		0		0		1
2SO2 + O2 -> 2SO3	接触室中二氧化硫被氧化，反应可逆	catal_hot			5				0	0		0		0		1
H2 + I2 -> 2HI	碘蒸气与氢气化合，反应可逆	heat							0	0		0		0		1
2NO2 -> N2O4	红棕色变浅，反应可逆								0	0		0		0		1
CH3COOH + C2H5OH -> CH3COOC2H5 + H2O	有果香味的酯生成，反应可逆	catal_warm							0	0		0		0		1
Cr(OH)3 + OH- -> Cr(OH)4-	灰绿色沉淀溶解，得到深绿色溶液								0	0		1		0		0
Zn(OH)2 + 4NH3 -> [Zn(NH3)4]2+ + 2OH-	白色沉淀溶解，得到无色溶液								0	0		1		0		0
2MnO4- + 5SO3^2- + 6H+ -> 2Mn2+ + 5SO4^2- + 3H2O	紫红色褪去，溶液变为近无色								0	0		0		0		0
SO3^2- + 2H+ -> SO2 + H2O	产生有刺激性气味的气体，能使湿润的蓝色石蕊试纸变红		so2	3					0	0		0		0	2	0
Na2SO3 + 2HCl -> 2NaCl + SO2 + H2O	固体溶解并产生有刺激性气味的气体		so2	3					0	0		0		0	2	0
S2O3^2- + 2H+ -> SO2 + S + H2O	溶液出现淡黄色浑浊，并放出有刺激性气味的气体	no_i2	so2					S	0	0		0		0		0
S2O3^2- + 2H+ -> S + SO2 + H2O	溶液出现淡黄色浑浊，并放出有刺激性气味的气体	no_i2	so2					S	0	0		0		0		0
IO3- + 5I- + 6H+ -> 3I2 + 3H2O	溶液立即变为棕黄色（生成碘），加淀粉后呈蓝黑色	acid							0	0		0		0		0
I2 + 2S2O3^2- -> 2I- + S4O6^2-	蓝色恰好褪去，溶液变为无色								0	0		0		0		0
2NaOH + H2SO4 -> Na2SO4 + 2H2O	中和反应，溶液温度升高								0	0		0		0		0
S2O8^2- + 2I- -> 2SO4^2- + I2	溶液由无色变为棕黄色（碘析出）	catal							0	0		0		0		0
2Fe3+ + 2I- -> 2Fe2+ + I2	棕黄色加深，说明 Fe3+ 把 I- 氧化成碘								0	0		0		0		0
2NO + O2 -> 2NO2	无色气体变为红棕色	catal	no2						0	0		0		0		0
SO2 + NO2 -> SO3 + NO	红棕色褪去，得到无色气体								0	0		0		0		0
MgCO3 -> MgO + CO2	固体分解，放出使石灰水变浑浊的气体	hot	co2						0	0		0		0		0
CaCO3 -> CaO + CO2	固体分解，放出使石灰水变浑浊的气体	hot	co2						0	0		0		0		0
SrCO3 -> SrO + CO2	需要更高的温度才分解	hot	co2						0	0		0		0		0
BaCO3 -> BaO + CO2	最难分解，需强热	hot	co2						0	0		0		0		0
2Mg(NO3)2 -> 2MgO + 4NO2 + O2	放出红棕色气体，残留白色固体	hot	no2						0	0		0		0		0
2ca(no3)2 -> 2cao + 4no2 + o2	放出红棕色气体	hot	no2						0	0		0		0		0
Cu2+ + 2OH- -> Cu(OH)2	产生浅蓝色絮状沉淀							Cu(OH)2	0	0		0		0		0
Cu2+ + 4NH3 -> [Cu(NH3)4]2+	浅蓝色沉淀溶解，溶液变为深蓝色（铜氨配离子）								0	0		0		0		0
Cu(OH)2 + 4NH3 -> [Cu(NH3)4]2+ + 2OH-	蓝色沉淀溶解，得到深蓝色溶液								0	0		0		0		0
Co2+ + 2OH- -> Co(OH)2	先产生蓝色沉淀，放置后变为粉红色	neutral							0	0		0		0		0
Co2+ + 6NH3 -> [Co(NH3)6]2+	沉淀溶解，溶液变为黄棕色								0	0		0		0		0
Co(OH)2 + 6NH3 -> [Co(NH3)6]2+ + 2OH-	沉淀溶解，得到黄棕色溶液								0	0		0		0		0
Co2+ + 4Cl- -> [CoCl4]2-	溶液由粉红色变为蓝色（四面体氯合钴）	conc							0	0		0		0		0
2MnO4- + 5C2O4^2- + 16H+ -> 2Mn2+ + 10CO2 + 8H2O	紫红色褪去并放出气泡，加热后加快	heat	co2	2					0	0		0		0		0
2Cu2+ + 4I- -> 2CuI + I2	产生白色碘化亚铜沉淀，同时溶液呈棕色（析出碘）							CuI	0	0		0		0		0
C6H6 + Cl2 -> C6H5Cl + HCl	紫外光下发生取代，产生白雾（HCl）	light							0	0		0		0		0
C6H6 + Br2 -> C6H5Br + HBr	需溴化铁催化，产生白雾	catal							0	0		0		0		0
C6H6 + HNO3 -> C6H5NO2 + H2O	浓硝酸浓硫酸混合酸，50–60 ℃ 水浴，生成淡黄色油状硝基苯	catal_warm							0	0		0		0		0
C6H6 + CH3Cl -> C6H5CH3 + HCl	无水 AlCl3 催化，发生傅-克烷基化	catal							0	0		0		0		0
C6H6 + CH3COCl -> C6H5COCH3 + HCl	无水 AlCl3 催化，发生傅-克酰基化，生成苯乙酮	catal							0	0		0		0		0
C6H5CH3 + 3[O] -> C6H5COOH + H2O	酸性高锰酸钾氧化侧链，紫色褪去并生成苯甲酸								0	0		0		0		0
C6H6 + 3H2 -> C6H12	镍催化加热，苯环加成为环己烷	catal_hot							0	0		0		0		0
C6H5CH3 + Cl2 -> C6H5CH2Cl + HCl	光照下取代侧链上的氢，而不是苯环上的氢	light							0	0		0		0		0
C6H6 + Cl2 -> C6H5Cl + HCl	需要路易斯酸催化才成卤代芳烃	catal							0	0		0		0		0
CH3COOH + C2H5OH -> CH3COOC2H5 + H2O	浓硫酸催化加热，产生有果香味的酯	catal_warm							0	0		0		0		1
CH3COCl + C2H5OH -> CH3COOC2H5 + HCl	剧烈反应，放出 HCl 白雾，不需催化剂								0	0		0		0		0
C6H5OH + NaOH -> C6H5ONa + H2O	苯酚溶解，说明苯酚显酸性								0	0		0		0		0
2C6H5OH + 2Na -> 2C6H5ONa + H2	产生气泡，钠逐渐溶解		h2	2					0	0		0		0		0
C6H5OH + 3Br2 -> C6H2Br3OH + 3HBr	溴水褪色并立即产生白色沉淀（2,4,6-三溴苯酚）							C6H2Br3OH	0	0		0		0		0
C6H5CH3 + 3[O] -> C6H5COOH + H2O	侧链被氧化成羧基，得到苯甲酸								0	0		0		0		0
3CH3COOH + PCl3 -> 3CH3COCl + H3PO3	放出有刺激性气味的气体，得到乙酰氯								0	0		0		0		0
CH3COOH + PCl5 -> CH3COCl + POCl3 + HCl	剧烈反应，放出 HCl 白雾								0	0		0		0		0
CH3COOH + SOCl2 -> CH3COCl + SO2 + HCl	放出 SO2 和 HCl，副产物都是气体，产物易提纯		so2						0	0		0		0		0
HCOOH + 2Cu2+ + 6OH- -> CO3^2- + Cu2O + 4H2O	产生砖红色沉淀（斐林试剂被还原）	heat							0	0		0		0		0
HCOOH + 2Ag+ + 2OH- -> CO2 + 2Ag + 2H2O	试管内壁形成光亮的银镜	heat							1	0		0		0		0
5HCOOH + 2MnO4- + 6H+ -> 5CO2 + 2Mn2+ + 8H2O	紫色褪去并放出气泡	heat	co2						0	0		0		0		0
3HCOOH + Cr2O7^2- + 8H+ -> 3CO2 + 2Cr3+ + 7H2O	橙色变为绿色	heat	co2						0	0		0		0		0
2MnO4- + 5H2C2O4 + 6H+ -> 2Mn2+ + 10CO2 + 8H2O	紫红色褪去并放出气泡，反应需要加热引发	heat	co2	2					0	0		0		0		0
CH3COCl + H2O -> CH3COOH + HCl	遇水剧烈水解，放出 HCl 白雾								0	0		0		0		0
CH3COCl + C2H5OH -> CH3COOC2H5 + HCl	与醇反应生成酯，比酯化剧烈得多								0	0		0		0		0
CH3COCl + C6H5OH -> CH3COOC6H5 + HCl	与酚反应生成苯酯								0	0		0		0		0
CH3COCl + NH3 -> CH3CONH2 + HCl	生成乙酰胺，HCl 被过量氨中和成 NH4Cl								0	0		0		0		0
C2H5Br + 2NH3 -> C2H5NH2 + NH4Br	氨过量时主要得到伯胺								0	0		0		0		0
CH3CONH2 + 2h2 -> c2h5nh2 + H2O	氢化铝锂还原酰胺，得到伯胺	catal							0	0		0		0		0
CH3CN + 2h2 -> c2h5nh2	氢化铝锂还原腈，得到伯胺	catal							0	0		0		0		0
CH3CN + 2h2 -> c2h5nh2	镍催化加氢，腈还原为伯胺	catal_hot	h2						0	0		0		0		0
C6H5NO2 + 3h2 -> aniline + 2H2O	硝基苯被还原，得到无色油状苯胺	catal							0	0		0		0		0
aniline + 3Br2 -> c6h2br3nh2 + 3HBr	溴水褪色并立即产生白色沉淀							C6H2Br3NH2	0	0		0		0		0
aniline + HNO2 + HCl -> C6H5N2Cl + 2H2O	0–5 ℃ 下生成重氮盐，需保持低温	cool							0	0		0		0		0
C6H5N2Cl + H2O -> C6H5OH + N2 + HCl	升温后重氮盐水解，放出氮气并生成苯酚	heat	n2	2					0	0		0		0		0
C6H5N2Cl + C6H5OH -> C6H5N=NC6H4OH + HCl	发生偶联，生成橙红色的偶氮染料								0	0		0		0		0
C2H5NH2 + HCl -> C2H5NH3+ + Cl-	胺与酸反应生成铵盐								0	0		0		0		0
CH3CONH2 + H2O + H+ -> CH3COOH + NH4+	酸性水解，得到羧酸和铵盐	acid							0	0		0		0		0
CH3CONH2 + OH- -> CH3COO- + NH3	碱性水解，放出氨气	heat	nh3						0	0		0		0		0
CH3CONH2 + 2h2 -> c2h5nh2 + H2O	酰胺被还原成伯胺	catal							0	0		0		0		0
H2NCH2COOH + HCl -> [H3NCH2COOH]+ + Cl-	氨基酸与酸反应，氨基被质子化								0	0		0		0		0
H2NCH2COOH + OH- -> H2NCH2COO- + H2O	氨基酸与碱反应，羧基被中和								0	0		0		0		0
2H2NCH2COOH -> H2NCH2CONHCH2COOH + H2O	缩合形成肽键，得到二肽	catal_warm							0	0		0		0		0
Cr3+ + 3OH- -> Cr(OH)3	产生灰绿色沉淀							Cr(OH)3	0	0		0		0		0
Cr(OH)3 + OH- -> Cr(OH)4-	灰绿色沉淀溶于过量碱，得到深绿色溶液								0	0		0		0		0
Co2+ + 2OH- -> Co(OH)2	先产生蓝色沉淀，放置后变为粉红色							Co(OH)2	0	0		0		0		0
SO3^2- + Ba2+ -> BaSO3	产生白色沉淀							BaSO3	0	0		0		0		0
BaSO3 + 2H+ -> Ba2+ + SO2 + H2O	沉淀溶解并放出有刺激性气味的气体		so2						0	0		0		0		0
2MnO4- + 5SO3^2- + 6H+ -> 2Mn2+ + 5SO4^2- + 3H2O	紫红色褪去，说明亚硫酸根有还原性								0	0		0		0		0
";

/* ---------- 英文本地化：物质id 英文名 / 中文原文 英文译文 ---------- */

/* ---------- 焓变：物种id 溶解焓 kJ/mol ---------- */
public const string TblDissolveH = @"
nh4br	16.8
nh4cl	14.8
nh4no3	25.7
kcl	17.2
ki	20.3
kno3	34.9
nacl	3.9
naoh	-44.5
cacl2	-82.8
nh42so4	6
cuso45h2o	11
";

/* ---------- 焓变：方程式 反应焓 kJ/mol ---------- */
public const string TblRuleH = @"
H2O2 -> H2O + 0.5O2	-98.2
2H2O2 -> 2H2O + O2	-196.4
H+ + OH- -> H2O	-57.3
NaOH + HCl -> NaCl + H2O	-57.3
";

/* ---------- 官能团家族：物质id 家族 ---------- */
public const string TblFamilies = @"
ch4	alkane
c2h6	alkane
c3h8	alkane
c4h10	alkane
c2h4	alkene
c2h2	alkyne
c2h5br	halogenoalkane
ch3i	halogenoalkane
ch3cl	halogenoalkane
ch3oh	primary_alcohol
c2h5oh	primary_alcohol
c3h8o3	alcohol
ch2o	aldehyde
ch3cho	aldehyde
acetone	ketone
hcooh	carboxylic_acid
ch3cooh	carboxylic_acid
ch3cooh_glacial	carboxylic_acid
c17h35cooh	carboxylic_acid
ch3cooc2h5	ester
c6h6	arene
c7h8	arene
phoh	phenol
c6h5no2	nitroarene
aniline	primary_amine
ch3nh2	primary_amine
glycine	amino_acid
urea	amide
oil	ester
c2h5cl	halogenoalkane
c2h5i	halogenoalkane
ch3ch2ch2oh	primary_alcohol
ch3chohch3	secondary_alcohol
c2h5cho	aldehyde
ch3cooch3	ester
c2h5nh2	primary_amine
ch3cn	nitrile
c2h5cn	nitrile
ch3ch2cooh	carboxylic_acid
ch3conh2	amide
c3h6	alkene
c6h5ch3	arene
c6h5cl	halogenoarene
c6h5cooh	carboxylic_acid
ch3cocl	acyl_chloride
c6h5cocl	acyl_chloride
c6h5n2cl	diazonium
azo_benzene	azo
peptide_gly	peptide
";

/* ---------- 通用有机规则：反应家族 产物家族 条件 可用试剂 副产物 现象 ---------- */
public const string TblOrganicRules = @"
primary_alcohol	aldehyde	distil	k2cr2o7,kmno4,cro3	H2O	滴加酸性重铬酸钾并蒸馏，馏出液有刺激性气味并能发生银镜反应
primary_alcohol	carboxylic_acid	reflux	k2cr2o7,kmno4	H2O	回流下橙色逐渐变绿，得到有酸味的羧酸
secondary_alcohol	ketone	reflux	k2cr2o7,kmno4	H2O	橙色变绿，得到酮；酮不能再被氧化
aldehyde	carboxylic_acid	reflux	k2cr2o7,kmno4,o2		醛被氧化成羧酸
aldehyde	primary_alcohol	aq	nabh4,lialh4		硼氢化钠把醛还原成伯醇
ketone	secondary_alcohol	aq	nabh4,lialh4		硼氢化钠把酮还原成仲醇
halogenoalkane	primary_alcohol	aq	oh-	halide	加热后卤代烃水解，生成醇
halogenoalkane	alkene	ethanol	oh-	halide	乙醇溶液中加热发生消去，产生烯烃
halogenoalkane	nitrile	ethanol	cn-	halide	氰离子取代卤素，碳链增长一个碳
halogenoalkane	primary_amine	ethanol	nh3	halide	氨取代卤素，得到胺
alkene	halogenoalkane		hbr,hcl,hi		烯烃与卤化氢加成
nitrile	carboxylic_acid	reflux	h2o,h+	nh4+	腈在酸中回流水解成羧酸
acyl_chloride	carboxylic_acid	aq	h2o	hcl	酰氯遇水剧烈水解，白雾，生成羧酸
acyl_chloride	ester	aq	primary_alcohol	hcl	酰氯与醇反应生成酯（比酯化剧烈得多）
acyl_chloride	amide	aq	nh3,primary_amine	hcl	酰氯与氨或胺反应生成酰胺
arene	nitroarene	conc	hno3	h2o	浓硝酸浓硫酸混合酸，50-60℃水浴，生成淡黄色油状硝基苯
arene	halogenoarene	catal	br2	hbr	铁粉或溴化铁催化下发生溴代，产生白雾
nitroarene	primary_amine	reflux	fe,hcl	h2o	锡与盐酸还原硝基，得到胺
primary_amine	diazonium	aq	hno2	h2o	5℃以下与亚硝酸反应生成重氮盐
diazonium	azo	aq	phenol	hcl	重氮盐与苯酚偶联，生成橙色偶氮染料
carboxylic_acid	ester	reflux	primary_alcohol	h2o	浓硫酸催化回流，生成有果香味的酯
";

/* ---------- 家族映射：家族 反应物id 产物id ---------- */
public const string TblOrganicMap = @"
primary_alcohol	ch3oh	aldehyde	ch2o
primary_alcohol	c2h5oh	aldehyde	ch3cho
primary_alcohol	ch3ch2ch2oh	aldehyde	c2h5cho
primary_alcohol	ch3oh	carboxylic_acid	hcooh
primary_alcohol	c2h5oh	carboxylic_acid	ch3cooh
primary_alcohol	ch3ch2ch2oh	carboxylic_acid	ch3ch2cooh
secondary_alcohol	ch3chohch3	ketone	acetone
aldehyde	ch2o	carboxylic_acid	hcooh
aldehyde	ch3cho	carboxylic_acid	ch3cooh
aldehyde	c2h5cho	carboxylic_acid	ch3ch2cooh
aldehyde	ch2o	primary_alcohol	ch3oh
aldehyde	ch3cho	primary_alcohol	c2h5oh
aldehyde	c2h5cho	primary_alcohol	ch3ch2ch2oh
ketone	acetone	secondary_alcohol	ch3chohch3
halogenoalkane	c2h5br	primary_alcohol	c2h5oh
halogenoalkane	c2h5cl	primary_alcohol	c2h5oh
halogenoalkane	c2h5i	primary_alcohol	c2h5oh
halogenoalkane	ch3i	primary_alcohol	ch3oh
halogenoalkane	ch3cl	primary_alcohol	ch3oh
halogenoalkane	c2h5br	alkene	c2h4
halogenoalkane	c2h5cl	alkene	c2h4
halogenoalkane	c2h5i	alkene	c2h4
halogenoalkane	c2h5br	nitrile	c2h5cn
halogenoalkane	ch3i	nitrile	ch3cn
halogenoalkane	c2h5br	primary_amine	c2h5nh2
halogenoalkane	ch3i	primary_amine	ch3nh2
nitrile	ch3cn	carboxylic_acid	ch3cooh
nitrile	c2h5cn	carboxylic_acid	ch3ch2cooh
acyl_chloride	ch3cocl	carboxylic_acid	ch3cooh
acyl_chloride	c6h5cocl	carboxylic_acid	c6h5cooh
acyl_chloride	ch3cocl	ester	ch3cooc2h5
acyl_chloride	ch3cocl	amide	ch3conh2
arene	c6h6	nitroarene	c6h5no2
arene	c6h6	halogenoarene	c6h5br
nitroarene	c6h5no2	primary_amine	aniline
primary_amine	aniline	diazonium	c6h5n2cl
diazonium	c6h5n2cl	azo	azo_benzene
carboxylic_acid	ch3cooh	ester	ch3cooc2h5
carboxylic_acid	ch3cooh_glacial	ester	ch3cooc2h5
";

/* ---------- 本地化：物质id 英文名 ---------- */
public const string TblI18nSub = @"
na	Sodium
k	Potassium
mg	Magnesium
al	Aluminium
zn	Zinc
fe	Iron
cu	Copper
ag	Silver
pb	Lead
sn	Tin
ca	Calcium
ba	Barium
ni	Nickel
hg	Mercury
au	Gold
pt	Platinum
cuo_powder	Copper(II) oxide powder
fe_metal	Iron nail
c	Carbon (graphite)
s	Sulfur
p_red	Red phosphorus
p_white	White phosphorus
i2	Iodine
br2	Bromine
cl2	Chlorine
h2	Hydrogen
o2	Oxygen
n2	Nitrogen
na2o	Sodium oxide
cao	Calcium oxide
mgo	Magnesium oxide
cuo	Copper(II) oxide
fe2o3	Iron(III) oxide
fe3o4	Iron(II,III) oxide
al2o3	Aluminium oxide
zno	Zinc oxide
mno2	Manganese(IV) oxide
sio2	Silicon dioxide
so2	Sulfur dioxide
co2	Carbon dioxide
co	Carbon monoxide
no2	Nitrogen dioxide
no	Nitrogen monoxide
p2o5	Phosphorus(V) oxide
cr2o3	Chromium(III) oxide
pbo	Lead(II) oxide
cu2o	Copper(I) oxide
na2o2	Sodium peroxide
so3	Sulfur trioxide
p4o10	Phosphorus(V) oxide
hcl	Dilute hydrochloric acid
hcl_conc	Concentrated hydrochloric acid
h2so4	Dilute sulfuric acid
h2so4_conc	Concentrated sulfuric acid
hno3	Dilute nitric acid
hno3_conc	Concentrated nitric acid
h3po4	Phosphoric acid
h2co3	Carbonic acid
ch3cooh	Ethanoic acid
ch3cooh_glacial	Glacial ethanoic acid
h2s	Hydrogen sulfide solution
hbr	Hydrobromic acid
hi	Hydroiodic acid
h2c2o4	Ethanedioic acid
hf	Hydrofluoric acid
hclo	Hypochlorous acid
h2sio3	Silicic acid
hno2	Nitrous acid
ch3ch2cooh	Propanoic acid
c6h5cooh	Benzoic acid
naoh	Sodium hydroxide
koh	Potassium hydroxide
caoh2	Calcium hydroxide
baoh2	Barium hydroxide
nh3	Aqueous ammonia
cuoh2	Copper(II) hydroxide
feoh3	Iron(III) hydroxide
feoh2	Iron(II) hydroxide
aloh3	Aluminium hydroxide
mgoh2	Magnesium hydroxide
znoh2	Zinc hydroxide
naoh_s	Sodium hydroxide (solid)
pboh2	Lead(II) hydroxide
mnoh2	Manganese(II) hydroxide
croh3	Chromium(III) hydroxide
nacl	Sodium chloride
kcl	Potassium chloride
cacl2	Calcium chloride
bacl2	Barium chloride
mgcl2	Magnesium chloride
alcl3	Aluminium chloride
fecl2	Iron(II) chloride
fecl3	Iron(III) chloride
cucl2	Copper(II) chloride
zncl2	Zinc chloride
agno3	Silver nitrate
kno3	Potassium nitrate
nano3	Sodium nitrate
pbno32	Lead(II) nitrate
cuno32	Copper(II) nitrate
na2co3	Sodium carbonate
nahco3	Sodium hydrogencarbonate
k2co3	Potassium carbonate
caco3	Calcium carbonate
baco3	Barium carbonate
na2so4	Sodium sulfate
cuso4	Copper(II) sulfate
cuso45h2o	Copper(II) sulfate pentahydrate
feso4	Iron(II) sulfate
fe2so43	Iron(III) sulfate
znso4	Zinc sulfate
mgso4	Magnesium sulfate
baso4	Barium sulfate
pbso4	Lead(II) sulfate
caso4	Calcium sulfate
ki	Potassium iodide
kbr	Potassium bromide
kmno4	Potassium manganate(VII)
k2cr2o7	Potassium dichromate(VI)
nh4cl	Ammonium chloride
nh42so4	Ammonium sulfate
na3po4	Sodium phosphate
na2s	Sodium sulfide
fes	Iron(II) sulfide
cus	Copper(II) sulfide
zns	Zinc sulfide
agcl	Silver chloride
agbr	Silver bromide
agi	Silver iodide
pbi2	Lead(II) iodide
cuco3	Basic copper(II) carbonate
na2sio3	Sodium silicate
naclo	Sodium chlorate(I)
kscn	Potassium thiocyanate
fes2	Iron(II) disulfide
al2so43	Aluminium sulfate
nabr	Sodium bromide
nai	Sodium iodide
mgno32	Magnesium nitrate
nh4no3	Ammonium nitrate
baso3	Barium sulfite
kclo3	Potassium chlorate(V)
cahco32	Calcium hydrogencarbonate
al2s3	Aluminium sulfide
k2mno4	Potassium manganate(VI)
pbs	Lead(II) sulfide
ag2s	Silver sulfide
mgco3	Magnesium carbonate
znco3	Zinc carbonate
feco3	Iron(II) carbonate
pbco3	Lead(II) carbonate
ag2co3	Silver carbonate
caso3	Calcium sulfite
casio3	Calcium silicate
basio3	Barium silicate
mns	Manganese(II) sulfide
hgs	Mercury(II) sulfide
pbcro4	Lead(II) chromate(VI)
bacro4	Barium chromate(VI)
ag2cro4	Silver chromate(VI)
alco3	Aluminium carbonate
nano2	Sodium nitrite
na2s2o3	Sodium thiosulfate
k2s2o8	Potassium peroxydisulfate
kcn	Potassium cyanide
cano32	Calcium nitrate
bano32	Barium nitrate
ch3coona	Sodium ethanoate
ch4	Methane
c2h6	Ethane
c2h4	Ethene
c2h2	Ethyne
c3h8	Propane
c4h10	Butane
c3h6	Propene
c6h6	Benzene
c7h8	Methylbenzene
ch3oh	Methanol
c2h5oh	Ethanol
c3h8o3	Propane-1,2,3-triol
phoh	Phenol
ch2o	Methanal
ch3cho	Ethanal
acetone	Propanone
hcooh	Methanoic acid
c17h35cooh	Stearic acid
ch3cooc2h5	Ethyl ethanoate
glucose	Glucose
sucrose	Sucrose
starch	Starch
cellulose	Cellulose
urea	Urea
aniline	Phenylamine
ch3nh2	Methylamine
chcl3	Trichloromethane
ccl4	Tetrachloromethane
c2h5br	Bromoethane
ch3i	Iodomethane
glycine	Glycine
oil	Vegetable oil
vitc	Vitamin C (ascorbic acid)
c6h6o6	Dehydroascorbic acid
c6h12o7	Gluconic acid
c6h5br	Bromobenzene
c6h5no2	Nitrobenzene
c2h4br2	1,2-Dibromoethane
c2h2br4	1,1,2,2-Tetrabromoethane
ch3cl	Chloromethane
c17h33coona	Sodium stearate
c2h5ona	Sodium ethoxide
c6h2br3oh	2,4,6-Tribromophenol
c6h5ona	Sodium phenoxide
c6h5oh_aq	Phenol solution
c2h5cl	Chloroethane
c2h5i	Iodoethane
ch3ch2ch2oh	Propan-1-ol
ch3chohch3	Propan-2-ol
c2h5cho	Propanal
ch3cooch3	Methyl ethanoate
c2h5nh2	Ethylamine
ch3cn	Ethanenitrile
c2h5cn	Propanenitrile
ch3conh2	Ethanamide
c6h5ch3	Methylbenzene
c6h5cl	Chlorobenzene
ch3cocl	Ethanoyl chloride
c6h5cocl	Benzoyl chloride
azo_benzene	Azobenzene
peptide_gly	Dipeptide (glycylglycine)
h2o	Water
h2o2	Hydrogen peroxide
litmus	Litmus solution
phenolphthalein	Phenolphthalein solution
methylorange	Methyl orange solution
iodine_sol	Iodine solution
limewater	Limewater
sif4	Silicon tetrafluoride
fescn3	Iron(III) thiocyanate
cunh34	Tetraamminecopper(II) ion
agnh32	Diamminesilver(I) ion
fec6h5o3	Iron(III) phenoxide
hcn	Hydrogen cyanide
pcl3	Phosphorus trichloride
pcl5	Phosphorus pentachloride
socl2	Thionyl chloride
nabh4	Sodium borohydride
lialh4	Lithium aluminium hydride
sicl4	Silicon tetrachloride
c6h5n2cl	Benzenediazonium chloride
[Al(OH)4]-	Tetrahydroxoaluminate ion
[CuCl4]2-	Tetrachlorocuprate(II) ion
[Cu(H2O)6]2+	Hexaaquacopper(II) ion
[Co(H2O)6]2+	Hexaaquacobalt(II) ion
[CoCl4]2-	Tetrachlorocobaltate(II) ion
scn-	Thiocyanate ion
cacl2_	Calcium chloride (duplicate stub entry)
nitrobenzene_	Nitrobenzene (duplicate stub entry)
nh2ch2cooh_	Glycine (duplicate stub entry)
phenylamine_	Phenylamine (duplicate stub entry)
ch3oh_	Methanol (duplicate stub entry)
";

/* ---------- 英文物质名补全：扩展有机与新增无机物种 ---------- */
public const string TblI18nSubAdditional = @"
pentanoic_acid	Pentanoic acid
hexanoic_acid	Hexanoic acid
heptanoic_acid	Heptanoic acid
octanoic_acid	Octanoic acid
pentanal	Pentanal
hexanal	Hexanal
heptanal	Heptanal
octanal	Octanal
pent_1_ene	Pent-1-ene
hex_1_ene	Hex-1-ene
hept_1_ene	Hept-1-ene
oct_1_ene	Oct-1-ene
n_pentane	Pentane
n_hexane	Hexane
n_heptane	Heptane
n_octane	Octane
pentan_1_ol	Pentan-1-ol
hexan_1_ol	Hexan-1-ol
heptan_1_ol	Heptan-1-ol
octan_1_ol	Octan-1-ol
n2o4	Dinitrogen tetroxide
n2o4	Dinitrogen tetroxide
nh42co3	Ammonium carbonate
bacl22h2o	Barium chloride dihydrate
CH3CH(OH)COO-	Lactate ion
CH3CHBrCOO-	2-bromopropanoate ion
ch3chbrcooh	2-bromopropanoic acid
cr2so43	Chromium(III) sulfate
znco3znoh2	Basic zinc carbonate
Cr(OH)4-	Tetrahydroxidochromate(III) ion
[Zn(NH3)4]2+	Tetraamminezinc(II) ion
NO2-	Nitrite ion
crcl3	Chromium(III) chloride
mnso4	Manganese(II) sulfate
na2so3	Sodium sulfite
nh4br	Ammonium bromide
kio3	Potassium iodate
IO3-	Iodate ion
S4O6^2-	Tetrathionate ion
na2s4o6	Sodium tetrathionate
s2o8^2-	Peroxodisulfate ion
na2s2o8	Sodium peroxodisulfate
poCl3	Phosphoryl chloride
h3po3	Phosphorous acid
bromobenzene	Bromobenzene
chlorobenzene	Chlorobenzene
nitrobenzene	Nitrobenzene
methylbenzene	Methylbenzene
phenylethanone	Phenylethanone
benzoic_acid	Benzoic acid
cyclohexane	Cyclohexane
benzenediazonium	Benzenediazonium ion
phenyl_ester	Phenyl ester
phenoxide	Phenoxide ion
cui	Copper(I) iodide
chi3	Iodoform
c2o4^2-	Oxalate ion
hydroxynitrile	Hydroxynitrile
dibromoalkane	Dibromoalkane
vicinaldiol	Vicinal diol
dnph_hydrazone	2,4-dinitrophenylhydrazone
rono2	Alkyl nitrate
rona	Sodium alkoxide
rch2oh	Primary alcohol
srco3	Strontium carbonate
sro	Strontium oxide
bao	Barium oxide
ca(no3)2	Calcium nitrate
Co2+	Cobalt(II) ion
Co(OH)2	Cobalt(II) hydroxide
[Co(NH3)6]2+	Hexamminecobalt(II) ion
c6h5ch2cl	Benzyl chloride
c6h2br3nh2	2,4,6-tribromoaniline
azo_dye	4-hydroxyazobenzene
ch3cooc6h5	Phenyl ethanoate
C2H5NH3+	Ethylammonium ion
glycinate	Glycinate ion
glycine_h	Protonated glycine
gly_gly	Glycylglycine
nascent_o	Nascent oxygen
butanal	Butanal
";

/* ---------- 本地化：中文原文 英文译文 ---------- */
public const string TblI18nText = @"
酸碱中和，溶液温度略微升高	Acid–base neutralisation; the solution temperature rises slightly
氨水的刺激性气味明显减弱	The pungent smell of ammonia is noticeably reduced
中和反应，无明显外观变化	Neutralisation occurs; there is no visible change
立即产生大量气泡，逸出无色无味气体	Effervescence starts immediately and a colourless, odourless gas is evolved
迅速产生大量气泡，气体使澄清石灰水变浑浊	Rapid effervescence; the gas turns limewater milky
产生有臭鸡蛋气味的气体	A gas with the smell of rotten eggs is evolved
固体表面立即产生大量气泡，固体逐渐溶解	Effervescence immediately on the solid surface; the solid gradually dissolves
产生大量气泡，白色固体溶解	Effervescence; the white solid dissolves
产生大量气泡	Effervescence
产生大量气泡，溶液呈浅绿色	Effervescence; the solution turns pale green
绿色固体溶解，产生气泡，溶液变蓝	The green solid dissolves, a gas is evolved and the solution turns blue
产生有刺激性气味的气体	A gas with a pungent smell is evolved
白色沉淀溶解，产生刺激性气体	The white precipitate dissolves and a pungent gas is evolved
产生白色胶状沉淀	A white gelatinous precipitate forms
缓慢产生气泡（醋酸酸性弱于盐酸）	Slow effervescence (ethanoic acid is a weaker acid than hydrochloric acid)
产生白色胶状沉淀，久置逐渐凝聚	A white gelatinous precipitate forms which slowly coagulates on standing
产生白色胶状沉淀，继续加酸沉淀溶解	A white gelatinous precipitate forms; it dissolves when more acid is added
产生白色沉淀	A white precipitate forms
产生黄绿色有刺激性气味的气体	A yellow-green gas with a pungent smell is evolved
锌粒表面产生大量气泡，锌逐渐溶解，溶液温度升高	Effervescence on the zinc surface; the zinc gradually dissolves and the solution warms.
镁条剧烈反应，产生大量气泡并迅速溶解，试管烫手	The magnesium ribbon reacts vigorously, effervescing strongly and dissolving rapidly; the tube becomes hot
铁表面产生气泡，溶液逐渐变为浅绿色	Effervescence on the iron surface; the solution gradually turns pale green
铝表面氧化膜溶解后开始剧烈冒气泡	Once the oxide layer has dissolved the aluminium effervesces vigorously
剧烈反应，产生大量气泡	Vigorous reaction with strong effervescence
反应较缓慢，缓慢产生气泡	The reaction is slow, with gentle effervescence
钠浮在水面上，熔成闪亮的小球四处游动，发出嘶嘶声，滴入酚酞溶液变红	The sodium floats on the water and melts into a shiny ball that darts about with a hissing sound; phenolphthalein turns red
钾与水剧烈反应，立即燃烧并发生轻微爆炸，火焰呈紫色	Potassium reacts violently with water, igniting at once with a slight explosion and a lilac flame
钙粒表面产生气泡，逐渐溶解，溶液变浑浊	Effervescence on the calcium granules, which gradually dissolve; the solution turns cloudy
镁条表面缓慢产生细小气泡（加热时明显加快）	Small bubbles form slowly on the magnesium ribbon (much faster on heating)
锌表面覆盖一层红色的铜，蓝色溶液逐渐变浅直至褪色	The zinc becomes coated with red copper and the blue solution fades until it is colourless
铁钉表面附着红色物质，蓝色溶液变为浅绿色	A red solid deposits on the iron nail and the blue solution turns pale green
铝表面析出红色铜，蓝色褪去	Red copper deposits on the aluminium and the blue colour fades
镁条表面迅速变红，溶液褪色	The magnesium ribbon quickly turns red and the solution loses its colour
铜丝表面析出银白色晶体，溶液变蓝	Silvery crystals deposit on the copper wire and the solution turns blue
铁表面析出银白色金属，溶液变浅绿色	A silvery metal deposits on the iron and the solution turns pale green
锌表面析出银白色金属	A silvery metal deposits on the zinc
棕黄色溶液逐渐变为浅绿色	The brown-yellow solution gradually turns pale green
铜溶解，溶液由棕黄变为蓝绿色	The copper dissolves and the solution turns from brown-yellow to blue-green
铅表面附着红色物质	A red solid deposits on the lead
溶液变为棕黄色，加入淀粉溶液后变蓝	The solution turns brown-yellow and turns blue on adding starch solution
溶液变为橙黄色	The solution turns orange-yellow
溶液由橙色变为棕黄色	The solution turns from orange to brown-yellow
黑色粉末逐渐溶解，溶液变为蓝色	The black powder gradually dissolves and the solution turns blue
红棕色粉末溶解，溶液变成棕黄色	The red-brown powder dissolves and the solution turns brown-yellow
黑色固体溶解，溶液呈黄绿色	The black solid dissolves giving a yellow-green solution
白色固体溶解，溶液温度上升	The white solid dissolves and the solution warms
固体溶解并放热	The solid dissolves and heat is released
白色固体逐渐溶解	The white solid gradually dissolves
白色固体缓慢溶解	The white solid dissolves slowly
黄色固体溶解	The yellow solid dissolves
加热后产生黄绿色刺激性气体，溶液变为浅粉色	On heating a pungent yellow-green gas is evolved and the solution turns a very pale pink
白色固体溶解，得到无色溶液	The white solid dissolves to give a colourless solution
白色固体溶解	The white solid dissolves
玻璃表面被腐蚀变毛，产生气体	The glass surface is etched and frosted, and a gas is evolved
绿色固体溶解，溶液呈绿色	The green solid dissolves giving a green solution
固体剧烈水解放热，溶液显强碱性	The solid hydrolyses vigorously with much heat and the solution is strongly alkaline
与水反应放出大量热，水沸腾冒白雾，块状固体崩解成粉末	The reaction with water releases much heat; the water boils with white fumes and the lump crumbles to a powder
气体溶解，溶液显酸性	The gas dissolves and the solution is acidic
气体溶解，溶液显弱酸性	The gas dissolves and the solution is weakly acidic
白色沉淀逐渐溶解，溶液变澄清	The white precipitate gradually dissolves and the solution clears
白色沉淀溶解	The white precipitate dissolves
铝表面产生大量气泡，说明铝具有两性	Strong effervescence on the aluminium, showing that aluminium is amphoteric
蓝色沉淀溶解，溶液变蓝	The blue precipitate dissolves giving a blue solution
红褐色沉淀溶解，溶液呈棕黄色	The reddish-brown precipitate dissolves giving a brown-yellow solution
产生有刺激性气味的气体，湿润的红色石蕊试纸变蓝	A pungent gas is evolved which turns damp red litmus paper blue
加热时放出刺激性气体，试纸变蓝	On heating a pungent gas is evolved which turns the indicator paper blue
紫红色迅速褪去，产生大量气泡，溶液变为近无色	The purple colour is rapidly decolourised, strong effervescence occurs and the solution becomes almost colourless
紫红色褪去，产生气泡（加热后反应加快）	The purple colour is decolourised and a gas is evolved (the reaction speeds up on heating)
紫红色褪去，产生棕黑色沉淀	The purple colour is decolourised and a brown-black precipitate forms
橙色变为绿色，同时产生气泡	The orange colour changes to green and a gas is evolved at the same time
橙色变为绿色	The orange colour changes to green
浅绿色溶液变为棕黄色	The pale green solution turns brown-yellow
溶液变为棕黄色，滴加淀粉变蓝	The solution turns brown-yellow and turns blue when starch is added
溶液变棕黄，加入淀粉后呈蓝色	The solution turns brown-yellow and blue after adding starch
产生大量气泡，带火星的木条复燃	Strong effervescence; a glowing splint relights
加热后铜丝溶解，产生刺激性气味气体，溶液变蓝	On heating the copper dissolves, a pungent gas is evolved and the solution turns blue
铜剧烈溶解，产生红棕色气体，溶液变蓝绿色	The copper dissolves vigorously giving a red-brown gas and the solution turns blue-green
铜溶解，产生无色气体，遇空气立即变为红棕色，溶液变蓝	The copper dissolves and a colourless gas is evolved which turns red-brown in air; the solution turns blue
银溶解，产生红棕色气体	The silver dissolves and a red-brown gas is evolved
需在催化剂和加热条件下进行	Requires a catalyst and heating
无色气体立即变为红棕色	The colourless gas immediately turns red-brown
产生淡黄色浑浊，析出硫单质	A pale yellow cloudiness appears as sulfur is deposited
产生淡黄色浑浊	A pale yellow cloudiness appears
加热时木炭与浓硫酸剧烈反应，放出刺激性气体	On heating the charcoal reacts vigorously with concentrated sulfuric acid, releasing a pungent gas
加热分解，放出气体	Decomposes on heating with evolution of a gas
碘水的棕黄色迅速褪去	The brown-yellow colour of the iodine solution is rapidly decolourised
氨水过量后蓝色沉淀溶解，溶液变为深蓝色（四氨合铜配离子）	The blue precipitate dissolves in excess ammonia giving a deep blue solution (tetraamminecopper(II) ion)
蓝色沉淀逐渐溶解，溶液变成深蓝色	The blue precipitate gradually dissolves and the solution turns deep blue
白色沉淀溶解，得到无色溶液	The white precipitate dissolves to give a colourless solution
先出现白色浑浊，氨水过量后溶解，得到无色银氨溶液	A white cloudiness appears first, then dissolves in excess ammonia giving colourless Tollens reagent
溶液立即变为血红色	The solution immediately turns blood-red
溶液显紫色，这是苯酚的特征反应	The solution turns purple; this is the characteristic reaction of phenol
淡黄色沉淀缓慢溶解（比氯化银难溶得多）	The pale yellow precipitate dissolves slowly (it is much less soluble than silver chloride)
氢气安静燃烧，产生淡蓝色火焰，管壁出现水雾	The hydrogen burns quietly with a pale blue flame and mist condenses on the tube wall
甲烷安静燃烧，产生明亮的淡蓝色火焰	The methane burns quietly with a bright pale blue flame
乙炔燃烧产生明亮火焰并伴有黑烟	The ethyne burns with a bright, sooty flame
木炭在氧气中剧烈燃烧，发出白光，放出大量热	The charcoal burns vigorously in oxygen with a white glow, releasing much heat
硫在氧气中燃烧，发出明亮的蓝紫色火焰，产生刺激性气味气体	The sulfur burns in oxygen with a bright blue-purple flame, giving a pungent gas
红磷燃烧产生大量白烟，放出大量热	The red phosphorus burns with much white smoke, releasing much heat
镁条剧烈燃烧，发出耀眼白光，生成白色固体	The magnesium ribbon burns vigorously with a dazzling white light, forming a white solid
铁丝在氧气中剧烈燃烧，火星四射，生成黑色固体	The iron wire burns vigorously in oxygen with sparks flying off, forming a black solid
铜丝表面逐渐变黑	The copper wire gradually turns black
钠表面迅速变暗，生成白色固体	The sodium surface quickly tarnishes, forming a white solid
钠在氧气中燃烧，产生黄色火焰，生成淡黄色固体	Sodium burns in oxygen with a yellow flame, forming a pale yellow solid
一氧化碳燃烧产生蓝色火焰	Carbon monoxide burns with a blue flame
加热后混合物红热，生成黑色固体	On heating the mixture glows red-hot and a black solid forms
剧烈反应，生成灰黑色固体	Vigorous reaction forming a grey-black solid
燃烧产生淡蓝色火焰	Burns with a pale blue flame
高温下分解，剩余固体变为白色疏松状	Decomposes at a high temperature, leaving a white friable solid
蓝色晶体逐渐变为白色粉末，试管口有水珠	The blue crystals gradually turn to a white powder and droplets of water collect at the mouth of the tube
加热放出气体，试管口有水珠	Heating releases a gas and droplets of water collect at the mouth of the tube
蓝色沉淀变为黑色固体	The blue precipitate turns to a black solid
红褐色沉淀变为红棕色固体	The reddish-brown precipitate turns to a red-brown solid
白色沉淀变为白色固体	The white precipitate turns to a white solid
固体逐渐减少，试管口有白色晶体附着	The solid gradually disappears and white crystals deposit on the cooler part of the tube
紫黑色固体变为黑色，放出能使带火星木条复燃的气体	The purple-black solid turns black and gives off a gas that relights a glowing splint
需要二氧化锰作催化剂，加热放出氧气	Manganese(IV) oxide is needed as a catalyst; heating releases oxygen
加热后溶液变浑浊，产生气泡	On heating the solution turns cloudy and a gas is evolved
碳酸不稳定，立即分解放出气体	Carbonic acid is unstable and decomposes at once, releasing a gas
浓硝酸见光分解，产生红棕色气体	Concentrated nitric acid decomposes in light, giving a red-brown gas
见光逐渐分解，固体变黑	Decomposes gradually in light and the solid turns black
加热后产生有果香味的油状液体，浮在液面上	On heating a sweet-smelling oily liquid forms and floats on the surface
乙醇中投入钠粒，缓慢产生气泡，钠沉底	Sodium added to ethanol sinks to the bottom and effervesces slowly
浓硫酸催化下加热到 170℃，产生能使溴水褪色的气体	Heated to 170 °C with concentrated sulfuric acid as catalyst, giving a gas that decolourises bromine water
乙醛被氧化，溶液酸性增强	The ethanal is oxidised and the solution becomes more acidic
试管内壁附着一层光亮如镜的银，形成银镜	A shiny mirror of silver deposits on the inside of the tube, forming a silver mirror
出现砖红色沉淀	A brick-red precipitate forms
出现砖红色沉淀，说明葡萄糖有还原性	A brick-red precipitate forms, showing that glucose is a reducing sugar
试管内壁形成光亮的银镜	A bright silver mirror forms on the inside of the tube
在铁粉催化下发生取代，产生白雾，液体分层	Substitution occurs with iron powder as catalyst; white fumes form and the liquid separates into two layers
浓硫酸催化下加热，生成有苦杏仁气味的淡黄色油状液体	Heated with concentrated sulfuric acid as catalyst, giving a pale yellow oily liquid with the smell of bitter almonds
溴的四氯化碳溶液迅速褪色	The bromine in tetrachloromethane is rapidly decolourised
溴水迅速褪色	The bromine water is rapidly decolourised
催化剂加热条件下发生加成	Addition occurs with a catalyst and heating
催化剂加热下与氢气加成	Adds hydrogen with a catalyst and heating
黄绿色逐渐褪去，管壁出现油状液滴，产生白雾	The yellow-green colour fades, oily droplets form on the tube wall and white fumes appear
加热后溴乙烷层逐渐减少，生成乙醇	On heating the bromoethane layer gradually decreases as ethanol is formed
加热搅拌后混合物变稠，不再分层，发生皂化反应	On heating and stirring the mixture thickens and no longer separates; saponification has occurred
立即产生白色沉淀（三溴苯酚）	A white precipitate forms immediately (2,4,6-tribromophenol)
缓慢产生气泡	Gentle effervescence
产生大量气泡，说明乙酸酸性强于碳酸	Strong effervescence, showing that ethanoic acid is a stronger acid than carbonic acid
甲酸也能发生银镜反应	Methanoic acid also gives a silver mirror
立即产生黑色沉淀	A black precipitate forms immediately
产生黑色沉淀	A black precipitate forms
澄清石灰水变浑浊，产生白色沉淀	The limewater turns milky as a white precipitate forms
黄色溶液变为橙色	The yellow solution turns orange
橙色溶液变为黄色	The orange solution turns yellow
淀粉遇碘立即变深蓝色，加热后蓝色褪去，冷却后又复现	Starch turns deep blue at once with iodine; the blue colour disappears on heating and returns on cooling
无色酚酞溶液变红，说明溶液显碱性	The colourless phenolphthalein turns red, showing that the solution is alkaline
酚酞在酸性或中性溶液中不变色	Phenolphthalein is colourless in acidic or neutral solution
紫色石蕊溶液变红，说明溶液显酸性	The purple litmus solution turns red, showing that the solution is acidic
紫色石蕊溶液变蓝，说明溶液显碱性	The purple litmus solution turns blue, showing that the solution is alkaline
甲基橙溶液变红	The methyl orange solution turns red
甲基橙溶液变黄	The methyl orange solution turns yellow
溶液立即显紫色，这是苯酚的特征反应	The solution immediately turns purple; this is the characteristic reaction of phenol
白色沉淀迅速变为灰绿色，最终变成红褐色（被空气氧化）	The white precipitate quickly turns grey-green and finally reddish-brown (oxidised by air)
溶液立即变成血红色	The solution immediately turns blood-red
白色絮状	white flocculent
淡黄色	pale yellow
黄色	yellow
白色	white
白色微溶	white, slightly soluble
蓝绿色	blue-green
蓝色絮状	blue flocculent
白色（迅速变灰绿）	white (rapidly turning grey-green)
红褐色絮状	reddish-brown flocculent
白色胶状	white gelatinous
金黄色	golden yellow
砖红色	brick-red
黑色	black
绿色	green
灰白色	greyish-white
灰绿色	grey-green
肉色	flesh-coloured
棕黑色	brown-black
极活泼，遇水剧烈反应并熔成小球，煤油保存	Very reactive; reacts violently with water, melting into a shiny ball; stored under paraffin oil
遇水剧烈反应甚至燃烧，紫色焰色	Reacts violently with water and may ignite; lilac flame colour
燃烧发出耀眼白光	Burns with a dazzling white light
两性金属，表面致密氧化膜	Amphoteric metal with a tough surface oxide layer
与稀酸反应放出氢气	Reacts with dilute acids releasing hydrogen
常见还原剂，铁粉为黑色	Common reducing agent; iron powder is black
紫红色，不与非氧化性酸反应	Reddish-brown; does not react with non-oxidising acids
重金属，有毒	Heavy metal; toxic
遇水反应放氢，砖红色焰色	Reacts with water releasing hydrogen; brick-red flame colour
有毒，黄绿色焰色	Toxic; yellow-green flame colour
常温液态金属，剧毒，易挥发	Liquid metal at room temperature; highly toxic and volatile
化学性质极稳定	Chemically very unreactive
催化剂	Catalyst
黑色粉末	Black powder
石墨，导电，还原剂	Graphite; conducts electricity; reducing agent
淡黄色，燃烧呈蓝紫色火焰	Pale yellow; burns with a blue-purple flame
剧毒，自燃，必须水中保存	Highly toxic and spontaneously flammable; must be stored under water
升华产生紫色蒸气，遇淀粉变蓝	Sublimes to a purple vapour; turns starch blue
深红棕色液体，易挥发，强腐蚀	Dark red-brown volatile liquid; strongly corrosive
黄绿色，刺激性气味，有毒	Yellow-green; pungent; toxic
无色无味，可燃，点燃有爆鸣声	Colourless and odourless; flammable, burns with a squeaky pop
无色无味，助燃	Colourless and odourless; supports combustion
化学性质稳定	Chemically unreactive
遇水生成氢氧化钠	Forms sodium hydroxide with water
生石灰，遇水放出大量热	Quicklime; releases much heat with water
白色疏松粉末	White friable powder
红棕色，铁锈主要成分	Red-brown; main component of rust
黑色，有磁性	Black and magnetic
两性氧化物	Amphoteric oxide
催化剂，也是氧化剂	Catalyst and oxidising agent
沙子，能与氢氟酸反应	Sand; reacts with hydrofluoric acid
无色有刺激性气味，有漂白性	Colourless with a pungent smell; bleaching agent
无色无味，使澄清石灰水变浑浊	Colourless and odourless; turns limewater milky
无色无味，剧毒，可燃	Colourless and odourless; highly toxic and flammable
红棕色，有刺激性气味，有毒	Red-brown with a pungent smell; toxic
无色，遇空气立即变红棕色	Colourless; turns red-brown immediately in air
白色固体，强吸水性	White solid; very hygroscopic
绿色颜料	Green pigment
黄色，有毒	Yellow; toxic
强酸，易挥发，有刺激性气味	Strong acid; volatile with a pungent smell
浓盐酸，瓶口有白雾	Concentrated; white fumes at the bottle mouth
强酸，不挥发	Strong, non-volatile acid
强吸水性和脱水性，稀释时剧烈放热	Strongly hygroscopic and dehydrating; dilution is highly exothermic
具有强氧化性	Strong oxidising agent
浓硝酸，见光分解，瓶口有白雾	Concentrated; decomposes in light, white fumes at the bottle mouth
中强酸，不挥发	Moderately strong, non-volatile acid
弱酸，极不稳定易分解	Weak acid; very unstable and readily decomposes
弱酸，有刺激性酸味	Weak acid with a pungent sour smell
无水乙酸，17℃以下凝结成冰状晶体	Anhydrous ethanoic acid; freezes to ice-like crystals below 17 °C
有臭鸡蛋气味，具有还原性	Smell of rotten eggs; reducing agent
强酸	Strong acid
强酸，有强还原性	Strong acid; strong reducing agent
二元弱酸，有还原性	Weak dicarboxylic acid; reducing agent
弱酸但腐蚀玻璃，剧毒	Weak acid that attacks glass; highly toxic
弱酸，强氧化性，有漂白作用	Weak acid; strong oxidising and bleaching agent
强碱，易潮解，腐蚀性强	Strong base; deliquescent and strongly corrosive
强碱，易潮解	Strong base; deliquescent
微溶，澄清溶液即石灰水	Slightly soluble; the clear solution is limewater
可溶强碱，有毒	Soluble strong base; toxic
有刺激性气味，弱碱，易挥发	Pungent smell; weak base; volatile
蓝色絮状沉淀	Blue flocculent precipitate
红褐色絮状沉淀	Reddish-brown flocculent precipitate
白色沉淀，迅速变灰绿最终变红褐	White precipitate that quickly turns grey-green and finally reddish-brown
白色胶状沉淀，两性	White gelatinous amphoteric precipitate
白色沉淀	White precipitate
白色沉淀，两性	White amphoteric precipitate
食盐，焰色黄色	Table salt; yellow flame colour
焰色紫色（透过蓝色钴玻璃）	Lilac flame colour (viewed through cobalt blue glass)
常用干燥剂	Common drying agent
有毒，用于检验硫酸根	Toxic; used to test for sulfate ions
易水解	Readily hydrolysed
浅绿色溶液	Pale green solution
棕黄色溶液	Brown-yellow solution
蓝绿色溶液	Blue-green solution
见光分解，检验氯离子	Decomposes in light; used to test for chloride ions
氧化剂，焰色紫色	Oxidising agent; lilac flame colour
焰色黄色	Yellow flame colour
有毒	Toxic
蓝色溶液	Blue solution
纯碱，水解显碱性	Washing soda; hydrolyses to give an alkaline solution
小苏打，受热分解放二氧化碳	Baking soda; decomposes on heating releasing carbon dioxide
大理石、石灰石，不溶于水	Marble and limestone; insoluble in water
白色沉淀，可溶于酸	White precipitate; soluble in acid
白色粉末，溶于水显蓝色	White powder; gives a blue solution in water
蓝色晶体，加热失水变白	Blue crystals; lose water and turn white on heating
浅绿色，易被氧化变黄	Pale green; readily oxidised and turns yellow
黄色溶液	Yellow solution
泻盐	Epsom salts
白色沉淀，不溶于稀酸，钡餐	White precipitate; insoluble in dilute acid; used as a barium meal
微溶，石膏	Slightly soluble; gypsum
紫黑色，强氧化剂，溶液紫红色	Purple-black; strong oxidising agent giving a purple solution
橙色，强氧化剂（酸性条件下）	Orange; strong oxidising agent in acidic conditions
受热易分解，遇碱放氨	Decomposes on heating; releases ammonia with alkali
氮肥	Nitrogen fertiliser
水解显强碱性	Hydrolyses to give a strongly alkaline solution
有臭味，遇酸放硫化氢	Foul-smelling; releases hydrogen sulfide with acid
黑色块状，遇酸放硫化氢	Black lumps; releases hydrogen sulfide with acid
黑色沉淀，不溶于稀酸	Black precipitate; insoluble in dilute acid
白色沉淀，不溶于稀硝酸，感光变黑	White precipitate; insoluble in dilute nitric acid; darkens in light
淡黄色沉淀，感光	Pale yellow precipitate; light-sensitive
黄色沉淀，用于人工降雨	Yellow precipitate; used in cloud seeding
金黄色沉淀，溶于热水	Golden yellow precipitate; soluble in hot water
铜绿，孔雀石主要成分	Verdigris; main component of malachite
水玻璃，矿物胶	Water glass; mineral adhesive
漂白液有效成分	Active ingredient of household bleach
检验铁离子	Used to test for iron(III) ions
黄铁矿，工业制硫酸原料	Pyrite; raw material for the industrial manufacture of sulfuric acid
净水剂	Water-treatment flocculant
易被氧化析出碘	Readily oxidised, liberating iodine
受热分解甚至爆炸	Decomposes on heating and may explode
天然气主要成分，可燃	Main component of natural gas; flammable
催熟剂，能使溴水褪色	Ripening agent; decolourises bromine water
氧炔焰可达 3000℃	The oxy-ethyne flame reaches 3000 °C
液化石油气主要成分	Main component of LPG
打火机燃料	Lighter fuel
无色有特殊气味的液体，有毒，不溶于水	Colourless liquid with a characteristic smell; toxic and insoluble in water
有毒，误饮致盲，可燃	Toxic; causes blindness if swallowed; flammable
酒精，可燃，与钠反应放氢	Alcohol; flammable; reacts with sodium releasing hydrogen
丙三醇，粘稠，吸湿	Propane-1,2,3-triol; viscous and hygroscopic
有腐蚀性，遇氯化铁显紫色	Corrosive; gives a purple colour with iron(III) chloride
刺激性气味，水溶液即福尔马林	Pungent; its aqueous solution is formalin
易挥发，有刺激性气味	Volatile with a pungent smell
常见有机溶剂	Common organic solvent
蚁酸，有腐蚀性	Formic acid; corrosive
高级脂肪酸	Higher fatty acid
有果香味，微溶于水	Fruity smell; slightly soluble in water
还原性糖，可发生银镜反应	Reducing sugar; gives a silver mirror
非还原性糖，水解生成葡萄糖和果糖	Non-reducing sugar; hydrolyses to glucose and fructose
遇碘变蓝，水解最终生成葡萄糖	Turns blue with iodine; hydrolysis finally gives glucose
水解最终生成葡萄糖	Hydrolysis finally gives glucose
第一个人工合成的有机物	The first organic compound to be synthesised from inorganic starting materials
有毒，易被氧化变深	Toxic; readily oxidised and darkens
有鱼腥味，碱性	Fishy smell; alkaline
氯仿，不溶于水，密度大于水	Chloroform; insoluble in water and denser than water
不燃，曾用作灭火剂	Non-flammable; formerly used in fire extinguishers
密度大于水，可发生水解	Denser than water; undergoes hydrolysis
最简单的氨基酸，两性	The simplest amino acid; amphoteric
不溶于水，密度小于水，可发生皂化	Insoluble in water and less dense than water; undergoes saponification
强还原性，能使碘水褪色	Strong reducing agent; decolourises iodine solution
溶剂	Solvent
双氧水，见光分解，可放出氧气	Hydrogen peroxide; decomposes in light releasing oxygen
酸性变红，碱性变蓝	Red in acid, blue in alkali
遇碱变红，酸性和中性无色	Red in alkali; colourless in acidic and neutral solution
酸性变红，碱性变黄	Red in acid, yellow in alkali
遇淀粉变蓝	Turns blue with starch
用于检验二氧化碳	Used to test for carbon dioxide
无色刺激性气体，遇水水解	Colourless pungent gas; hydrolyses in water
白色沉淀，可溶于稀盐酸	White precipitate; soluble in dilute hydrochloric acid
砖红色沉淀，检验醛基	Brick-red precipitate; tests for the aldehyde group
白色胶状沉淀，加热失水成二氧化硅	White gelatinous precipitate; loses water on heating to give silicon dioxide
强氧化剂，与可燃物混合易爆	Strong oxidising agent; explosive when mixed with combustible material
淡黄色固体，与水或二氧化碳反应放出氧气	Pale yellow solid; releases oxygen with water or carbon dioxide
只存在于溶液中，加热分解	Exists only in solution; decomposes on heating
遇水完全水解，放出硫化氢	Hydrolyses completely in water releasing hydrogen sulfide
血红色，检验铁离子	Blood-red colour; tests for iron(III) ions
深蓝色，铜离子与过量氨水的特征产物	Deep blue; characteristic product of copper(II) ions with excess ammonia
银氨溶液，用于银镜反应	Tollens reagent; used for the silver mirror test
维生素C被氧化后的产物	Product formed when vitamin C is oxidised
葡萄糖被氧化的产物	Product formed when glucose is oxidised
无色油状液体，密度大于水，有特殊气味	Colourless oily liquid, denser than water, with a characteristic smell
苦杏仁气味，淡黄色油状液体，有毒	Smell of bitter almonds; pale yellow oily liquid; toxic
无色液体，密度大于水	Colourless liquid, denser than water
无色气体，光照下甲烷氯代的产物	Colourless gas; product of the chlorination of methane in light
肥皂的主要成分，水溶液显碱性	Main component of soap; its solution is alkaline
遇水强烈水解生成乙醇和氢氧化钠	Hydrolyses vigorously with water to give ethanol and sodium hydroxide
白色沉淀，苯酚的特征反应产物	White precipitate; characteristic product of phenol
溶于水显碱性	Soluble in water giving an alkaline solution
无色气体，遇水生成硫酸	Colourless gas; forms sulfuric acid with water
绿色，高锰酸钾受热分解产物	Green; product of the thermal decomposition of potassium manganate(VII)
黑色沉淀	Black precipitate
紫色，苯酚与铁离子的特征产物	Purple; characteristic product of phenol with iron(III) ions
弱酸性，能使石蕊不变色	Weakly acidic; does not change the colour of litmus
灰白色沉淀	Greyish-white precipitate
淡黄色沉淀	Pale yellow precipitate
白色沉淀，易被氧化变褐	White precipitate; readily oxidised and turns brown
灰绿色胶状沉淀，两性	Grey-green gelatinous amphoteric precipitate
不存在于溶液中，立即水解	Does not exist in solution; hydrolyses immediately
肉色沉淀	Flesh-coloured precipitate
黄色沉淀	Yellow precipitate
砖红色沉淀	Brick-red precipitate
弱酸，不稳定，用于重氮化	Weak, unstable acid; used in diazotisation
与酸生成亚硝酸	Forms nitrous acid with acid
碘量法滴定剂，遇酸析硫	Titrant in iodometric titrations; deposits sulfur with acid
强氧化剂，碘钟反应用	Strong oxidising agent; used in the iodine clock reaction
遇水剧烈水解	Hydrolyses vigorously in water
氯化剂，遇水剧烈水解	Chlorinating agent; hydrolyses vigorously in water
把羧酸变成酰氯，副产物全是气体	Converts carboxylic acids into acyl chlorides; the by-products are all gases
温和还原剂，还原羰基成醇	Mild reducing agent; reduces carbonyl compounds to alcohols
强还原剂，遇水剧烈反应	Strong reducing agent; reacts violently with water
剧毒，苦杏仁味	Highly toxic; smell of bitter almonds
剧毒，提供 CN-	Highly toxic; supplies CN- ions
遇水水解生成硅酸与氯化氢	Hydrolyses in water to give silicic acid and hydrogen chloride
受热分解	Decomposes on heating
强干燥剂	Strong drying agent
水解显碱性	Hydrolyses to give an alkaline solution
卤代烃，密度小于水	Halogenoalkane, less dense than water
卤代烃	Halogenoalkane
伯醇	Primary alcohol
仲醇	Secondary alcohol
醛	Aldehyde
酯，有果香	Ester with a fruity smell
伯胺，碱性	Primary amine; basic
腈	Nitrile
羧酸	Carboxylic acid
酰胺	Amide
烯烃，加成反应	Alkene; undergoes addition reactions
甲基苯，侧链可被氧化	Methylbenzene; the side chain can be oxidised
卤代芳烃	Halogenoarene
芳香羧酸	Aromatic carboxylic acid
酰氯，遇水剧烈水解	Acyl chloride; hydrolyses vigorously in water
酰氯	Acyl chloride
重氮盐，5℃以下稳定	Diazonium salt; stable below 5 °C
橙色偶氮染料	Orange azo dye
肽键 -CONH-	Peptide link, -CONH-
铝的两性产物	Amphoteric product of aluminium
浓盐酸中呈黄绿色	Yellow-green in concentrated hydrochloric acid
水合铜离子，蓝色	Blue aqua complex of copper(II)
蓝色，浓盐酸中生成	Blue; formed in concentrated hydrochloric acid
与铁离子显血红色	Gives a blood-red colour with iron(III) ions
ChemLab ▲ 虚拟化学实验室	ChemLab ▲ Virtual Chemistry Laboratory
虚拟化学实验室 · 离子级反应引擎	Virtual chemistry laboratory · ion-level reaction engine
容器 0	Vessels 0
物种 0	Species 0
反应 0	Reactions 0
容器	Vessels
物种	Species
反应	Reactions
清空全部	Clear all
器材架	Apparatus rack
未选择容器	No vessel selected
温度	Temperature
液面	Fill level
容器是空的	The vessel is empty
加热	Heat
搅拌	Stir
点燃	Ignite
倾倒 →	Pour →
倒空	Empty
移走	Remove
视图	View
网格吸附	Snap to grid
自动画质	Auto quality
渲染画质	Render quality
高	High
试剂架	Reagent shelf
适量	Moderate
少许	A little
过量	Excess
实验记录	Experiment log
选择左侧器材 → 点右侧试剂加入 → 观察现象	Choose apparatus on the left → click a reagent on the right to add it → observe what happens
清空记录	Clear log
正在装载试剂与器材…	Loading reagents and apparatus…
解析化学式 · 建立电离表 · 编译反应规则	Parsing formulae · building ionisation tables · compiling reaction rules
搜索名称 / 化学式…	Search name / formula…
烧杯	Beaker
锥形瓶	Conical flask
试管	Test tube
量筒	Measuring cylinder
圆底烧瓶	Round-bottom flask
蒸发皿	Evaporating dish
坩埚	Crucible
集气瓶	Gas jar
金属	Metal
非金属	Non-metal
氧化物	Oxide
酸	Acid
碱	Base
盐	Salt
有机物	Organic
指示剂	Indicator
其他	Other
其它	Other
没有匹配的试剂	No matching reagents
遇水反应	Reacts with water
难溶	Insoluble
气体	Gas
先在左边的器材架放一个容器	Put a vessel on the apparatus rack on the left first
先在左边器材架放一个容器	Put a vessel on the apparatus rack on the left first
加入约 ${amt.water} mL 水	Add about ${amt.water} mL of water
加入少量 ${name}	Add a little ${name}
溶液中	In solution
未溶固体	Undissolved solid
沉淀	Precipitate
水	Water
内容物	Contents
至少需要两个容器才能倾倒	You need at least two vessels before you can pour
已清空全部	Everything has been cleared
3D 渲染在这台机器上超时，已自动切到 2D 平面渲染	3D rendering timed out on this machine; switched to 2D rendering automatically
实验台已就绪。左侧选择器材，右侧点击试剂加入。	The bench is ready. Choose apparatus on the left and click reagents on the right to add them.
氢离子 H⁺	Hydrogen ion H⁺
氢氧根 OH⁻	Hydroxide ion OH⁻
钠离子 Na⁺	Sodium ion Na⁺
钾离子 K⁺	Potassium ion K⁺
铵根 NH₄⁺	Ammonium ion NH₄⁺
铜离子 Cu²⁺	Copper(II) ion Cu²⁺
亚铁离子 Fe²⁺	Iron(II) ion Fe²⁺
铁离子 Fe³⁺	Iron(III) ion Fe³⁺
银离子 Ag⁺	Silver ion Ag⁺
钡离子 Ba²⁺	Barium ion Ba²⁺
钙离子 Ca²⁺	Calcium ion Ca²⁺
镁离子 Mg²⁺	Magnesium ion Mg²⁺
铝离子 Al³⁺	Aluminium ion Al³⁺
锌离子 Zn²⁺	Zinc ion Zn²⁺
铅离子 Pb²⁺	Lead(II) ion Pb²⁺
锰离子 Mn²⁺	Manganese(II) ion Mn²⁺
铬离子 Cr³⁺	Chromium(III) ion Cr³⁺
氯离子 Cl⁻	Chloride ion Cl⁻
溴离子 Br⁻	Bromide ion Br⁻
碘离子 I⁻	Iodide ion I⁻
硝酸根 NO₃⁻	Nitrate ion NO₃⁻
硫酸根 SO₄²⁻	Sulfate ion SO₄²⁻
碳酸根 CO₃²⁻	Carbonate ion CO₃²⁻
碳酸氢根 HCO₃⁻	Hydrogencarbonate ion HCO₃⁻
磷酸根 PO₄³⁻	Phosphate ion PO₄³⁻
醋酸根 CH₃COO⁻	Ethanoate ion CH₃COO⁻
高锰酸根 MnO₄⁻	Manganate(VII) ion MnO₄⁻
重铬酸根 Cr₂O₇²⁻	Dichromate(VI) ion Cr₂O₇²⁻
铬酸根 CrO₄²⁻	Chromate(VI) ion CrO₄²⁻
硫离子 S²⁻	Sulfide ion S²⁻
硫氰酸根 SCN⁻	Thiocyanate ion SCN⁻
草酸根 C₂O₄²⁻	Ethanedioate ion C₂O₄²⁻
硅酸根 SiO₃²⁻	Silicate ion SiO₃²⁻
偏铝酸根 AlO₂⁻	Aluminate ion AlO₂⁻
次氯酸根 ClO⁻	Chlorate(I) ion ClO⁻
亚硫酸氢根 HSO₃⁻	Hydrogensulfite ion HSO₃⁻
亚硫酸根 SO₃²⁻	Sulfite ion SO₃²⁻
氟离子 F⁻	Fluoride ion F⁻
四氨合铜离子	Tetraamminecopper(II) ion
二氨合银离子	Diamminesilver(I) ion
左侧选器材 → 右侧点试剂加入 → 观察现象	Choose apparatus on the left → add reagents on the right → observe
左侧选器材	Choose apparatus on the left
选择左侧器材 → 右侧点试剂加入 → 观察现象	Choose apparatus on the left → add reagents on the right → observe
倾倒→	Pour →
加入约 {amt.water:F0} mL 水	Add about {amt.water:F0} mL of water
实验台已就绪。左侧选器材，右侧点试剂加入。	The bench is ready. Choose apparatus on the left and click reagents on the right to add them.
[ChemLab] 材质模板为空，无法创建视图	[ChemLab] material template is empty; the view cannot be created
[ChemLab] 场景初始化完成	[ChemLab] scene initialisation complete
[ChemLab] 找不到着色器 {name}，回退到 {fallback}	[ChemLab] shader {name} not found, falling back to {fallback}
[ChemLab] 连 {fallback} 都没有	[ChemLab] even {fallback} is missing
[ChemLab FX] 粒子顶点数={fxVerts}	[ChemLab FX] particle vertices = {fxVerts}
[ChemLab] UI 像素 {nz}	[ChemLab] UI pixels {nz}
[ChemLab] 已出图 {w}x{h} → {path}	[ChemLab] image written {w}x{h} → {path}
放置了一个${VESSEL_NAMES[type]}	Placed a ${VESSEL_NAMES[type]}
清空了所有容器	Emptied all vessels
加入 ${amountTxt} ${s.name}（${s.formula || ''}）	Added ${amountTxt} ${s.name} (${s.formula || ''})
加入 ${ml} mL 水	Added ${ml} mL of water
加入 ${ml} mL ${s.name}	Added ${ml} mL of ${s.name}
钠浮在水面上熔成小球快速游动，发出嘶嘶声，很快消失	The sodium floats on the water, melts into a ball that darts about with a hissing sound and soon disappears
钾遇水立即燃烧，发出紫色火焰并发生轻微爆炸	The potassium ignites at once with a lilac flame and a slight explosion
钙粒表面剧烈产生气泡，逐渐溶解	Vigorous effervescence on the calcium granules, which gradually dissolve
固体剧烈水解放出大量热	The solid hydrolyses vigorously, releasing much heat
与水反应放出大量热，水剧烈沸腾冒白雾	Reaction with water releases much heat; the water boils vigorously with white fumes
淡黄色固体溶解并放出大量热，产生气泡	The pale yellow solid dissolves releasing much heat and a gas is evolved
立即水解，产生有臭鸡蛋气味的气体并析出白色沉淀	Hydrolyses at once, giving a gas with the smell of rotten eggs and a white precipitate
固体溶解，溶液显强碱性	The solid dissolves and the solution is strongly alkaline
开始加热	Heating started
停止加热	Heating stopped
搅拌中	Stirring
${s.name}燃烧，产生明亮的火焰	${s.name} burns with a bright flame
火焰呈特征颜色（${flameIon} 的焰色反应）	The flame shows a characteristic colour (flame test for ${flameIon})
没有可燃烧的物质	There is nothing here that will burn
倒入约 ${w.toFixed(0)} mL 溶液	Poured about ${w.toFixed(0)} mL of solution
倒空并洗净容器	Vessel emptied and rinsed
产生${precip.desc}的${s.name}（${s.formula}）	A ${precip.desc} precipitate of ${s.name} (${s.formula}) forms
${s.name}沉淀逐渐溶解	The ${s.name} precipitate gradually dissolves
溶液颜色由${n1}变为${n2}	The solution turns from ${n1} to ${n2}
溶液温度升高到约 ${v.temp.toFixed(0)} ℃	The solution warms to about ${v.temp.toFixed(0)} °C
液体沸腾，大量水蒸气逸出	The liquid boils and much steam is given off
无色	colourless
灰色	grey
粉红色	pink
红色	red
橙红色	orange-red
浅棕黄色	pale brown-yellow
棕黄色	brown-yellow
橙黄色	orange-yellow
橙色	orange
淡蓝色	pale blue
蓝色	blue
浅绿色	pale green
黄绿色	yellow-green
淡色	pale
深色	dark
[ChemLab] 3D 渲染退出：	[ChemLab] 3D rendering exited:
，切换到 2D	, switching to 2D
脚本异常	Script error
着色器编译失败:	Shader compilation failed:
着色器链接失败	Shader linking failed
[ChemLab] 着色器编译耗时	[ChemLab] shader compilation took
(总加载	(total load
省电	Battery saver
中	Medium
极高	Ultra
图形上下文丢失，已暂停渲染。<br>刷新页面即可恢复。	Graphics context lost; rendering paused.<br>Refresh the page to recover.
首帧超时	First frame timed out
pH	pH
容器	Vessel
记录	Log
标准单元	Standard cell
工艺节点	Process node
实时	Real-time
未选择容器	No vessel selected
内容物	Contents
倾倒量	Pour amount
本生灯火力	Bunsen burner setting
容器标签	Vessel labels
方程式标签	Equation labels
时长	Duration
秒	seconds
焓变计算器	Enthalpy calculator
试剂架	Reagent shelf
拖入容器添加	Drag into a vessel to add
搜索名称 / 化学式…	Search name / formula…
左侧选器材 → 从右侧拖试剂到容器 → 输入数量	Select apparatus on the left → drag a reagent from the right into a vessel → enter an amount
暂无图片	No image available
上下拖动旋钮 / 滚轮调速	Drag or scroll to adjust flow
本生灯 · 调节火力	Bunsen burner · adjust flame
输入物质的量或质量（g / mol）	Enter amount of substance or mass (g / mol)
器材架 · 滚动查看操作	Apparatus · scroll to view controls
全场反应已暂停；预览后可撤销，完成才提交	All reactions are paused. Preview can be undone; changes are committed when you finish.
冷凝传热 · 模型说明	Condenser heat transfer · model
冷凝传热 · 实时账本	Condenser heat transfer · live ledger
加入 	Add 
超过目标容器安全容量，请减量	The target vessel would exceed its safe capacity; reduce the amount.
请选择两个不同且已打开的容器	Select two different, open vessels.
倾倒比例必须在0至100%之间	Pour fractions must be between 0 and 100%.
请等待内容物全部到达后再完成	Wait for all contents to arrive before finishing.
木条没有点燃，什么也检验不出来	The splint is not lit; no test can be performed.
木条被点燃	The splint is lit.
试纸已用蒸馏水湿润	The test paper has been moistened with distilled water.
未测	Not measured
无液相	No liquid phase
密闭	Sealed
具体物质	Molecular substances
离子状态	Ionic species
标称容量	Rated capacity
气	Gas
固	Solid
沉	Precipitate
无	None
";

/* ---------- 本地化：其它固定界面、菜单与教程 ---------- */
public const string TblI18nUiAdditional = @"
产生白色絮状的氯化银（AgCl）	A white, flocculent precipitate of silver chloride (AgCl) forms.
装置	assembly
教学参数	teaching parameter
参数	parameter
可撤销	can be undone
重新选量	choose the amounts again
完成才提交	committed only on completion
调高火力会增大火焰并加快加热。	Increasing the flame setting makes the flame larger and speeds up heating.
未点燃 · 拖动滑条立即生效	Unlit · slider changes apply immediately
水溶液按水的近室温热容近似，未计溶质热容	Aqueous solutions use water's near-room-temperature heat capacity; solute heat capacities are not included.
密闭模拟气密瓶	Seal as a simulated airtight vessel
请选择未密闭的加热烧瓶	Select an unsealed flask for heating.
该器皿已经连接另一套装置	This vessel is already connected to another assembly.
请先连接装置和接收容器	Connect the apparatus and receiver first.
蒸馏/回流装置必须保持通大气，不能密闭加热	Distillation/reflux equipment must remain open to the atmosphere; do not heat it while sealed.
请先打开冷凝水（下口进、上口出）	Turn on the coolant first (inlet at the bottom, outlet at the top).
冷凝水温度超出当前0–40℃教学模型	Coolant temperature is outside the current 0–40 °C teaching model.
当前只有常压蒸馏数据，未套用到减压/加压条件	Only atmospheric-pressure distillation data are available; vacuum or pressurised conditions are not modelled.
蒸馏演示流量必须为正数	The demonstration distillation flow rate must be positive.
传热参数或供热量无效	Invalid heat-transfer parameters or heat input.
当前混合物/有机物尚未接入能量冷凝模型	The energy-based condensation model is not yet connected for this mixture or organic substances.
部分物质缺少常压沸点；未凭空蒸馏该物质	Some substances have no known atmospheric boiling point; they have not been distilled without data.
当前冷凝水温度不能冷凝部分低沸点物质；未把它们伪装成馏出液	At this coolant temperature, some low-boiling substances cannot condense; they are not falsely shown as distillate.
接收容器已满，蒸馏已停止；未丢弃或覆盖物料	The receiver is full. Distillation stopped; no material was discarded or overwritten.
接收容器温度过高，请先冷却或更换空容器	The receiver is too hot. Cool it or replace it with an empty vessel.
该接收瓶已连接另一套蒸馏装置	This receiver is already connected to another distillation assembly.
接收容器不能同时作为另一个装置的加热瓶	A receiver cannot also serve as the heated flask of another assembly.
蒸馏需要另一个未密闭的接收容器	Distillation requires another unsealed receiver.
密闭	Sealed
开启	On
该容器正作为接收瓶，请先拆除原连接	This vessel is already serving as the receiver; detach the existing assembly first.
已安装竖直回流冷凝器；右键打开冷凝水后启动	Vertical reflux condenser installed. Right-click to turn on the coolant before starting.
已安装蒸馏头、温度计和冷凝器，并将接收瓶移到出口；右键打开冷凝水后启动	Still head, thermometer and condenser installed; the receiver has been moved to the outlet. Right-click to turn on the coolant before starting.
台面没有足够空位放置整套装置，请先腾出空间	There is not enough clear bench space for the full apparatus. Make room first.
冷凝水已打开：下进上出	Coolant is on: inlet at the bottom, outlet at the top.
冷凝水已关闭	Coolant is off.
查看装置状态	View apparatus status
安装回流冷凝器	Install reflux condenser
安装蒸馏装置／选接收瓶	Install distillation setup / choose receiver
传热设置（水／甲醇／乙醇）	Heat-transfer settings (water / methanol / ethanol)
查看热量账本	View heat ledger
传热模型说明	Heat-transfer model notes
查看装置全貌	Frame full apparatus
拆除冷凝装置	Remove condenser assembly
新建250 mL锥形接收瓶	New 250 mL conical receiver
传热教学参数：	Heat-transfer teaching parameter:
器皿蓄热：	Vessel heat storage:
按实际模型计算冷凝管、器皿玻璃及水套体积。	Condenser tube, vessel glass and water-jacket volumes are calculated from the model geometry.
玻璃采用 BOROFLOAT 平均热容代理，并作 0–100℃定热容近似。	Glass uses an average BOROFLOAT heat-capacity proxy, approximated as constant from 0–100 °C.
空气换热、液体接触热导和两侧 UA 分配为教学设定，不是实测仪器标定。	Air heat exchange, liquid-contact conductance and UA allocation are teaching assumptions, not instrument calibration data.
储热、冷却水外流和环境散热分别记账。	Stored heat, heat carried away by coolant and ambient losses are tracked separately.
关闭 / Enter / Esc	Close / Enter / Esc
全场反应已暂停；预览后可撤销，完成才提交	All reactions are paused. Preview can be undone; changes are committed only when you finish.
倾倒量	Pour amount
液体	Liquid
固体	Solid
预览完成；可撤销、重新选量或完成操作	Preview complete. Undo, adjust the amounts or finish.
已恢复倾倒前状态，请重新选量	The pre-pour state has been restored. Choose the amounts again.
请拖到目标器皿上方	Drag over a target vessel
调高火力会增大火焰并加快加热。	Increasing the flame setting makes the flame larger and speeds up heating.
非金属	Nonmetal
氨（气体）	Ammonia (gas)
氯化氢（气体）	Hydrogen chloride (gas)
溴化氢（气体）	Hydrogen bromide (gas)
碘化氢（气体）	Hydrogen iodide (gas)
硫化氢（气体）	Hydrogen sulfide (gas)
氟化氢（气体）	Hydrogen fluoride (gas)
硫酸（液体）	Sulfuric acid (liquid)
硝酸（液体）	Nitric acid (liquid)
乙醛（25℃气体）	Ethanal (gas at 25 °C)
乙胺（气体）	Ethylamine (gas)
未点燃 · 拖动滑条立即生效	Unlit · slider changes apply immediately
调高火力会增大火焰并加快加热。	Increasing the flame setting makes the flame larger and speeds up heating.
低于 12% 无法维持火焰；调高后需重新点燃。	Below 12%, the flame cannot stay lit; increase the setting and ignite it again.
Esc 关闭，保留当前设置。	Esc closes the panel and keeps the current setting.
请输入大于 0 的有限数值	Enter a finite value greater than 0.
放回台面	Put back on bench
密闭模拟气密瓶	Seal as a simulated airtight vessel
用蒸馏水湿润	Moisten with distilled water
确定 / Enter	Confirm / Enter
下一步 ▶	Next ▶
跳过教程 ⏭	Skip tutorial ⏭
焓变计算器 · Hess's Law	Enthalpy · Hess's Law
未点燃 · 拖动滑条立即生效\n调高火力会增大火焰并加快传热。\nEsc 关闭，保留当前设置。	Unlit · slider changes apply immediately\nHigher heat increases the flame and speeds up heating.\nEsc closes while keeping the current setting.
KCl 的 Born-Haber 循环已搭好：晶格能是未知量。点「求解未知焓变」。	The KCl Born–Haber cycle is ready, with lattice enthalpy as the unknown. Select “Solve for unknown enthalpy change”.
回路上有不止一个未知量，条件不足	The cycle contains more than one unknown; there is insufficient information.
水溶液按水的近室温热容近似，未计溶质热容	Aqueous solutions use water's near-room-temperature heat capacity; solute heat capacities are not included.
放回台面	Put back on bench
器材	Apparatus
试剂	Reagents
未选择容器	No vessel selected
显示 开	Show: On
显示 关	Show: Off
解除吸附	Detach
倒空并洗净	Empty and rinse
标称容量	Nominal capacity
时长	Dur.
秒	s
方程式标签	Eq.
乙胺（气体）	Ethylamine (gas)
丁-1-烯	But-1-ene
丁-1-醇	Butan-1-ol
丁酸	Butanoic acid
偏硼酸钠	Sodium metaborate
实验台已就绪。左侧选器材；从右侧拖试剂到器皿，松开后输入数量并确认加入。	The lab is ready. Select apparatus on the left, then drag a reagent from the right into a vessel. Release it, enter an amount, and confirm to add it.
加热	Heat
搅拌	Stir
点燃	Ignite
倾倒→	Pour →
倒空	Empty
移走	Remove
检验器材	Test tool
湿润	Moisten
伸入	Insert
标签	Tags
具体/离子	Molecular / ionic
详情	Details
全部详情	All details
选中详情	Selected details
物质	Species
浓度	Concentration
显示 开	Show: On
显示 关	Show: Off
离子式 开	Ionic equation: On
离子式 关	Ionic equation: Off
标签 开	Tags: On
标签 关	Tags: Off
物质 开	Species: On
物质 关	Species: Off
浓度 开	Concentration: On
浓度 关	Concentration: Off
mol 开	Amount: On
mol 关	Amount: Off
抽取	Aspirate
释放	Dispense
停止	Stop
查看全貌	Frame apparatus
返回视角	Return to previous view
百分比 / g	Percent / g
预览 / Enter	Preview / Enter
撤销	Undo
完成操作	Finish
取消	Cancel
本生灯 · 调节火力	Bunsen burner · adjust flame
点燃	Ignite
完成 / Enter	Done / Enter
返回实验台	Return to lab
求解未知焓变	Solve ΔH
清空	Clear all
空白处右键 = 新建节点 · 节点左键拖出箭头连线 · 连线/节点右键可编辑或删除	Right-click blank space to add a node. Drag from a node to draw an arrow. Right-click an edge or node to edit or delete it.
示例：C → CO2、C → CO 已知，CO → CO2 未知。点「求解未知焓变」。	Example: C → CO2 and C → CO are known; CO → CO2 is unknown. Select “Solve for unknown enthalpy change”.
化学式	Formula
在此新建节点	Add node here
编辑化学式	Edit formula
编辑系数	Edit coefficient
复制节点	Duplicate node
删除节点	Delete node
编辑焓变值	Edit enthalpy change
删除连线	Delete edge
手动输入…	Enter manually…
设为未知量（待求）	Set as unknown
取消（撤销这条线）	Cancel (remove this edge)
输入这条连线的焓变 ΔH（kJ/mol）	Enter the enthalpy change ΔH for this edge (kJ/mol)
确定	OK
还没有连线。	There are no edges yet.
没有找到闭合回路：至少要三个节点、三条首尾相接的连线。	No closed cycle found. At least three nodes and three connected edges are required.
已清空。	Cleared.
调节火力…	Adjust flame…
点燃木条	Light splint
熄灭木条（保留余烬20秒）	Extinguish splint (embers last 20 seconds)
回流套件	Reflux kit
蒸馏套件	Distillation kit
新建250 mL锥形接收瓶	New 250 mL conical receiver
安装回流冷凝器	Install reflux condenser
安装蒸馏装置／选接收瓶	Install distillation apparatus / choose receiver
打开冷凝水	Turn on coolant
关闭冷凝水	Turn off coolant
启动蒸馏／回流	Start distillation / reflux
停止蒸馏／回流	Stop distillation / reflux
查看装置状态	View apparatus status
查看装置全貌	Frame full apparatus
拆除冷凝装置	Remove condenser
传热设置（水／甲醇／乙醇）	Heat-transfer settings (water / methanol / ethanol)
基准功率（灯50%） 	Reference power (burner at 50%) 
冷却水流量 	Coolant flow rate 
冷却水入口 	Coolant inlet temperature 
总热导 UA 	Overall thermal conductance UA 
查看热量账本	View heat ledger
传热模型说明	Heat-transfer model explanation
无可选目标	No available target
倾倒操作已结束	The pour operation has ended.
飞行中物质量无效	Invalid amount of material in transit.
预览完成；可撤销、重新选量或完成操作	Preview complete. Undo, adjust the amounts or finish.
已恢复倾倒前状态	The pre-pour state has been restored.
方程式标签验证场景	Equation-label validation scene
新建250 mL锥形接收瓶	New 250 mL conical receiver
非金属	Nonmetal
热量必须为有限数值且需要容器	Heat must be finite and a vessel is required.
纯液体热量模型超出温度或近常压范围，保留原温度	The pure-liquid heat model is outside its temperature or near-atmospheric-pressure range; the original temperature is retained.
热量超出纯液体焓模型范围，保留原温度；此反应热入口尚未处理相变潜热	Heat is outside the pure-liquid enthalpy model range; the original temperature is retained. This reaction-heat input does not yet model latent heat.
计算后温度超出所用液相热容近似范围，未应用该反应热；相变潜热未建模	The resulting temperature is outside the liquid heat-capacity approximation range. The reaction heat was not applied; latent heat is not modelled.
温差必须为有限数值且需要容器	Temperature difference must be finite and a vessel is required.
量热区间超出纯液体温度或近常压范围，未外推焓	The calorimetry interval is outside the pure-liquid temperature or near-atmospheric-pressure range; enthalpy was not extrapolated.
量热终温超出所用液相热容近似范围，未外推能量	The final calorimetry temperature is outside the liquid heat-capacity approximation range; energy was not extrapolated.
缺少容器，未计算定量热效应	No vessel is available; quantitative thermal effects were not calculated.
当前纯液体超出温度或近常压范围，未计算热容	The pure liquid is outside its temperature or near-atmospheric-pressure range; heat capacity was not calculated.
混合液缺少专属热容/混合焓数据，未计算定量热效应；保留原温度	The mixture has no dedicated heat-capacity or enthalpy-of-mixing data. Quantitative thermal effects were not calculated; the original temperature is retained.
当前温度超出所用液相热容近似范围，未计算定量热效应；保留原温度	The current temperature is outside the liquid heat-capacity approximation range. Quantitative thermal effects were not calculated; the original temperature is retained.
缺少当前液体的热容数据，未计算定量热效应；保留原温度	Heat-capacity data for the current liquid are unavailable. Quantitative thermal effects were not calculated; the original temperature is retained.
水/乙醇使用近室温定热容近似，未计混合焓与热容温变	Water/ethanol uses a near-room-temperature constant-heat-capacity approximation; enthalpy of mixing and temperature-dependent heat capacity are omitted.
固体热容未计入	Solid heat capacity is not included.
纯液体焓曲线只用于近常压，当前压力缺少修正数据；保留接收器皿温度	The pure-liquid enthalpy curve is only for near-atmospheric pressure; no correction data are available at this pressure. The receiver temperature is retained.
超出纯液体液相焓模型温区，未外推混温；保留接收器皿温度	Outside the pure-liquid enthalpy model range; the mixture temperature was not extrapolated and the receiver temperature is retained.
同种纯液体按等比热容近似混温；缺少该物质随温度变化的热容数据	Mixing the same pure liquid uses an equal-specific-heat approximation; temperature-dependent heat-capacity data are unavailable.
混合液缺少专属热容/混合焓数据，未计算混温；保留接收器皿温度	The mixture has no dedicated heat-capacity or enthalpy-of-mixing data. Its mixed temperature was not calculated; the receiver temperature is retained.
当前温度超出所用液相热容近似范围，未计算混温；保留接收器皿温度	The current temperature is outside the liquid heat-capacity approximation range. The mixed temperature was not calculated; the receiver temperature is retained.
缺少当前液体的热容数据，未计算混温；保留接收器皿温度	Heat-capacity data for the current liquid are unavailable. Its mixed temperature was not calculated; the receiver temperature is retained.
纯水冰水转移按显热与熔化潜热守恒；冰水共存保持0℃，未计容器热容、过冷成核及混合物凝固点	Pure-water/ice transfer conserves sensible and latent heat of fusion. Coexisting ice and water remain at 0 °C; vessel heat capacity, supercooling nucleation and mixture freezing points are omitted.
本步热量超出冰水相变近似范围，保留原状态；未跳过潜热直接升温	This step's heat exceeds the ice/water phase-change approximation range. The original state is retained; latent heat is not bypassed to raise the temperature directly.
当前温压或混合物缺少适用的水/冰相平衡模型；保留原库存，未套用纯水0℃冻结结论	No applicable water/ice phase-equilibrium model exists for the current temperature, pressure or mixture. Inventory is retained; the pure-water 0 °C freezing result is not applied.
纯水常压冰水共存：保持0℃及当前相比例；加热按熔化潜热处理，未计容器热容和过冷成核	Pure water at atmospheric pressure with coexisting ice and water: 0 °C and the current phase ratio are preserved. Heating uses latent heat of fusion; vessel heat capacity and supercooling nucleation are omitted.
";

/* ---------- 本地化：类别 / 容器 ---------- */
public const string TblI18nCls = @"
metal	Metal
nonmetal	Non-metal
oxide	Oxide
acid	Acid
base	Base
salt	Salt
organic	Organic
indicator	Indicator
other	Other
ion	Ion
";
public const string TblI18nVessel = @"
beaker	Beaker
flask	Conical flask
tube	Test tube
cylinder	Measuring cylinder
roundbottom	Round-bottom flask
crucible	Crucible
dish	Evaporating dish
bottle	Gas jar
";

/* ---------- 特殊规则（颜色指示剂类）：id 现象 tint clearTint ---------- */
public const string TblSpecials = @"
starch-iodine	淀粉遇碘立即变深蓝色，加热后蓝色褪去，冷却后又复现	#2a1a6a	0
phenolphthalein	无色酚酞溶液变红，说明溶液显碱性	#e83a7a	0
phenolphthalein-acid	酚酞在酸性或中性溶液中不变色		1
litmus-acid	紫色石蕊溶液变红，说明溶液显酸性	#d83a3a	0
litmus-base	紫色石蕊溶液变蓝，说明溶液显碱性	#3a5ad8	0
methylorange-acid	甲基橙溶液变红	#e03a2a	0
methylorange-base	甲基橙溶液变黄	#e8b83a	0
phenol-fecl3	溶液立即显紫色，这是苯酚的特征反应	#7a3ac8	0
feoh2-oxidize	白色沉淀迅速变为灰绿色，最终变成红褐色（被空气氧化）		0
fe-scn	溶液立即变成血红色	#c01a1a	0
";

}
}
