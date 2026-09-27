// Explicit straight-chain scope. Physical data and reaction integration must be
// independently sourced before these records are emitted into the reagent data.
const pending = [];
for (const [family, start, prefix, suffix] of [
  ['alkane', 5, 'n-', 'ane'], ['alkene', 4, '1-', 'ene'],
  ['alcohol', 4, '1-', 'anol'], ['aldehyde', 4, '', 'anal'],
  ['carboxylic_acid', 4, '', 'anoic acid']
]) {
  for (let carbon = start; carbon <= 8; carbon++) {
    const stem = ['', 'meth', 'eth', 'prop', 'but', 'pent', 'hex', 'hept', 'oct'][carbon];
    const hydrogen = family === 'alkane' || family === 'alcohol' ? 2 * carbon + 2 : 2 * carbon;
    const oxygen = family === 'carboxylic_acid' ? 'O2' : ['alcohol', 'aldehyde'].includes(family) ? 'O' : '';
    pending.push({ family, carbon, name: prefix + stem + suffix, formula: `C${carbon}H${hydrogen}${oxygen}` });
  }
}
module.exports = pending;
if (require.main === module) {
  if (pending.length !== 24 || new Set(pending.map(x => x.name)).size !== 24) throw new Error('Invalid expansion scope');
  process.stdout.write(JSON.stringify({ count: pending.length, status: 'pending physical data and reaction integration', compounds: pending }, null, 2) + '\n');
}
