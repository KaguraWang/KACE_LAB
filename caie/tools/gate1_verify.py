# =====================================================================
# Gate 0/1：核验 30 份 PDF 并抽取全文
#  - 页码、SHA-256、能否完整解析（防截断）
#  - 首页身份匹配：syllabus 9701 / component / 考季
#  - QP 与 MS 组件号必须一致
#  - 全文导出到 extracted/
# =====================================================================
import json, os, re, hashlib
import pypdf
import warnings
warnings.filterwarnings('ignore')

SRC = r'D:\ChemLabUnity\caie\sources'
EXT = r'D:\ChemLabUnity\caie\extracted'
os.makedirs(EXT, exist_ok=True)
MONTHS = {'s': 'May/June', 'w': 'October/November'}

def parse_name(fn):
    m = re.match(r'9701_([sw])(\d{2})_([a-z]+)_(\d{2})\.pdf$', fn)
    return None if not m else dict(season=m.group(1), year=2000 + int(m.group(2)),
                                   kind=m.group(3), component=m.group(4))

def read_all(path):
    """返回 (页数, 前3页文字, 全文, 错误)"""
    try:
        r = pypdf.PdfReader(path)
        n = len(r.pages)
        pages = []
        for pg in r.pages:
            pages.append(pg.extract_text() or '')
        return n, '\n'.join(pages[:3]), pages, None
    except Exception as e:
        return -1, '', [], str(e)

def detect_identity(txt):
    """身份识别。不同年份排版不同：有的写 CHEMISTRY 9701/33，
       有的把 9701/33 单独一行、CHEMISTRY 在后面。"""
    out = {}
    m = re.search(r'\b(9701)\s*/\s*(\d{2})\b', txt)          # 宽松：任意位置的 9701/NN
    if m:
        out['syllabus'] = m.group(1)
        out['component'] = m.group(2)
    m2 = re.search(r'(May/June|October/November|February/March)\s+(\d{4})', txt)
    if m2:
        out['month'] = m2.group(1)
        out['year'] = int(m2.group(2))
    if re.search(r'Paper\s+1\b', txt):   out['paper'] = 1
    elif re.search(r'Paper\s+2\b', txt): out['paper'] = 2
    elif re.search(r'Paper\s+3\b', txt): out['paper'] = 3
    up = txt.upper()
    if 'MARK SCHEME' in up: out['is_ms'] = True
    if 'CONFIDENTIAL INSTRUCTIONS' in up: out['is_ci'] = True
    return out

EXPECT_IS_MS = {'ms': True, 'qp': False, 'ci': False}

rows = []
for fn in sorted(f for f in os.listdir(SRC) if f.endswith('.pdf')):
    path = os.path.join(SRC, fn)
    meta = parse_name(fn)
    pages, head, allpages, err = read_all(path)
    ident = detect_identity(head if head else '')
    sha = hashlib.sha256(open(path, 'rb').read()).hexdigest()

    body = []
    for i, t in enumerate(allpages):
        body.append(f'\n===== PAGE {i+1} =====\n' + t)
    open(os.path.join(EXT, fn.replace('.pdf', '.txt')), 'w', encoding='utf-8').write(''.join(body))

    chk = dict(
        name=meta is not None,
        parseable=(err is None),
        pagecount=pages > 0,
        syllabus=ident.get('syllabus') == '9701',
        component=(meta is not None and ident.get('component') == meta['component']),
        month=(meta is not None and ident.get('month') == MONTHS.get(meta['season'])),
        year=(meta is not None and ident.get('year') == meta['year']),
        kind_matches=(meta is not None and ident.get('is_ms', False) == EXPECT_IS_MS.get(meta['kind'], False)),
    )
    rows.append(dict(
        file=fn,
        season=meta['season'] if meta else '?', year=meta['year'] if meta else 0,
        kind=meta['kind'] if meta else '?', component=meta['component'] if meta else '?',
        pages=pages, sha256=sha, bytes=os.path.getsize(path),
        identity=ident, checks=chk, parse_error=err,
        text_chars=sum(len(p) for p in allpages),
        status='OK' if all(chk.values()) else 'REVIEW',
    ))

report = dict(source_dir=SRC, file_count=len(rows),
              all_ok=all(r['status'] == 'OK' for r in rows), files=rows)
open(r'D:\ChemLabUnity\caie\gate1_report.json', 'w', encoding='utf-8').write(
    json.dumps(report, ensure_ascii=False, indent=2))

print(f'{"file":<22}{"pages":>6}{"chars":>9}  {"kind":<4}{"comp":>5}  status  failed-checks')
for r in rows:
    bad = [k for k, v in r['checks'].items() if not v]
    print(f'{r["file"]:<22}{r["pages"]:>6}{r["text_chars"]:>9}  {r["kind"]:<4}{r["component"]:>5}  '
          f'{r["status"]:<7} {",".join(bad) if bad else "-"}')
print()
print('ALL OK' if report['all_ok'] else 'SOME NEED REVIEW')
