const fs = require('fs');
let n = 0;

/* 方程标签跟着容器走：Ev.Pos 是「反应发生那一刻」的快照，
   容器被拖走后标签还停在原地。改成每次 Sync 都读容器的当前位置。 */
const E = 'D:/ChemLabUnity/Assets/Scripts/Game/EquationTag.cs';
let e = fs.readFileSync(E, 'utf8');
const oldL = '        Vector3 center = Ev.Pos + Vector3.up * (1.85f + Slot * 0.62f) + Vector3.up * worldH * 0.5f;';
const newL = `        /* 用容器的**当前位置**，不是 Ev.Pos ——
           Ev.Pos 是反应发生那一刻的快照，容器被拖走后标签会留在原地。
           容器已经删掉时才退回快照。 */
        Vector3 basePos = (Ev.Vessel != null) ? Ev.Vessel.Pos : Ev.Pos;
        float liftY = (Ev.Vessel != null) ? Ev.Vessel.LiftY : 0f;
        Vector3 center = basePos + Vector3.up * (liftY + 1.85f + Slot * 0.62f + worldH * 0.5f);`;
if (e.includes(oldL)) { e = e.replace(oldL, newL); fs.writeFileSync(E, e, 'utf8'); n++; console.log('  ✓ 方程标签改为跟随容器'); }
else console.log('  ✗ 锚点未命中');

/* VesselFx 的 Sync 也检查一下：把缓存的 origin 去掉 */
const P = 'D:/ChemLabUnity/Assets/Scripts/Game/Particles.cs';
let p = fs.readFileSync(P, 'utf8');
if (p.includes('Vector3 origin = transform.position;')) {
  console.log('  · VesselFx 用 transform.position（世界坐标），本身会跟随容器');
  /* 看看有没有把 origin 缓存到别处 */
  const uses = (p.match(/origin/g) || []).length;
  console.log('  · origin 在 Particles.cs 里出现', uses, '次');
}
console.log('改动处数:', n);
