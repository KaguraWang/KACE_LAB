# 盘点所有 P3 卷（33/34 x 3 年）：FA 定义、反应方程式、CI 真实配制
import os, re, json

EXT = r'D:\ChemLabUnity\caie\extracted'

def load(n):
    p = os.path.join(EXT, n + '.txt')
    return open(p, encoding='utf-8').read() if os.path.exists(p) else ''

def clean(s):
    """去掉边缘乱码与页眉页脚"""
    s = re.sub(r'\* \d+ \*', ' ', s)
    s = re.sub(r'DO NOT WRITE IN THIS MARGIN', ' ', s)
    s = re.sub(r'\[Turn over\]', ' ', s)
    # 去掉连续的非 ASCII 乱码行
    out = []
    for ln in s.split('\n'):
        bad = sum(1 for ch in ln if ord(ch) > 0x2fff)
        if len(ln) > 4 and bad > len(ln) * 0.4: continue
        out.append(ln.strip())
    s = '\n'.join(l for l in out if l)
    return re.sub(r'\n{2,}', '\n', s)

surveys = []
for y in ['s24', 's25', 's26']:
    for comp in ['33', '34']:
        qp = clean(load(f'9701_{y}_qp_{comp}'))
        ci = clean(load(f'9701_{y}_ci_{comp}'))
        ms = clean(load(f'9701_{y}_ms_{comp}'))

        # QP 里的 FA 定义
        fa_qp = re.findall(r'(FA\s*\d\s*is[^\n]{0,160})', qp)
        # QP 里的化学方程式（含 -> 或 箭头 或 =）
        eqs = re.findall(r'([A-Za-z0-9\(\)\[\]\+\-\u2192\s\.]{8,90}(?:->|\u2192|⟶)\s*[^\n]{2,80})', qp)
        # CI 里的真实浓度
        conc = re.findall(r'(FA\s*\d)\s[^\n]{0,20}?(\d+)\s*cm3\s+([^\n]{0,70}?)(?:\s{2,}|$)', ci)
        mols = re.findall(r'(\d+\.?\d*)\s*mol\s*dm[–\-]3', ci)
        # 题目数
        qnums = sorted(set(re.findall(r'\n\s*(\d)\s{2,}[A-Z]', qp)))

        surveys.append(dict(
            paper=f'9701/{comp}/M/J/{y[1:]}', year=y, comp=comp,
            qp_pages=qp.count('===== PAGE'), chars=len(qp),
            fa_in_qp=fa_qp[:8],
            equations=list(dict.fromkeys(eqs))[:6],
            ci_concentrations=[f'{a} {b} cm3 {c.strip()}' for a, b, c in conc][:10],
            ci_molarities=sorted(set(mols))[:12],
            questions=qnums,
        ))

for s in surveys:
    print('=' * 78)
    print(s['paper'])
    print('  FA 定义：')
    for f in s['fa_in_qp']: print('    -', f[:150])
    print('  方程式：')
    for e in s['equations']: print('    -', e[:120])
    print('  CI 浓度：', ', '.join(s['ci_molarities'][:12]))

json.dump(surveys, open(r'D:\ChemLabUnity\caie\p3_survey.json', 'w', encoding='utf-8'),
          ensure_ascii=False, indent=2)
print()
print('已写出 p3_survey.json')
