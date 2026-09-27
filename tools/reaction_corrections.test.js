'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const { corrections, correctRules } = require('./reaction_corrections');
const original = () => [...corrections.keys()].map(eq => ({eq, obs: 'preserved', fx: {heat: 2}}));
test('all eight audited equations survive generation', () => {
  assert.equal(corrections.size, 8);
  assert.deepEqual(correctRules(original()).map(r => r.eq), [...corrections.values()]);
});
test('already-corrected upstream is accepted and correction is idempotent', () => {
  const fixed = correctRules(original());
  assert.deepEqual(correctRules(fixed), fixed);
});
test('upstream objects and reaction metadata are preserved', () => {
  const input = original(); const snapshot = JSON.parse(JSON.stringify(input));
  const result = correctRules(input);
  assert.deepEqual(input, snapshot);
  assert.equal(result[0].obs, input[0].obs);
  assert.deepEqual(result[0].fx, input[0].fx);
});
test('unrelated equations are unchanged', () => {
  const extra = {eq: '2H2 + O2 -> 2H2O', obs: 'water'};
  assert.deepEqual(correctRules([...original(), extra]).at(-1), extra);
});
test('unexpected upstream changes fail before a file is written', () => {
  assert.throws(() => correctRules(original().slice(1)), /Upstream reaction changed/);
});
