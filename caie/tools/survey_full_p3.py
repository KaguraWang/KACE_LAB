# 完整盘点六份 P3 卷的题目结构（题干 + CI + MS）
import os, re

EXT = r'D:\ChemLabUnity\caie\extracted'
OUT = r'D:\ChemLabUnity\caie\p3_full_survey.txt'

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
        ln = re.sub(r'©.*$', '', ln)
        if ln.strip(): out.append(ln.strip())
    return out

out = []
for y in ['s24', 's25', 's26']:
    for comp in ['33', '34']:
        qp = clean(load(f'9701_{y}_qp_{comp}'))
        ms = clean(load(f'9701_{y}_ms_{comp}'))
        ci = clean(load(f'9701_{y}_ci_{comp}'))
        out.append('#' * 100)
        out.append(f'## 9701/{comp}/M/J/{y[1:]}')
        out.append('#' * 100)

        # 题目分节
        heads = []
        for i, ln in enumerate(qp):
            if re.match(r'^Qualitative analysis$', ln): heads.append((i, 'QUAL'))
            if re.match(r'^Quantitative analysis$', ln): heads.append((i, 'QUANT'))
            m = re.match(r'^([123])\s{1,4}([A-Z].{10,})$', ln)
            if m: heads.append((i, 'Q' + m.group(1) + ' | ' + m.group(2)[:110]))

        out.append('\n=== 结构 ===')
        for i, (idx, h) in enumerate(heads):
            out.append(f'  [{idx}] {h}')

        # 每个大题前 60 行的正文
        qstarts = [x for x in heads if x[1].startswith('Q')]
        for k, (idx, h) in enumerate(qstarts):
            end = qstarts[k+1][0] if k+1 < len(qstarts) else len(qp)
            out.append(f'\n──────── {h}')
            for ln in qp[idx+1:min(end, idx+70)]:
                out.append('  ' + ln[:150])

        out.append('\n=== CI 材料 ===')
        for ln in ci:
            if re.match(r'^(FA|FB)\s*\d', ln) or re.match(r'^(label|identity)', ln):
                out.append('  ' + ln[:180])

        out.append('\n=== MS 要点 ===')
        started = False
        for ln in ms:
            if re.match(r'^[123]\(', ln) or re.match(r'^Question\b', ln):
                started = True
            if started and (re.match(r'^[123]\(', ln) or re.match(r'^[123]\s', ln)):
                out.append('  ' + ln[:170])
        out.append('')

open(OUT, 'w', encoding='utf-8').write('\n'.join(out))
print('已写出', OUT, len(out), '行')
