# -*- coding: utf-8 -*-
"""从 CIE 数据手册抽取电离能 / 键能 / 电极电势 / 常数"""
import re, json

SRC = r'D:\ChemLabUnity\caie\extracted\cie_specimen_data_booklet.txt'
txt = open(SRC, encoding='utf-8').read()
pages = {}
for m in re.finditer(r'===== PAGE (\d+) =====\n(.*?)(?=\n===== PAGE |\Z)', txt, re.S):
    pages[int(m.group(1))] = m.group(2)

out = {'ionisation': [], 'bondDiatomic': [], 'bondPoly': [], 'electrode': [], 'constants': {}}

# ---------- 1) 电离能 ----------
for ln in pages.get(4, '').split('\n'):
    m = re.match(r'^\s*([A-Z][a-z]?)\s+(\d{1,2})\s+(\d+)\s+(\d+|\u2013)\s+(\d+|\u2013)\s+(\d+|\u2013)\s*$', ln)
    if not m:
        continue
    num = lambda x: None if x == '\u2013' else int(x)
    out['ionisation'].append({
        'el': m.group(1), 'z': int(m.group(2)),
        'ie': [num(m.group(3)), num(m.group(4)), num(m.group(5)), num(m.group(6))],
    })

# ---------- 2) 键能 ----------
# PDF 里单键是私用区字符 U+F8E7；= 双键，≡ 三键，…. 苯环离域键
SB = r'[\uF8E7\u2013\u2014\-]'
BOND = re.compile(r'([A-Z][a-z]?)\s*(' + SB + r'|=|\u2261|\u2026\.{0,4})\s*([A-Z][a-z]?)(?:\s*\(([^)]*)\))?\s+(\d{3,4})')

def order_of(sym):
    if sym == '=': return 2
    if sym == '\u2261': return 3
    if '\u2026' in sym or '.' in sym: return 1.5   # 苯环离域键
    return 1

for pg, exact in ((5, True), (6, False)):
    for ln in pages.get(pg, '').split('\n'):
        for m in BOND.finditer(ln):
            a, sym, b, note, kj = m.group(1), m.group(2), m.group(3), m.group(4) or '', int(m.group(5))
            if a in ('Bond',) or b in ('Energy',):
                continue
            rec = {'a': a, 'b': b, 'order': order_of(sym), 'note': note, 'kJ': kj, 'exact': exact}
            (out['bondDiatomic'] if exact else out['bondPoly']).append(rec)

# ---------- 3) 电极电势 ----------
def clean(s):
    s = re.sub(r'(\d)\s*\.\s*(\d)', r'\1.\2', s)   # "+ 1 . 0 7" -> "+1.07"
    s = s.replace('\u2013', '-').replace('\u2212', '-')
    return re.sub(r'\s+', '', s)

for pg in (7, 8):
    for ln in pages.get(pg, '').split('\n'):
        m = re.match(r'^(.*?)\s*([+\-\u2212]\s*[\d.\s]+)\s*$', ln)
        if not m:
            continue
        eq = m.group(1).strip()
        if not re.search(r'e[\-\u2212\u2013]', eq):
            continue
        v = clean(m.group(2))
        try:
            val = float(v)
        except ValueError:
            continue
        out['electrode'].append({'eq': re.sub(r'\s+', ' ', eq), 'E': val})

out['constants'] = {'R': 8.31, 'F': 9.65e4, 'L': 6.02e23, 'h': 6.63e-34, 'c': 3.00e8,
                    'e': -1.60e-19, 'Vm_stp': 22.4, 'Vm_rtp': 24.0, 'Kw': 1.00e-14,
                    'c_water': 4.18}

open(r'D:\ChemLabUnity\caie\specimen_tables.json', 'w', encoding='utf-8').write(
    json.dumps(out, ensure_ascii=False, indent=1))

print('电离能: %d 种元素' % len(out['ionisation']))
print('  样例:', out['ionisation'][:3])
print('双原子键能: %d 条' % len(out['bondDiatomic']))
for r in out['bondDiatomic'][:6]: print('   ', r)
print('多原子键能: %d 条' % len(out['bondPoly']))
for r in out['bondPoly'][:6]: print('   ', r)
print('电极电势: %d 条' % len(out['electrode']))
for r in out['electrode'][:3]: print('   ', r)
