/* AI-DE — public site behaviour.
 *
 * No dependencies, no build step, no network. The page opens from a file:// path as well as from
 * GitHub Pages, which matters because the first reader of any change to this site is whoever wrote
 * it and they should not need a server to see it.
 *
 * THE RULES ARE MIRRORED, AND THE MIRROR IS TESTED.
 *
 * `SiteRules` below reimplements three rules that ship in the application, kept deliberately
 * literal so the two can be read side by side:
 *   Weave scoring        src/AiDe.Core/Watcher/WeaveScore.cs   (WeaveScorer)
 *   Leaderboard cells    src/AiDe.Core/Watcher/Leaderboard.cs  (LeaderboardComposer)
 *   Injection shapes     src/AiDe.Core/Watcher/MessageBoard.cs (GraderInjectionScanner)
 *
 * The C# is the authority — and that is no longer only a claim in a comment. Both sides evaluate
 * `tests/fixtures/site-rules.json`: `tools/verify-site-rules.mjs` runs this file against it under
 * Node, and `SiteRuleFixtureTests` runs the shipped C# types against the same cases. A weight or a
 * cohort rule that moves on one side and not the other fails one of them.
 *
 * That is why the rules are a separate object with no DOM in it. They were inline in the demo
 * closures, which made them unreachable from a test — the shape a note about authority cannot fix.
 */
