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
          #back { font: inherit; background: #2a2a2a; color: #ddd; border: 1px solid #555;
                  border-radius: 4px; padding: 2px 10px; cursor: pointer; }
          #back[disabled] { opacity: .4; cursor: default; }
          #caption { color: #999; margin: 6px 0 0; }
          #warn { color: #e8b339; margin: 4px 0 0; min-height: 1em; }
          #stage { position: relative; height: 440px; margin-top: 10px; }
          svg { position: absolute; inset: 0; width: 100%; height: 100%; }
          .node { position: absolute; transform: translate(-50%, -50%); padding: 6px 10px;
                  border: 1px solid #555; border-radius: 4px; background: #2a2a2a; cursor: pointer;
                  white-space: nowrap; max-width: 210px; overflow: hidden; text-overflow: ellipsis; }
          .node.root { border-color: #4da3ff; background: #21303f; font-weight: 600; }
          .legend { color: #999; font-size: 12px; margin-top: 8px; }
          .legend b { color: #ddd; font-weight: 600; }
          .node:focus { outline: 2px solid #4da3ff; outline-offset: 2px; }
        </style></head>
        <body>
          <header>
            <h1>Graph canvas</h1>
            <button id="back" disabled>&#8592; Back</button>
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

            function activate(nodeId) {
              if (!nodeId || nodeId === current) { return; }
              if (current) { history.push(current); }
              post({ kind: 'node.activate', nodeId: nodeId });
            }

            backButton.addEventListener('click', function () {
              if (!history.length) { return; }
              // Popped BEFORE the request, and current cleared, so the render that follows does not
              // push the entry straight back on.
              var previous = history.pop();
              current = null;
              post({ kind: 'node.activate', nodeId: previous });
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

              // An empty graph must not be a trap either: with nothing to tab through, both
              // directions leave.
              if (nodes.length === 0) { e.preventDefault(); leave(e.shiftKey ? 'backward' : 'forward'); return; }
              if (!e.shiftKey && active === nodes[nodes.length - 1]) { e.preventDefault(); leave('forward'); }
              else if (e.shiftKey && (active === nodes[0] || active === document.body)) {
                e.preventDefault(); leave('backward');
              }
            });

            function render(graph) {
              var stage = document.getElementById('stage');
              var svg = document.getElementById('edges');
              var caption = document.getElementById('caption');
              var warn = document.getElementById('warn');

              Array.prototype.slice.call(stage.querySelectorAll('.node')).forEach(function (n) { n.remove(); });
              svg.innerHTML = '';

              current = graph.rootId;
              backButton.disabled = history.length === 0;

              var nodes = graph.nodes || [];
              var width = stage.clientWidth || 800;
              var height = stage.clientHeight || 420;
              var cx = width / 2;
              var cy = height / 2;
              var placed = {};
              var others = nodes.filter(function (n) { return !n.isRoot; });
              var radius = Math.max(80, Math.min(cx, cy) - 70);

              // Root centred, neighbours on a ring. Deliberately NOT a force simulation: a layout
              // that keeps moving while you read it makes a node hard to point at, and position
              // carries no meaning here beyond "attached to the root".
              nodes.forEach(function (n) {
                var el = document.createElement('span');
                el.className = 'node' + (n.isRoot ? ' root' : '');
                el.tabIndex = 0;
                el.textContent = n.label;
                el.title = n.id;
                el.setAttribute('data-id', n.id);

                var x = cx, y = cy;
                if (!n.isRoot) {
                  var i = others.indexOf(n);
                  var angle = (i / Math.max(1, others.length)) * Math.PI * 2 - Math.PI / 2;
                  x = cx + Math.cos(angle) * radius;
                  y = cy + Math.sin(angle) * radius;
                }

                el.style.left = x + 'px';
                el.style.top = y + 'px';
                el.addEventListener('click', function () { activate(n.id); });
                stage.appendChild(el);
                placed[n.id] = { x: x, y: y };
              });

              var joins = 0, inferred = 0;

              (graph.edges || []).forEach(function (edge) {
                var a = placed[edge.from], b = placed[edge.to];
                if (!a || !b) { return; }
                var line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
                line.setAttribute('x1', a.x); line.setAttribute('y1', a.y);
                line.setAttribute('x2', b.x); line.setAttribute('y2', b.y);

                // A join across artifact types is drawn differently from a compiler-resolved edge,
                // and an INFERRED one is dashed. A convention-derived link between a class and a
                // table looks more authoritative than it is precisely because it spans more, so the
                // drawing has to say which kind of claim it is.
                if (edge.isJoin) {
                  joins++;
                  line.setAttribute('stroke', edge.isInferred ? '#c98b2e' : '#4da3ff');
                  line.setAttribute('stroke-width', '2');
                  if (edge.isInferred) { line.setAttribute('stroke-dasharray', '5 4'); inferred++; }
                  var title = document.createElementNS('http://www.w3.org/2000/svg', 'title');
                  title.textContent = edge.predicate + ' (' + edge.status + ')';
                  line.appendChild(title);
                } else {
                  line.setAttribute('stroke', '#3d3d3d');
                }

                svg.appendChild(line);
              });

              var legend = document.getElementById('legend');
              legend.innerHTML = joins === 0
                ? ''
                : joins + ' join(s) across artifact types: '
                  + '<b style="color:#4da3ff">solid blue</b> = declared, '
                  + '<b style="color:#c98b2e">dashed amber</b> = inferred from a convention ('
                  + inferred + ' of ' + joins + '). Hover a line for its basis.';

              caption.textContent = graph.message
                ? graph.message
                : nodes.length + ' node(s), ' + (graph.edges || []).length + ' edge(s). '
                  + 'Enter or click focuses a node; Backspace goes back; Tab off either end to leave.';

              // Truncation and non-extraction are different, and both are stated: omitted edges
              // exist and were not returned, disclosures were never extracted at all.
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
