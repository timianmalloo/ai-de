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
          body { font: 14px system-ui, sans-serif; margin: 0; padding: 12px 16px; background: #1e1e1e; color: #ddd; }
          header { display: flex; align-items: baseline; gap: 12px; }
          h1 { font-size: 15px; margin: 0; }
          button.chrome { font: inherit; background: #2a2a2a; color: #ddd; border: 1px solid #555;
                  border-radius: 8px; padding: 2px 10px; cursor: pointer; }
          button.chrome[disabled] { opacity: .4; cursor: default; }
          #mode { margin-left: auto; }
          #caption { color: #999; margin: 6px 0 0; }
          #warn { color: #e8b339; margin: 4px 0 0; min-height: 1em; }
          #stage { position: relative; height: 440px; margin-top: 10px; border-radius: 10px;
                   background: #1b1b1c; overflow: hidden; }
          #stage.grab { cursor: grab; }
          #stage.grabbing { cursor: grabbing; }
          svg { position: absolute; inset: 0; width: 100%; height: 100%; }
          .node { position: absolute; transform: translate(-50%, -50%); padding: 6px 10px;
                  border: 1px solid #555; border-radius: 8px; background: #2a2a2a; cursor: pointer;
                  white-space: nowrap; max-width: 210px; overflow: hidden; text-overflow: ellipsis; }
          .node.root { border-color: #5B9DD9; background: #21303f; font-weight: 600; }
          .legend { color: #999; font-size: 12px; margin-top: 8px; }
          .legend b { color: #ddd; font-weight: 600; }
          .node:focus { outline: 2px solid #5B9DD9; outline-offset: 2px; }
        </style></head>
        <body>
          <header>
            <h1>Graph canvas</h1>
            <button id="back" class="chrome" disabled>&#8592; Back</button>
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
            var stage = document.getElementById('stage');

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
              stage.classList.toggle('grab', mode === '3d');
              place();
            }
            modeButton.addEventListener('click', function () { setMode(mode === '3d' ? '2d' : '3d'); });

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

            // Drag-to-rotate, 3D only, and only when the drag starts on empty stage (not on a node),
            // so a node's click-to-activate is never stolen by a rotation gesture.
            var dragging = false, dragX = 0, dragY = 0;
            stage.addEventListener('pointerdown', function (e) {
              if (mode !== '3d') { return; }
              if (e.target.classList && e.target.classList.contains('node')) { return; }
              dragging = true; dragX = e.clientX; dragY = e.clientY;
              stage.classList.add('grabbing');
              stage.setPointerCapture(e.pointerId);
            });
            stage.addEventListener('pointermove', function (e) {
              if (!dragging) { return; }
              rotY += (e.clientX - dragX) * 0.01;
              rotX += (e.clientY - dragY) * 0.01;
              rotX = Math.max(-1.4, Math.min(1.4, rotX));
              dragX = e.clientX; dragY = e.clientY;
              place();
            });
            function endDrag() { dragging = false; stage.classList.remove('grabbing'); }
            stage.addEventListener('pointerup', endDrag);
            stage.addEventListener('pointercancel', endDrag);

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
                  r.el.style.left = r.p2.x + 'px';
                  r.el.style.top = r.p2.y + 'px';
                  r.el.style.transform = 'translate(-50%, -50%)';
                  r.el.style.opacity = '1';
                  r.el.style.zIndex = '';
                  centres[r.id] = { x: r.p2.x, y: r.p2.y };
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

              // Root centred, neighbours on a ring (2D) AND on a sphere (3D). Deliberately NOT a
              // force simulation in either mode: a layout that keeps moving while you read it makes a
              // node hard to point at, and position carries no meaning here beyond "attached to the
              // root". The sphere uses a Fibonacci lattice so neighbours spread evenly.
              nodes.forEach(function (n) {
                var el = document.createElement('span');
                el.className = 'node' + (n.isRoot ? ' root' : '');
                el.tabIndex = 0;
                el.textContent = n.label;
                el.title = n.id;
                el.setAttribute('data-id', n.id);

                var x = cx, y = cy;
                var p3 = { x: 0, y: 0, z: 0 };
                if (!n.isRoot) {
                  var i = others.indexOf(n);
                  var angle = (i / Math.max(1, others.length)) * Math.PI * 2 - Math.PI / 2;
                  x = cx + Math.cos(angle) * radius;
                  y = cy + Math.sin(angle) * radius;

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
                  el.style.borderColor = 'hsl(' + hue + ', 55%, 55%)';
                  el.title = n.id + '  [' + n.context + ']';
                  contexts[n.context] = 'hsl(' + hue + ', 55%, 55%)';
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
                  line.setAttribute('stroke', edge.isInferred ? '#c98b2e' : '#5B9DD9');
                  line.setAttribute('stroke-width', '2');
                  if (edge.isInferred) { line.setAttribute('stroke-dasharray', '5 4'); inferred++; }
                  var title = document.createElementNS('http://www.w3.org/2000/svg', 'title');
                  title.textContent = edge.predicate + ' (' + edge.status + ')';
                  line.appendChild(title);
                } else {
                  line.setAttribute('stroke', '#3d3d3d');
                }

                svg.appendChild(line);
                edgeRecs.push({ from: edge.from, to: edge.to, line: line });
              });

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
                  + '<b style="color:#c98b2e">dashed amber</b> = inferred from a convention ('
                  + inferred + ' of ' + joins + '). Hover a line for its basis.');

              caption.textContent = graph.message
                ? graph.message
                : nodes.length + ' node(s), ' + (graph.edges || []).length + ' edge(s). '
                  + 'Enter or click focuses a node; Backspace goes back; 2/3 toggles 2D/3D; '
                  + 'drag to rotate in 3D; Tab off either end to leave.';

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
