'use strict';
// Atom/charge-balanced corrections audited by ChemLab.Editor.PhaseAudit.
// Keep the upstream stock data untouched; preserve fixes on every C# regeneration.
const corrections = new Map([
  ['C2H2 + O2 -> 2CO2 + H2O', '2C2H2 + 5O2 -> 4CO2 + 2H2O'],
  ['H2S + O2 -> SO2 + H2O', '2H2S + 3O2 -> 2SO2 + 2H2O'],
  ['CH3CHO + 2[Ag(NH3)2]+ + 2OH- -> CH3COO- + 2Ag + 4NH3 + H2O', 'CH3CHO + 2[Ag(NH3)2]+ + 3OH- -> CH3COO- + 2Ag + 4NH3 + 2H2O'],
  ['CH3CHO + 2Cu(OH)2 + 2OH- -> CH3COO- + Cu2O + 4H2O', 'CH3CHO + 2Cu(OH)2 + OH- -> CH3COO- + Cu2O + 3H2O'],
  ['C6H12O6 + 2Cu(OH)2 + 2OH- -> C6H12O7 + Cu2O + 2H2O', 'C6H12O6 + 2Cu(OH)2 -> C6H12O7 + Cu2O + 2H2O'],
  ['HCOOH + 2[Ag(NH3)2]+ + 2OH- -> C2O4^2- + 2Ag + 4NH3 + 2H2O', 'HCOOH + 2[Ag(NH3)2]+ + 4OH- -> CO3^2- + 2Ag + 4NH3 + 3H2O'],
  ['NO3- + 8Al + 5OH- + 2H2O -> NH3 + 8AlO2-', '3NO3- + 8Al + 5OH- + 2H2O -> 3NH3 + 8AlO2-'],
  ['HCOOH + 2Cu2+ + 4OH- -> CO3^2- + Cu2O + 3H2O', 'HCOOH + 2Cu2+ + 6OH- -> CO3^2- + Cu2O + 4H2O']
]);
function correctRules(rules) {
  const equations = new Set(rules.map(r => r.eq));
  for (const [oldEquation, corrected] of corrections) {
    if (!equations.has(oldEquation) && !equations.has(corrected)) {
      throw new Error(`Upstream reaction changed; review correction before generating: ${oldEquation}`);
    }
  }
  return rules.map(rule => ({...rule, eq: corrections.get(rule.eq) || rule.eq}));
}
module.exports = { corrections, correctRules };
