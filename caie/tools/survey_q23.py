# 盘点所有 P3 卷的 Q2 / Q3：题目类型、涉及反应、可自动化程度
import os, re, json

EXT = r'D:\ChemLabUnity\caie\extracted'
OUT = r'D:\ChemLabUnity\caie\p3_q23_survey.txt'

def load(n):
    p = os.path.join(EXT, n + '.txt')
    return open(p, encoding='utf-8').read() if os.path.exists(p) else ''

def clean(s):
    out = []
    for ln in s.split('\n'):
        bad = sum(1 for ch in ln if ord(ch) > 0x2fff)
        if len(ln) > 4 and bad > len(ln) * 0.4:
            continue
        out.append(ln.rstrip())
    return '\n'.join(l for l in out if l.strip())

lines = []
for y in ['s24', 's25', 's26']:
    for comp in ['33', '34']:
        qp = clean(load(f'9701_{y}_qp_{comp}'))
        lines.append('=' * 80)
        lines.append(f'9701/{comp}/M/J/{y[1:]}')
        lines.append('=' * 80)
        # 找每个大题开头："2  <描述>" 或 "3  <描述>"
        for qn in ['2', '3']:
            pat = re.compile(r'\n\s*' + qn + r'\s{2,}([A-Z][^\n]{10,200})')
            m = pat.search(qp)
            if m:
                lines.append(f'\n── Q{qn} 开头：{m.group(1).strip()[:180]}')
                # 该题后面 1500 字
                seg = qp[m.start():m.start() + 2600]
                for ln in seg.split('\n')[1:60]:
                    if ln.strip():
                        lines.append('   ' + ln.strip()[:150])
            else:
                lines.append(f'\n── Q{qn}：未匹配到开头')
        lines.append('')

open(OUT, 'w', encoding='utf-8').write('\n'.join(lines))
print('已写出', OUT, len(lines), '行')
