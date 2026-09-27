# 逐份提取 P3 的 Q2 / Q3 全文（更宽松的匹配）
import os, re

EXT = r'D:\ChemLabUnity\caie\extracted'
OUT = r'D:\ChemLabUnity\caie\p3_q23_full.txt'

def load(n):
    p = os.path.join(EXT, n + '.txt')
    return open(p, encoding='utf-8').read() if os.path.exists(p) else ''

def clean(s):
    out = []
    for ln in s.split('\n'):
        bad = sum(1 for ch in ln if ord(ch) > 0x2fff)
        if len(ln) > 4 and bad > len(ln) * 0.4: continue
        ln = re.sub(r'\* \d+ \*', '', ln)
        ln = re.sub(r'DO NOT WRITE IN THIS MARGIN', '', ln)
        ln = re.sub(r'\[Turn over\]', '', ln)
        if ln.strip(): out.append(ln.strip())
    return out

out = []
for y in ['s24', 's25', 's26']:
    for comp in ['33', '34']:
        qp = clean(load(f'9701_{y}_qp_{comp}'))
        ci = clean(load(f'9701_{y}_ci_{comp}'))
        out.append('=' * 90)
        out.append(f'9701/{comp}/M/J/{y[1:]}')
        out.append('=' * 90)

        # 找大题起点
        starts = []
        for i, ln in enumerate(qp):
            m = re.match(r'^([23])\s{2,}([A-Z].{15,})$', ln)
            if m: starts.append((i, m.group(1), m.group(2)))
        if not starts:
            for i, ln in enumerate(qp):
                m = re.match(r'^([23])\s+([A-Z].{15,})$', ln)
                if m: starts.append((i, m.group(1), m.group(2)))
        starts = starts[:4]
        for k, (idx, qn, title) in enumerate(starts):
            end = starts[k+1][0] if k + 1 < len(starts) else min(idx + 90, len(qp))
            out.append(f'\n──────── Q{qn} ────────')
            out.append(f'开头：{title[:200]}')
            for ln in qp[idx+1:end]:
                out.append('  ' + ln[:160])
        out.append('\n  ── CI 材料表（前 40 行）──')
        for ln in ci:
            if re.match(r'^(FA|FB)\s*\d', ln) or re.match(r'^\d+(\.\d+)?\s*(cm3|g)', ln):
                out.append('  ' + ln[:170])

open(OUT, 'w', encoding='utf-8').write('\n'.join(out))
print('已写出', OUT)
print('行数', len(out))
