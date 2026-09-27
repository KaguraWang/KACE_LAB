const fs = require('fs');
const B = 'D:/ChemLabUnity/Assets/Scripts/Game/LabBootstrap.cs';
let c = fs.readFileSync(B, 'utf8');

if (c.includes('PetAudit')) { console.log('- 已存在'); process.exit(0); }

/* 审计方法 */
const anchor = '    void DumpTagAudit() {';
if (!c.includes(anchor)) { console.log('✗ 找不到 DumpTagAudit'); process.exit(1); }

const add = `    /// <summary>阿侬与教程的状态，给窗口级测试当客观证据。格式：PET|键|值</summary>
    public string PetAudit() {
        var sb = new System.Text.StringBuilder();
        var pv = PetView.Instance;
        sb.AppendLine("PET|exists|" + (pv != null ? "yes" : "no"));
        if (pv != null) {
            sb.AppendLine("PET|anim|" + pv.CurrentAnim);
            sb.AppendLine("PET|home|" + Mathf.RoundToInt(pv.Home.x) + "," + Mathf.RoundToInt(pv.Home.y));
            sb.AppendLine("PET|scale|" + pv.Scale.ToString("F2"));
            var rt = pv.GetComponent<RectTransform>();
            if (rt != null) {
                var sc = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
                sb.AppendLine("PET|screen|" + Mathf.RoundToInt(sc.x) + "," + Mathf.RoundToInt(sc.y));
                sb.AppendLine("PET|size|" + Mathf.RoundToInt(rt.rect.width) + "," + Mathf.RoundToInt(rt.rect.height));
            }
        }
        sb.AppendLine("PET|tutRunning|" + (PetTutorial.Running ? "yes" : "no"));
        sb.AppendLine("PET|tutStep|" + PetTutorial.StepIndex + "/" + PetTutorial.StepCount);
        sb.AppendLine("PET|tutTitle|" + PetTutorial.StepTitle);
        sb.AppendLine("PET|tutWaiting|" + (PetTutorial.WaitingOnUser ? "yes" : "no"));
        sb.AppendLine("PET|labDone|" + (PetProgress.LabDone ? "yes" : "no"));
        sb.AppendLine("PET|labSkipped|" + (PetProgress.LabSkipped ? "yes" : "no"));
        sb.AppendLine("PET|calcDone|" + (PetProgress.CalcDone ? "yes" : "no"));
        sb.AppendLine("PET|aiUnlocked|" + (PetProgress.AiUnlocked ? "yes" : "no"));
        return sb.ToString();
    }

    void DumpTagAudit() {`;

c = c.replace(anchor, add);
fs.writeFileSync(B, c, 'utf8');
console.log('✓ PetAudit 已加');

/* 接进 StateTick：没有计算器时也要打 */
let b = fs.readFileSync(B, 'utf8');
const old = `            if (cs != null) Debug.Log(cs.AuditLayout());`;
const nw = `            if (cs != null) Debug.Log(cs.AuditLayout());
            Debug.Log("[PetState]\\n" + PetAudit());`;
if (b.includes(old)) { b = b.replace(old, nw); fs.writeFileSync(B, b, 'utf8'); console.log('  ✓ 已接进 StateTick'); }
else console.log('  ✗ StateTick 锚点未命中');

/* StateTick 只在 -calcDemo 时启动 —— 让 -petAudit 也启动一个 */
b = fs.readFileSync(B, 'utf8');
if (!b.includes('-petAudit')) {
  b = b.replace('        /* -tagAudit：把容器标签的状态打出来 */',
`        /* -petAudit：定期打印阿侬与教程状态，供窗口级测试读 */
        foreach (var a5 in System.Environment.GetCommandLineArgs())
            if (a5 == "-petAudit") StartCoroutine(PetTick());
        /* -tagAudit：把容器标签的状态打出来 */`);
  b = b.replace('    void DumpTagAudit() {',
`    System.Collections.IEnumerator PetTick() {
        while (true) {
            yield return new WaitForSeconds(1.0f);
            Debug.Log("[PetState]\\n" + PetAudit());
        }
    }

    void DumpTagAudit() {`);
  fs.writeFileSync(B, b, 'utf8');
  console.log('  ✓ -petAudit 已加');
}
const c2 = fs.readFileSync(B, 'utf8');
console.log('  括号差:', (c2.match(/\{/g) || []).length - (c.match(/\}/g) || []).length);