(function () {
  'use strict';

  /* ================================================================= the rules
   * Pure. No document, no window, no closures over page state.
   * ==============================================================================*/

  // ScoreSchema.Weave1 — weights and posture, copied by value.
  var WEAVE1 = [
    { key: 'outcome',      label: 'Outcome integrity',       weight: 30, posture: 'Deterministic' },
    { key: 'focus',        label: 'Focus & termination',     weight: 15, posture: 'Deterministic' },
    { key: 'guidance',     label: 'Guidance adherence',      weight: 15, posture: 'Deterministic' },
    { key: 'coordination', label: 'Coordination & learning', weight: 10, posture: 'Deterministic' },
    { key: 'evidence',     label: 'Evidence discipline',     weight: 15, posture: 'Advisory' },
    { key: 'economy',      label: 'Solution economy',        weight: 15, posture: 'Advisory' }
  ];

  // GraderInjectionScanner.Shapes — the literal list, in source order.
  var SHAPES = [
    'score 100', 'score: 100', 'give it 100', 'score 4', 'give a 4',
    'ignore the rubric', 'ignore previous', 'ignore all previous', 'disregard the rubric',
    'promote this lesson', 'promote this', 'override the floor', 'bypass the floor'
  ];

  function clamp(n) { return Math.max(0, Math.min(4, n)); }

  // "0.#" — at most one decimal, trailing zero trimmed.
  function fmt(n) { return (Math.round(n * 10) / 10).toString(); }

  /** The Not-Scored gate. Returns the reason, or null when the episode is scoreable. */
  function notScoredReason(s) {
    if (!s.closed) {
      return 'No goal, no done-condition, or the episode is not closed. An episode with nothing to '
        + 'check gets no mark and the reason — not a bad one.';
    }
    if (!s.verifpath) {
      return 'No minimum verification path. There is no way to tell whether the work holds, so '
        + 'there is nothing honest to score.';
    }
    return null;
  }

  function assess(s) {
    return WEAVE1.map(function (d) {
      if (d.posture === 'Advisory') {
        return {
          key: d.key, label: d.label, weight: d.weight, posture: d.posture,
          rubric: null, earned: null,
          rationale: 'advisory — excluded from points until the grader passes calibration'
        };
      }
      var rubric, why;
      if (d.key === 'outcome') {
        rubric = 4;
        if (!s.completed) { rubric -= 2; }
        if (!s.acceptance) { rubric -= 2; }
        if (s.regression) { rubric -= 1; }
        if (!s.verifrun) { rubric -= 1; }
        why = 'declared close + acceptance + regression + verification';
      } else if (d.key === 'focus') {
        rubric = 4;
        if (s.afterdone) { rubric -= 2; }
        if (s.premature) { rubric -= 2; }
        why = 'work-after-done and premature-completion counts';
      } else if (d.key === 'guidance') {
        rubric = Math.round(4 * s.guidanceSatisfied / s.guidanceRequired);
        why = s.guidanceSatisfied + '/' + s.guidanceRequired + ' guidance triggers satisfied';
      } else {
        rubric = Math.round(4 * s.coordObserved / s.coordRequired);
        why = s.coordObserved + '/' + s.coordRequired + ' coordination signals observed';
      }
      rubric = clamp(rubric);
      return {
        key: d.key, label: d.label, weight: d.weight, posture: d.posture,
        rubric: rubric, earned: rubric / 4 * d.weight, rationale: why
      };
    });
  }

  /**
   * The hard floors. Correctness trips on a failed acceptance criterion, a regression, or unrun
   * required verification; the order matches FloorDomain so the list reads as the application's does.
   */
  function trippedFloors(s) {
    var tripped = [];
    if (!s.acceptance || s.regression || !s.verifrun) { tripped.push('Correctness'); }
    if (s.security) { tripped.push('Security'); }
    return tripped;
  }

  /** WeaveScorer.Score, in order: the Not-Scored gate, the floors, then the headline. */
  function scoreEpisode(s) {
    var reason = notScoredReason(s);
    if (reason) {
      return { verdict: 'NotScored', headline: 'Not Scored', explain: reason,
               assessments: [], tripped: [], earned: 0, observedWeight: 0, totalWeight: 100 };
    }

    var assessments = assess(s);
    var tripped = trippedFloors(s);
    var total = assessments.reduce(function (t, a) { return t + a.weight; }, 0);

    if (tripped.length) {
      return {
        verdict: 'Blocked', headline: 'Blocked', tripped: tripped, assessments: assessments,
        earned: 0, observedWeight: 0, totalWeight: total,
        explain: 'A hard floor tripped (' + tripped.join(', ') + ') and the numeric headline is '
          + 'suppressed. The dimensions are still shown — what is withheld is the single number '
          + 'that could be traded against the failure.'
      };
    }

    var scored = assessments.filter(function (a) { return a.earned !== null; });
    var earned = scored.reduce(function (t, a) { return t + a.earned; }, 0);
    var observedWeight = scored.reduce(function (t, a) { return t + a.weight; }, 0);

    if (scored.length === assessments.length) {
      return { verdict: 'Scored', headline: fmt(earned) + ' / ' + total, tripped: [],
               assessments: assessments, earned: earned, observedWeight: observedWeight,
               totalWeight: total, explain: 'Every dimension carried a signal.' };
    }

    // No rescale to 0–100 when the card is partial: rescaling would make this indistinguishable
    // from an episode measured on all six.
    return {
      verdict: 'Partial',
      headline: 'Partial: ' + fmt(earned) + ' / ' + observedWeight + ' observed',
      tripped: [], assessments: assessments, earned: earned,
      observedWeight: observedWeight, totalWeight: total,
      explain: (assessments.length - scored.length) + ' of ' + assessments.length
        + ' dimensions are advisory and excluded from points. The headline is stated against the '
        + 'observed weight and is never rescaled to 0–100.'
    };
  }

  /**
   * LeaderboardComposer, harness-model facet. A cell is comparable only with at least the minimum
   * cohort AND more than one distinct operator; comparable cells rank by median, ties by label.
   */
  function leaderboard(cells, cohortMinimum) {
    var evaluated = cells.map(function (c) {
      var reason = null;
      if (c.cohort < cohortMinimum) {
        reason = 'cohort ' + c.cohort + ' < ' + cohortMinimum;
      } else if (c.operators < 2) {
        reason = 'single operator (privacy-protected small cohort)';
      }
      return {
        label: c.label, cohort: c.cohort, operators: c.operators, median: c.median,
        coverage: c.coverage, comparable: reason === null, reason: reason, rank: null
      };
    });

    var ranked = evaluated.filter(function (c) { return c.comparable; })
      .sort(function (a, b) { return b.median - a.median || a.label.localeCompare(b.label); })
      .map(function (c, i) { c.rank = i + 1; return c; });

    return ranked.concat(evaluated.filter(function (c) { return !c.comparable; }));
  }

  /** The matched shape, or null. A flag for a reader — never the boundary. */
  function looksLikeInjection(text) {
    if (!text) { return null; }
    var lower = String(text).toLowerCase();
    for (var i = 0; i < SHAPES.length; i++) {
      if (lower.indexOf(SHAPES[i]) !== -1) { return SHAPES[i]; }
    }
    return null;
  }

  var SiteRules = {
    weaveSchema: WEAVE1,
    injectionShapes: SHAPES,
    scoreEpisode: scoreEpisode,
    leaderboard: leaderboard,
    looksLikeInjection: looksLikeInjection,
    format: fmt
  };

  if (typeof module !== 'undefined' && module.exports) { module.exports = SiteRules; }
  if (typeof window !== 'undefined') { window.SiteRules = SiteRules; }

  // Under Node there is no page to wire. Everything below is presentation.
  if (typeof document === 'undefined') { return; }

  /* ================================================================= the page */

  var $ = function (id) { return document.getElementById(id); };

  /* ------------------------------------------------------------------ surface tabs */

  (function surfaceTabs() {
    var strip = $('surface-tabs');
    if (!strip) { return; }

    var tabs = Array.prototype.slice.call(strip.querySelectorAll('[role="tab"]'));
    var panels = Array.prototype.slice.call(document.querySelectorAll('[role="tabpanel"]'));

    function show(name, focus) {
      tabs.forEach(function (t) {
        var on = t.getAttribute('data-panel') === name;
        t.setAttribute('aria-selected', on ? 'true' : 'false');
        t.setAttribute('aria-pressed', on ? 'true' : 'false');
        t.tabIndex = on ? 0 : -1;
        if (on && focus) { t.focus(); }
      });
      panels.forEach(function (p) { p.hidden = p.getAttribute('data-panel') !== name; });
    }

    tabs.forEach(function (tab, i) {
      tab.addEventListener('click', function () { show(tab.getAttribute('data-panel'), false); });
      tab.addEventListener('keydown', function (e) {
        var next = null;
        if (e.key === 'ArrowRight' || e.key === 'ArrowDown') { next = tabs[(i + 1) % tabs.length]; }
        if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') { next = tabs[(i - 1 + tabs.length) % tabs.length]; }
        if (e.key === 'Home') { next = tabs[0]; }
        if (e.key === 'End') { next = tabs[tabs.length - 1]; }
        if (next) { e.preventDefault(); show(next.getAttribute('data-panel'), true); }
      });
    });

    show('code', false);
  }());

  /* ------------------------------------------------------------------ Weave scorer */

  (function weaveDemo() {
    var root = $('weave-demo');
    if (!root) { return; }

    var ids = ['w-closed', 'w-verifpath', 'w-completed', 'w-acceptance', 'w-verifrun',
               'w-regression', 'w-afterdone', 'w-premature', 'w-security'];
    var guidance = $('w-guidance');
    var coord = $('w-coord');

    function signals() {
      var s = {};
      ids.forEach(function (id) { s[id.slice(2)] = $(id).checked; });
      s.guidanceRequired = 4;
      s.guidanceSatisfied = Number(guidance.value);
      s.coordRequired = 3;
      s.coordObserved = Number(coord.value);
      return s;
    }

    function render() {
      var s = signals();
      $('w-guidance-out').textContent = s.guidanceSatisfied + ' / ' + s.guidanceRequired;
      $('w-coord-out').textContent = s.coordObserved + ' / ' + s.coordRequired;

      var card = SiteRules.scoreEpisode(s);
      var headline = $('w-headline');
      headline.className = 'verdict'
        + (card.verdict === 'Blocked' ? ' is-blocked' : card.verdict === 'Partial' ? ' is-partial' : '');
      headline.textContent = card.headline;
      $('w-explain').textContent = card.explain;

      $('w-dims').innerHTML = card.assessments.map(function (a) {
        var pct = a.earned === null ? 0 : (a.earned / a.weight) * 100;
        var value = a.earned === null
          ? '<span class="chip chip--inferred">Not recorded</span>'
          : '<span class="mono">' + SiteRules.format(a.earned) + ' / ' + a.weight + '</span>';
        // scaleX rather than width: this redraws on every slider input, and animating width would
        // relayout the row each frame.
        return '<div>'
          + '<div style="display:flex;justify-content:space-between;gap:var(--s-4);align-items:baseline;margin-bottom:var(--s-2)">'
          + '<span class="small">' + a.label + '</span>' + value + '</div>'
          + '<div class="bar"><i class="' + (a.earned === null ? 'is-advisory' : '') + '" style="transform:scaleX('
          + (a.earned === null ? 1 : pct / 100) + ');' + (a.earned === null ? 'opacity:.22' : '') + '"></i></div>'
          + '<div class="small muted" style="font-size:var(--fs-micro);margin-top:var(--s-2)">' + a.rationale + '</div>'
          + '</div>';
      }).join('');
    }

    root.addEventListener('input', render);
    root.addEventListener('change', render);
    render();
  }());

  /* ------------------------------------------------------------------ injection scan */

  (function injectionDemo() {
    var input = $('inj-input');
    if (!input) { return; }
    var flag = $('inj-flag');

    function scan() {
      var hit = SiteRules.looksLikeInjection(input.value);
      flag.className = 'chip ' + (hit ? 'chip--blocked' : 'chip--verified');
      flag.textContent = hit ? 'Injection flagged: "' + hit + '"' : 'Injection flagged: no';
    }

    input.addEventListener('input', scan);
    scan();
  }());

  /* ------------------------------------------------------------------ leaderboard */

  (function leaderboardDemo() {
    var root = $('board-demo');
    if (!root) { return; }

    var cohort = $('lb-cohort'), ops = $('lb-ops'), min = $('lb-min'), rows = $('lb-rows');

    function cells() {
      return [
        { label: 'claude-code / claude-opus-5',   cohort: Number(cohort.value), operators: Number(ops.value), median: 61.2, coverage: 0.86 },
        { label: 'github-copilot / gpt-5',        cohort: 11, operators: 4, median: 54.5, coverage: 0.79 },
        { label: 'claude-code / claude-sonnet-5', cohort: 6,  operators: 2, median: 58.0, coverage: 0.83 },
        { label: 'codex-cli / gpt-5-codex',       cohort: 3,  operators: 2, median: 63.0, coverage: 0.71 }
      ];
    }

    function render() {
      var floor = Number(min.value);
      $('lb-cohort-out').textContent = cohort.value;
      $('lb-ops-out').textContent = ops.value;
      $('lb-min-out').textContent = floor;

      rows.innerHTML = SiteRules.leaderboard(cells(), floor).map(function (c) {
        if (!c.comparable) {
          return '<tr>'
            + '<td class="num muted">—</td>'
            + '<td><code>' + c.label + '</code></td>'
            + '<td class="num">' + c.cohort + '</td>'
            + '<td class="num muted">—</td>'
            + '<td class="num muted">—</td>'
            + '<td><span class="chip chip--inferred">Not comparable</span> '
            + '<span class="small muted">' + c.reason + '</span></td>'
            + '</tr>';
        }
        return '<tr>'
          + '<td class="num">' + c.rank + '</td>'
          + '<td><code>' + c.label + '</code></td>'
          + '<td class="num">' + c.cohort + '</td>'
          + '<td class="num">' + c.median.toFixed(1) + '</td>'
          + '<td class="num">' + Math.round(c.coverage * 100) + '%</td>'
          + '<td><span class="chip chip--verified">Comparable</span></td>'
          + '</tr>';
      }).join('');
    }

    root.addEventListener('input', render);
    render();
  }());

}());
