/* AI-DE — public site behaviour.
 *
 * No dependencies, no build step, no network. The page opens from a file:// path as well as from
 * GitHub Pages, which matters because the first reader of any change to this site is whoever wrote
 * it and they should not need a server to see it.
 *
 * Every rule below is a reimplementation of one that ships in the application, kept deliberately
 * literal so the two can be read side by side:
 *   Weave scoring        src/AiDe.Core/Watcher/WeaveScore.cs   (WeaveScorer)
 *   Leaderboard cells    src/AiDe.Core/Watcher/Leaderboard.cs  (LeaderboardComposer)
 *   Injection shapes     src/AiDe.Core/Watcher/MessageBoard.cs (GraderInjectionScanner)
 * The C# is the authority. If they ever disagree, this file is the one that is wrong.
 */
(function () {
  'use strict';

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

  // ScoreSchema.Weave1 — weights and posture, copied by value.
  var WEAVE1 = [
    { key: 'outcome',      label: 'Outcome integrity',      weight: 30, posture: 'Deterministic' },
    { key: 'focus',        label: 'Focus & termination',    weight: 15, posture: 'Deterministic' },
    { key: 'guidance',     label: 'Guidance adherence',     weight: 15, posture: 'Deterministic' },
    { key: 'coordination', label: 'Coordination & learning', weight: 10, posture: 'Deterministic' },
    { key: 'evidence',     label: 'Evidence discipline',    weight: 15, posture: 'Advisory' },
    { key: 'economy',      label: 'Solution economy',       weight: 15, posture: 'Advisory' }
  ];

  function clamp(n) { return Math.max(0, Math.min(4, n)); }

  // "0.#" — at most one decimal, trailing zero trimmed.
  function fmt(n) { return (Math.round(n * 10) / 10).toString(); }

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

    function assess(s) {
      return WEAVE1.map(function (d) {
        if (d.posture === 'Advisory') {
          return Object.assign({}, d, {
            rubric: null, earned: null,
            rationale: 'advisory — excluded from points until the grader passes calibration'
          });
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
        return Object.assign({}, d, { rubric: rubric, earned: rubric / 4 * d.weight, rationale: why });
      });
    }

    function floors(s) {
      var tripped = [];
      // Correctness trips on a failed acceptance criterion, a regression, or unrun required
      // verification. Order matches FloorDomain so the list reads the same as the application's.
      if (!s.acceptance || s.regression || !s.verifrun) { tripped.push('Correctness'); }
      if (s.security) { tripped.push('Security'); }
      return tripped;
    }

    function render() {
      var s = signals();
      $('w-guidance-out').textContent = s.guidanceSatisfied + ' / ' + s.guidanceRequired;
      $('w-coord-out').textContent = s.coordObserved + ' / ' + s.coordRequired;

      var headline = $('w-headline');
      var explain = $('w-explain');
      var dims = $('w-dims');
      headline.className = 'verdict';

      // 1. The Not-Scored gate runs before anything is assessed.
      if (!s.closed || !s.verifpath) {
        headline.textContent = 'Not Scored';
        explain.textContent = !s.closed
          ? 'No goal, no done-condition, or the episode is not closed. An episode with nothing to check gets no mark and the reason — not a bad one.'
          : 'No minimum verification path. There is no way to tell whether the work holds, so there is nothing honest to score.';
        dims.innerHTML = '';
        return;
      }

      var assessments = assess(s);
      var tripped = floors(s);

      dims.innerHTML = assessments.map(function (a) {
        var pct = a.earned === null ? 0 : (a.earned / a.weight) * 100;
        var value = a.earned === null
          ? '<span class="chip chip--inferred">Not recorded</span>'
          : '<span class="mono">' + fmt(a.earned) + ' / ' + a.weight + '</span>';
        return '<div>'
          + '<div style="display:flex;justify-content:space-between;gap:var(--s-4);align-items:baseline;margin-bottom:var(--s-2)">'
          + '<span class="small">' + a.label + '</span>' + value + '</div>'
          // scaleX rather than width: this redraws on every slider input, and animating width
          // would relayout the row each frame.
          + '<div class="bar"><i class="' + (a.earned === null ? 'is-advisory' : '') + '" style="transform:scaleX('
          + (a.earned === null ? 1 : pct / 100) + ');' + (a.earned === null ? 'opacity:.22' : '') + '"></i></div>'
          + '<div class="small muted" style="font-size:var(--fs-micro);margin-top:var(--s-2)">' + a.rationale + '</div>'
          + '</div>';
      }).join('');

      // 2. A tripped floor blocks the card and suppresses the numeric headline.
      if (tripped.length) {
        headline.className = 'verdict is-blocked';
        headline.textContent = 'Blocked';
        explain.innerHTML = 'A hard floor tripped (<strong>' + tripped.join(', ')
          + '</strong>) and the numeric headline is suppressed. The dimensions are still shown — '
          + 'what is withheld is the single number that could be traded against the failure.';
        return;
      }

      // 3. Verdict and headline. No rescale to 0–100 when the card is partial.
      var scored = assessments.filter(function (a) { return a.earned !== null; });
      var earned = scored.reduce(function (t, a) { return t + a.earned; }, 0);
      var observedWeight = scored.reduce(function (t, a) { return t + a.weight; }, 0);
      var total = assessments.reduce(function (t, a) { return t + a.weight; }, 0);

      if (scored.length === assessments.length) {
        headline.textContent = fmt(earned) + ' / ' + total;
        explain.textContent = 'Every dimension carried a signal.';
      } else {
        headline.className = 'verdict is-partial';
        headline.textContent = 'Partial: ' + fmt(earned) + ' / ' + observedWeight + ' observed';
        explain.innerHTML = (assessments.length - scored.length) + ' of ' + assessments.length
          + ' dimensions are advisory and excluded from points. The headline is stated against the '
          + '<strong>observed</strong> weight and is never rescaled to 0–100 — rescaling would make '
          + 'this indistinguishable from an episode measured on all six.';
      }
    }

    root.addEventListener('input', render);
    root.addEventListener('change', render);
    render();
  }());

  /* ------------------------------------------------------------------ injection scan */

  // GraderInjectionScanner.Shapes — the literal list, in source order.
  var SHAPES = [
    'score 100', 'score: 100', 'give it 100', 'score 4', 'give a 4',
    'ignore the rubric', 'ignore previous', 'ignore all previous', 'disregard the rubric',
    'promote this lesson', 'promote this', 'override the floor', 'bypass the floor'
  ];

  (function injectionDemo() {
    var input = $('inj-input');
    if (!input) { return; }
    var flag = $('inj-flag');

    function scan() {
      var text = input.value.toLowerCase();
      var hit = null;
      for (var i = 0; i < SHAPES.length; i++) {
        if (text.indexOf(SHAPES[i]) !== -1) { hit = SHAPES[i]; break; }
      }
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

      var evaluated = cells().map(function (c) {
        var reason = null;
        if (c.cohort < floor) {
          reason = 'cohort ' + c.cohort + ' < ' + floor;
        } else if (c.operators < 2) {
          reason = 'single operator (privacy-protected small cohort)';
        }
        return Object.assign({}, c, { comparable: reason === null, reason: reason });
      });

      // Comparable cells rank by median Weave, best first, ties broken by label. Not-comparable
      // cells keep their place in the table but never receive a rank.
      var ranked = evaluated.filter(function (c) { return c.comparable; })
        .sort(function (a, b) { return b.median - a.median || a.label.localeCompare(b.label); })
        .map(function (c, i) { return Object.assign({}, c, { rank: i + 1 }); });
      var rest = evaluated.filter(function (c) { return !c.comparable; });

      rows.innerHTML = ranked.concat(rest).map(function (c) {
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
