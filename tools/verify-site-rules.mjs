#!/usr/bin/env node
/**
 * Run the public site's JavaScript rules against the shared fixture.
 *
 * WHY. `site/assets/site.js` reimplements WeaveScorer, LeaderboardComposer and
 * GraderInjectionScanner so the published page runs offline. Its README said "the C# is the
 * authority; if they disagree, the JavaScript is wrong" — a note, and a note has no failure mode.
 *
 * Both sides now evaluate `tests/fixtures/site-rules.json`. This is the JavaScript half;
 * `SiteRuleFixtureTests` is the C# half, running the shipped types against the same cases. A rule
 * that moves on one side and not the other fails one of the two.
 *
 * Usage:  node tools/verify-site-rules.mjs
 * Exit:   0 all cases agree · 1 a case disagrees · 2 nothing was checked
 */
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const root = dirname(dirname(fileURLToPath(import.meta.url)));
const require = createRequire(import.meta.url);

const rules = require(join(root, 'site', 'assets', 'site.js'));
const fixture = JSON.parse(readFileSync(join(root, 'tests', 'fixtures', 'site-rules.json'), 'utf8'));

const failures = [];
let checked = 0;

const fail = (what, expected, actual) =>
  failures.push(`${what}\n    expected: ${expected}\n    actual:   ${actual}`);

// ------------------------------------------------------------------ schema

{
  const schema = fixture.weaveSchema;
  const shipped = rules.weaveSchema;
  checked++;
  if (shipped.length !== schema.dimensions.length) {
    fail('weave schema: dimension count', schema.dimensions.length, shipped.length);
  } else {
    schema.dimensions.forEach((d, i) => {
      if (shipped[i].key !== d.key || shipped[i].weight !== d.weight || shipped[i].posture !== d.posture) {
        fail(`weave schema: dimension ${i}`,
          `${d.key} ${d.weight} ${d.posture}`,
          `${shipped[i].key} ${shipped[i].weight} ${shipped[i].posture}`);
      }
    });
  }

  const observed = shipped
    .filter((d) => d.posture === 'Deterministic')
    .reduce((t, d) => t + d.weight, 0);
  if (observed !== schema.observedWeight) {
    fail('weave schema: observed weight', schema.observedWeight, observed);
  }
}

// ------------------------------------------------------------------ weave

for (const kase of fixture.weaveCases) {
  checked++;
  const card = rules.scoreEpisode(kase.signals);

  if (card.verdict !== kase.expect.verdict) {
    fail(`weave "${kase.name}": verdict`, kase.expect.verdict, card.verdict);
    continue;
  }

  if (kase.expect.tripped) {
    const got = card.tripped.join(',');
    const want = kase.expect.tripped.join(',');
    if (got !== want) {
      fail(`weave "${kase.name}": tripped floors`, want || '(none)', got || '(none)');
    }
  }

  if (kase.expect.earned !== undefined) {
    // The C# sums doubles in the same order, so an exact comparison would be fragile for the wrong
    // reason. A tolerance well below one rubric step keeps it meaningful.
    if (Math.abs(card.earned - kase.expect.earned) > 1e-9) {
      fail(`weave "${kase.name}": earned points`, kase.expect.earned, card.earned);
    }
    if (card.observedWeight !== kase.expect.observedWeight) {
      fail(`weave "${kase.name}": observed weight`, kase.expect.observedWeight, card.observedWeight);
    }
  }
}

// ------------------------------------------------------------------ leaderboard

for (const kase of fixture.leaderboardCases) {
  checked++;
  const composed = rules.leaderboard(kase.cells, kase.cohortMinimum);

  for (const want of kase.expect) {
    const cell = composed.find((c) => c.label === want.label);
    if (!cell) {
      fail(`leaderboard "${kase.name}": no cell for ${want.label}`, want.label, '(missing)');
      continue;
    }
    if (cell.comparable !== want.comparable) {
      fail(`leaderboard "${kase.name}": ${want.label} comparable`, want.comparable, cell.comparable);
    }
    if ((cell.rank ?? null) !== (want.rank ?? null)) {
      fail(`leaderboard "${kase.name}": ${want.label} rank`, want.rank, cell.rank);
    }
    if (want.reason !== undefined && cell.reason !== want.reason) {
      fail(`leaderboard "${kase.name}": ${want.label} reason`, want.reason, cell.reason);
    }
  }
}

// ------------------------------------------------------------------ injection

for (const kase of fixture.injectionCases) {
  checked++;
  const flagged = rules.looksLikeInjection(kase.text) !== null;
  if (flagged !== kase.flagged) {
    fail(`injection "${kase.text}"`, kase.flagged, flagged);
  }
}

// ------------------------------------------------------------------ report

// A verifier that checked nothing must not report success (R4). An empty fixture would otherwise
// look identical to a clean run — the exact shape this whole file exists to prevent elsewhere.
if (checked === 0) {
  console.error('verify-site-rules: nothing was checked; the fixture is empty or unreadable');
  process.exit(2);
}

if (failures.length) {
  console.error(`verify-site-rules: ${failures.length} disagreement(s) out of ${checked} case(s)\n`);
  for (const f of failures) {
    console.error('  ' + f + '\n');
  }
  console.error('The C# is the authority: tests/AiDe.Core.Tests/SiteRuleFixtureTests.cs runs the');
  console.error('same fixture against the shipped types. If that passes and this does not, the');
  console.error('JavaScript in site/assets/site.js has drifted.');
  process.exit(1);
}

console.log(`verify-site-rules: ${checked} case(s) agree with the fixture`);
