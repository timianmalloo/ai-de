namespace AiDe.App.Workbench;

/// <summary>
/// The canvas page. Its own file because it is a contract, not a resource.
/// </summary>
/// <remarks>
/// <para><b>The boundary handlers in here are the only way out of the canvas.</b> WPF's Tab traversal
/// cannot reach or leave a hosted browser, so a page that loses them strands the user inside the
/// graph with no keyboard route back. That is why it is inlined rather than deployed as an asset —
/// it cannot be separated from the control that depends on it — and why `P2-FOCUS-03` drives this
/// exact markup through a real WebView2.</para>
///
/// <para><b>Every handler is bound at the DOCUMENT level against the current node list.</b> The graph
/// re-renders on every navigation, so element-scoped handlers would be lost with the nodes they were
/// attached to and the keyboard trap would return the first time someone clicked a node.</para>
///
/// <para><b>2D and 3D are two projections of one node list.</b> The DOM nodes are the same focusable
/// <c>.node</c> spans in both modes — 3D only changes where they are drawn and how big/opaque they
/// are by depth — so the keyboard-trap contract, the <c>__tabsSeen</c> counter, and node activation
/// are identical in either mode. 3D is self-contained (a hand-rolled sphere layout + perspective
/// projection, no external library and no CDN, because the page is delivered by
/// <c>NavigateToString</c> with no network). It defaults to 2D so the focus probe and its tests are
/// unaffected.</para>
/// </remarks>
internal static class CanvasPage
{
    internal const string Html = """
        <!doctype html>
        <html lang="en">
        <head><meta charset="utf-8"><title>Graph canvas</title>
        <style>
          :root { color-scheme: dark; }
          body { font: 14px system-ui, sans-serif; margin: 0; padding: 12px 16px; background: #12151A; color: #E4E9EF; }
          header { display: flex; align-items: baseline; gap: 12px; }
          h1 { font-size: 15px; margin: 0; }
          button.chrome { font: inherit; background: #1A1F26; color: #E4E9EF; border: 1px solid #2A313B;
                  border-radius: 8px; padding: 2px 10px; cursor: pointer; }
          button.chrome[disabled] { opacity: .4; cursor: default; }
          #fit { margin-left: auto; }
          #mode { margin-left: 6px; }
          #caption { color: #98A3B2; margin: 6px 0 0; }
          #warn { color: #D8A650; margin: 4px 0 0; min-height: 1em; }
          #stage { position: relative; height: 440px; margin-top: 10px; border-radius: 10px;
                   background: #0D1014; overflow: hidden; }
          #stage.grab { cursor: grab; }
          #stage.grabbing { cursor: grabbing; }
          svg { position: absolute; inset: 0; width: 100%; height: 100%; }
          .node { position: absolute; transform: translate(-50%, -50%); cursor: pointer;
                  width: var(--r, 12px); height: var(--r, 12px); }
          /* The glyph is a degree-sized dot, not an opaque card: cards occlude each other and hide
             the edges behind them (DC-036). */
          .node::before { content: ''; display: block; width: 100%; height: 100%; border-radius: 50%;
                  box-sizing: border-box; background: var(--dot, #47566B); border: 1px solid var(--dotb, #63748C); }
          .node.root::before { background: #21303f; border-color: #5B9DD9; }
          /* The label rides under the dot and appears on demand — a dense graph stays readable, and a
             focused or hovered node still tells you what it is. The root is always labelled. */
          .node > .lbl { position: absolute; left: 50%; top: 100%; transform: translateX(-50%);
                  margin-top: 3px; padding: 1px 6px; border-radius: 6px; background: rgba(20,25,32,.92);
                  border: 1px solid #2A313B; color: #E4E9EF; font-size: 12px; white-space: nowrap;
                  max-width: 200px; overflow: hidden; text-overflow: ellipsis; opacity: 0;
                  pointer-events: none; transition: opacity .12s ease; }
          .node:hover > .lbl, .node:focus > .lbl, .node.root > .lbl { opacity: 1; }
          .node:focus { outline: none; }
          .node:focus::before { outline: 2px solid #5B9DD9; outline-offset: 2px; }
          @media (prefers-reduced-motion: reduce) { .node > .lbl { transition: none; } }
          .legend { color: #98A3B2; font-size: 12px; margin-top: 8px; }
          .legend b { color: #E4E9EF; font-weight: 600; }
        </style></head>
        <body>
          <header>
            <h1>Graph canvas</h1>
            <button id="back" class="chrome" disabled>&#8592; Back</button>
            <button id="fit" class="chrome" title="Fit the graph to the view (2D)">Fit</button>
            <button id="mode" class="chrome" title="Toggle 2D / 3D (press 2 or 3)" aria-label="Switch to 3D view">View in 3D</button>
          </header>
          <p id="caption">Waiting for the workspace&#8230;</p>
          <p id="warn"></p>
          <div id="stage"><svg id="edges"></svg></div>
          <p class="legend" id="legend"></p>
          <script>
            function post(msg) { window.chrome.webview.postMessage(msg); }
            function leave(direction) { post({ kind: 'focus.leave', direction: direction }); }

            var history = [];
            var current = null;
            var backButton = document.getElementById('back');
            var modeButton = document.getElementById('mode');
            var fitButton = document.getElementById('fit');
            var stage = document.getElementById('stage');
            stage.classList.add('grab');   // 2D pans, 3D rotates — both are grab gestures

            // 2D pan/zoom. The dots carry no absolute meaning beyond "attached to the root", so panning
            // and zooming the settled layout is the natural way to read a spread-out graph. 3D keeps
            // its own drag-to-rotate; this view transform applies only in 2D (place() ignores it in 3D).
            var view2d = { scale: 1, tx: 0, ty: 0 };

            // Two projections of one node list. '2d' is the default so the focus probe (which runs
            // in 2D) and its tests are unaffected by 3D.
            var mode = '2d';
            var rotX = -0.35, rotY = 0.6;   // a gentle default 3D framing
            var lastGraph = null;
            var records = [];   // { id, isRoot, el, p2:{x,y}, p3:{x,y,z} }
            var edgeRecs = [];  // { from, to, line }

            function activate(nodeId) {
              if (!nodeId || nodeId === current) { return; }
              if (current) { history.push(current); }
              post({ kind: 'node.activate', nodeId: nodeId });
            }

            backButton.addEventListener('click', function () {
              if (!history.length) { return; }
              var previous = history.pop();
              current = null;
              post({ kind: 'node.activate', nodeId: previous });
            });

            function setMode(next) {
              if (next === mode) { return; }
              mode = next;
              modeButton.textContent = mode === '3d' ? 'View in 2D' : 'View in 3D';
              modeButton.setAttribute('aria-label', mode === '3d' ? 'Switch to 2D view' : 'Switch to 3D view');
              place();
            }
            modeButton.addEventListener('click', function () { setMode(mode === '3d' ? '2d' : '3d'); });
            fitButton.addEventListener('click', function () {
              if (mode !== '2d') { setMode('2d'); }   // Fit frames the 2D layout
              fit();
              place();
            });

            function focusable() { return Array.prototype.slice.call(document.querySelectorAll('.node')); }

            function claimFocus() {
              var nodes = focusable();
              var active = document.activeElement;
              if (nodes.length && (!active || active === document.body)) { nodes[0].focus(); }
            }
            window.addEventListener('focus', claimFocus);

            document.addEventListener('keydown', function (e) {
              if (e.key === 'Escape') { e.preventDefault(); leave('restore'); return; }

              // Mode switch. Digit keys are free (Enter/Space activate, Tab traverses) and only act
              // while the canvas has focus, so they do not collide with host shortcuts.
              if (e.key === '2') { e.preventDefault(); setMode('2d'); return; }
              if (e.key === '3') { e.preventDefault(); setMode('3d'); return; }

              if (e.key === 'Enter' || e.key === ' ') {
                var target = document.activeElement;
                if (target && target.classList.contains('node')) {
                  e.preventDefault();
                  activate(target.getAttribute('data-id'));
                }
                return;
              }

              if ((e.key === 'Backspace' || (e.key === 'ArrowLeft' && e.altKey)) && history.length) {
                e.preventDefault(); backButton.click(); return;
              }

              if (e.key !== 'Tab') { return; }
              window.__tabsSeen = (window.__tabsSeen || 0) + 1;

              var nodes = focusable();
              var active = document.activeElement;

              if (nodes.length === 0) { e.preventDefault(); leave(e.shiftKey ? 'backward' : 'forward'); return; }
              if (!e.shiftKey && active === nodes[nodes.length - 1]) { e.preventDefault(); leave('forward'); }
              else if (e.shiftKey && (active === nodes[0] || active === document.body)) {
                e.preventDefault(); leave('backward');
              }
            });

            // Drag on empty stage: pan in 2D, rotate in 3D. A drag that starts on a node is left
            // alone so its click-to-activate is never stolen by the gesture.
            var dragging = false, dragX = 0, dragY = 0;
            stage.addEventListener('pointerdown', function (e) {
              if (e.target.classList && e.target.classList.contains('node')) { return; }
              dragging = true; dragX = e.clientX; dragY = e.clientY;
              stage.classList.add('grabbing');
              stage.setPointerCapture(e.pointerId);
            });
            stage.addEventListener('pointermove', function (e) {
              if (!dragging) { return; }
              if (mode === '3d') {
                rotY += (e.clientX - dragX) * 0.01;
                rotX += (e.clientY - dragY) * 0.01;
                rotX = Math.max(-1.4, Math.min(1.4, rotX));
              } else {
                view2d.tx += (e.clientX - dragX);
                view2d.ty += (e.clientY - dragY);
              }
              dragX = e.clientX; dragY = e.clientY;
              place();
            });
            function endDrag() { dragging = false; stage.classList.remove('grabbing'); }
            stage.addEventListener('pointerup', endDrag);
            stage.addEventListener('pointercancel', endDrag);

            // Wheel zoom in 2D, keeping the point under the cursor stationary.
            stage.addEventListener('wheel', function (e) {
              if (mode !== '2d') { return; }
              e.preventDefault();
              var rect = stage.getBoundingClientRect();
              var mx = e.clientX - rect.left, my = e.clientY - rect.top;
              var next = Math.max(0.2, Math.min(3, view2d.scale * (e.deltaY < 0 ? 1.1 : 1 / 1.1)));
              view2d.tx = mx - (mx - view2d.tx) * (next / view2d.scale);
              view2d.ty = my - (my - view2d.ty) * (next / view2d.scale);
              view2d.scale = next;
              place();
            }, { passive: false });

            function project(p) {
              // Rotate around Y then X, then perspective-project. Root is at the origin, so it stays
              // centred and full-size in both modes.
              var cosY = Math.cos(rotY), sinY = Math.sin(rotY);
              var cosX = Math.cos(rotX), sinX = Math.sin(rotX);
              var x1 = p.x * cosY + p.z * sinY;
              var z1 = -p.x * sinY + p.z * cosY;
              var y2 = p.y * cosX - z1 * sinX;
              var z2 = p.y * sinX + z1 * cosX;
              var width = stage.clientWidth || 800, height = stage.clientHeight || 420;
              var cx = width / 2, cy = height / 2;
              var sr = Math.max(80, Math.min(cx, cy) - 70);
              var d = sr * 2.6;
              var persp = d / (d - z2 * sr);
              var scale = Math.max(0.6, Math.min(1.4, persp));
              return {
                x: cx + x1 * sr * persp,
                y: cy + y2 * sr * persp,
                scale: scale,
                depth: z2   // -1 (far) .. 1 (near)
              };
            }

            // A bounded Fruchterman-Reingold settle for the 2D view. The root stays pinned at the
            // centre; the rest are nudged apart (repulsion) and pulled along their edges (springs)
            // for a number of passes that SHRINKS as the graph grows, so the cost stays roughly
            // constant and a huge graph falls back to its phyllotaxis spread rather than freezing the
            // UI. It runs once per render and then stops — a layout that keeps moving while you read
            // it is hard to point at.
            function layout2d(recs, edges, width, height) {
              var n = recs.length;
              if (n <= 1) { return; }
              var margin = 40;
              var cx = width / 2, cy = height / 2;

              var index = {};
              recs.forEach(function (r, i) { index[r.id] = i; });
              var springs = [];
              (edges || []).forEach(function (e) {
                var a = index[e.from], b = index[e.to];
                if (a != null && b != null && a !== b) { springs.push([a, b]); }
              });

              var iters = Math.max(1, Math.min(300, Math.floor(4000000 / (n * n))));
              var k = Math.sqrt(Math.max(1, (width - 2 * margin) * (height - 2 * margin)) / n) * 0.8;

              for (var it = 0; it < iters; it++) {
                var dx = new Float64Array(n), dy = new Float64Array(n);

                for (var a = 0; a < n; a++) {
                  for (var b = a + 1; b < n; b++) {
                    var rx = recs[a].p2.x - recs[b].p2.x, ry = recs[a].p2.y - recs[b].p2.y;
                    var dist = Math.sqrt(rx * rx + ry * ry) || 0.01;
                    var rep = (k * k) / dist, ux = rx / dist, uy = ry / dist;
                    dx[a] += ux * rep; dy[a] += uy * rep;
                    dx[b] -= ux * rep; dy[b] -= uy * rep;
                  }
                }

                springs.forEach(function (s) {
                  var pa = recs[s[0]].p2, pb = recs[s[1]].p2;
                  var sx = pa.x - pb.x, sy = pa.y - pb.y;
                  var dist = Math.sqrt(sx * sx + sy * sy) || 0.01;
                  var att = (dist * dist) / k, ux = sx / dist, uy = sy / dist;
                  dx[s[0]] -= ux * att; dy[s[0]] -= uy * att;
                  dx[s[1]] += ux * att; dy[s[1]] += uy * att;
                });

                var temp = Math.max(2, (1 - it / iters) * (Math.min(width, height) / 8));
                for (var c = 0; c < n; c++) {
                  if (recs[c].isRoot) { recs[c].p2.x = cx; recs[c].p2.y = cy; continue; }
                  var d = Math.sqrt(dx[c] * dx[c] + dy[c] * dy[c]) || 0.01;
                  var step = Math.min(d, temp);
                  recs[c].p2.x += (dx[c] / d) * step + (cx - recs[c].p2.x) * 0.01;
                  recs[c].p2.y += (dy[c] / d) * step + (cy - recs[c].p2.y) * 0.01;
                  recs[c].p2.x = Math.max(margin, Math.min(width - margin, recs[c].p2.x));
                  recs[c].p2.y = Math.max(margin, Math.min(height - margin, recs[c].p2.y));
                }
              }
            }

            // Frame the settled 2D layout: centre its bounding box and scale it to fill the stage.
            function fit() {
              var width = stage.clientWidth || 800, height = stage.clientHeight || 420;
              var margin = 50;
              if (!records.length) { view2d = { scale: 1, tx: 0, ty: 0 }; return; }
              var minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
              records.forEach(function (r) {
                if (r.p2.x < minX) { minX = r.p2.x; }
                if (r.p2.x > maxX) { maxX = r.p2.x; }
                if (r.p2.y < minY) { minY = r.p2.y; }
                if (r.p2.y > maxY) { maxY = r.p2.y; }
              });
              var gw = Math.max(1, maxX - minX), gh = Math.max(1, maxY - minY);
              var scale = Math.max(0.2, Math.min(2.5,
                Math.min((width - 2 * margin) / gw, (height - 2 * margin) / gh)));
              view2d.scale = scale;
              view2d.tx = width / 2 - ((minX + maxX) / 2) * scale;
              view2d.ty = height / 2 - ((minY + maxY) / 2) * scale;
            }

            function place() {
              var width = stage.clientWidth || 800, height = stage.clientHeight || 420;
              var cx = width / 2, cy = height / 2;
              var centres = {};

              records.forEach(function (r) {
                if (mode === '3d') {
                  var pr = project(r.p3);
                  r.el.style.left = pr.x + 'px';
                  r.el.style.top = pr.y + 'px';
                  r.el.style.transform = 'translate(-50%, -50%) scale(' + pr.scale.toFixed(3) + ')';
                  r.el.style.opacity = r.isRoot ? '1' : (0.5 + (pr.depth + 1) * 0.25).toFixed(3);
                  r.el.style.zIndex = String(1000 + Math.round(pr.depth * 500));
                  centres[r.id] = { x: pr.x, y: pr.y };
                } else {
                  var sx = view2d.tx + r.p2.x * view2d.scale;
                  var sy = view2d.ty + r.p2.y * view2d.scale;
                  r.el.style.left = sx + 'px';
                  r.el.style.top = sy + 'px';
                  r.el.style.transform = 'translate(-50%, -50%)';
                  r.el.style.opacity = '1';
                  r.el.style.zIndex = '';
                  centres[r.id] = { x: sx, y: sy };
                }
              });

              edgeRecs.forEach(function (er) {
                var a = centres[er.from], b = centres[er.to];
                if (!a || !b) { er.line.setAttribute('x1', -10); er.line.setAttribute('x2', -10); return; }
                er.line.setAttribute('x1', a.x); er.line.setAttribute('y1', a.y);
                er.line.setAttribute('x2', b.x); er.line.setAttribute('y2', b.y);
              });
            }

            function render(graph) {
              lastGraph = graph;
              var svg = document.getElementById('edges');
              var caption = document.getElementById('caption');
              var warn = document.getElementById('warn');

              Array.prototype.slice.call(stage.querySelectorAll('.node')).forEach(function (n) { n.remove(); });
              svg.innerHTML = '';
              records = [];
              edgeRecs = [];

              current = graph.rootId;
              backButton.disabled = history.length === 0;

              var nodes = graph.nodes || [];
              var width = stage.clientWidth || 800;
              var height = stage.clientHeight || 420;
              var cx = width / 2;
              var cy = height / 2;
              var contexts = {};
              var uncovered = 0;
              var others = nodes.filter(function (n) { return !n.isRoot; });
              var radius = Math.max(80, Math.min(cx, cy) - 70);
              var n3 = others.length;

              // Undirected degree per node, from the edge list, so a hub can be drawn larger.
              var degree = {};
              (graph.edges || []).forEach(function (e) {
                degree[e.from] = (degree[e.from] || 0) + 1;
                degree[e.to] = (degree[e.to] || 0) + 1;
              });

              // Root centred, neighbours on a ring (2D) AND on a sphere (3D). Deliberately NOT a
              // force simulation in either mode: a layout that keeps moving while you read it makes a
              // node hard to point at, and position carries no meaning here beyond "attached to the
              // root". The sphere uses a Fibonacci lattice so neighbours spread evenly.
              nodes.forEach(function (n) {
                var el = document.createElement('span');
                el.className = 'node' + (n.isRoot ? ' root' : '');
                el.tabIndex = 0;
                var lbl = document.createElement('span');
                lbl.className = 'lbl';
                lbl.textContent = n.label;   // the accessible name, and the on-demand visible label
                el.appendChild(lbl);
                el.title = n.id;
                el.setAttribute('data-id', n.id);

                // Degree-sized dot: a hub reads as bigger, so the eye finds the load-bearing nodes.
                var deg = degree[n.id] || 0;
                el.style.setProperty('--r', (n.isRoot ? 18 : Math.min(26, 9 + deg * 3)) + 'px');

                var x = cx, y = cy;
                var p3 = { x: 0, y: 0, z: 0 };
                if (!n.isRoot) {
                  var i = others.indexOf(n);
                  // Phyllotaxis (sunflower) spread for the initial 2D placement: neighbours fan out
                  // across the plane instead of piling onto a single ring, so even a large graph — or
                  // one the force pass barely settles — is not a single overlapping blob (DC-036).
                  var t = (i + 1) / Math.max(1, others.length);
                  var rr = radius * Math.sqrt(t);
                  var aa = (i + 1) * Math.PI * (3 - Math.sqrt(5));
                  x = cx + Math.cos(aa) * rr;
                  y = cy + Math.sin(aa) * rr;

                  var k = i + 0.5;
                  var phi = Math.acos(1 - 2 * k / Math.max(1, n3));
                  var theta = Math.PI * (1 + Math.sqrt(5)) * k;
                  p3 = {
                    x: Math.sin(phi) * Math.cos(theta),
                    y: Math.sin(phi) * Math.sin(theta),
                    z: Math.cos(phi)
                  };
                }

                if (n.context) {
                  var hue = 0, i2 = 0;
                  for (i2 = 0; i2 < n.context.length; i2++) { hue = (hue * 31 + n.context.charCodeAt(i2)) % 360; }
                  el.style.setProperty('--dot', 'hsl(' + hue + ', 50%, 45%)');
                  el.style.setProperty('--dotb', 'hsl(' + hue + ', 55%, 62%)');
                  el.title = n.id + '  [' + n.context + ']';
                  contexts[n.context] = 'hsl(' + hue + ', 55%, 62%)';
                } else if (!n.isRoot) {
                  uncovered++;
                }

                el.addEventListener('click', function () { activate(n.id); });
                stage.appendChild(el);
                records.push({ id: n.id, isRoot: !!n.isRoot, el: el, p2: { x: x, y: y }, p3: p3 });
              });

              var joins = 0, inferred = 0;

              (graph.edges || []).forEach(function (edge) {
                var line = document.createElementNS('http://www.w3.org/2000/svg', 'line');

                if (edge.isJoin) {
                  joins++;
                  line.setAttribute('stroke', edge.isInferred ? '#D8A650' : '#5B9DD9');
                  line.setAttribute('stroke-width', '2');
                  if (edge.isInferred) { line.setAttribute('stroke-dasharray', '5 4'); inferred++; }
                  var title = document.createElementNS('http://www.w3.org/2000/svg', 'title');
                  title.textContent = edge.predicate + ' (' + edge.status + ')';
                  line.appendChild(title);
                } else {
                  line.setAttribute('stroke', '#2A313B');
                }

                svg.appendChild(line);
                edgeRecs.push({ from: edge.from, to: edge.to, line: line });
              });

              layout2d(records, graph.edges, stage.clientWidth || 800, stage.clientHeight || 420);
              fit();
              place();

              var legend = document.getElementById('legend');
              var contextNames = Object.keys(contexts);
              var contextLegend = contextNames.length === 0
                ? ''
                : 'contexts: ' + contextNames.map(function (c) {
                    return '<b style="color:' + contexts[c] + '">' + c + '</b>';
                  }).join(', ')
                  + (uncovered > 0 ? ' &middot; ' + uncovered + ' node(s) in no declared context' : '')
                  + '<br>';

              legend.innerHTML = contextLegend + (joins === 0
                ? ''
                : joins + ' join(s) across artifact types: '
                  + '<b style="color:#5B9DD9">solid blue</b> = declared, '
                  + '<b style="color:#D8A650">dashed amber</b> = inferred from a convention ('
                  + inferred + ' of ' + joins + '). Hover a line for its basis.');

              caption.textContent = graph.message
                ? graph.message
                : nodes.length + ' node(s), ' + (graph.edges || []).length + ' edge(s). '
                  + 'Tab or hover a dot to see its label; Enter or click focuses it; drag to pan and '
                  + 'scroll to zoom (Fit reframes); Backspace goes back; 2/3 toggles 2D/3D; drag to '
                  + 'rotate in 3D; Tab off either end to leave.';

              var notes = [];
              if (graph.omitted > 0) { notes.push(graph.omitted + ' edge(s) omitted by the result bound'); }
              if ((graph.disclosures || []).length) { notes.push('not analysed: ' + graph.disclosures.join(', ')); }
              warn.textContent = notes.join(' - ');

              claimFocus();
            }

            window.chrome.webview.addEventListener('message', function (e) {
              var payload = e.data;
              if (payload && payload.kind === 'graph') { render(payload.graph); }
            });
          </script>
        </body>
        </html>
        """;
}
