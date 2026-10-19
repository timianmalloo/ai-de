// THE SHIPPED HARNESS, page half (ADR-0043 NFR-02: editor p95 < 50 ms at 100k characters).
// WL-2 reuses it as-is: add `harness.extension` to the EditorView, expose `harness.api`, and drive it
// with the host half (LatencyHarness.cs) — real keystrokes through SendInput.
//
// WHAT ONE SAMPLE IS. From the keydown event's own timestamp (the time the browser took the OS
// input, so OS -> browser -> renderer queueing is inside the sample) to the first task after the
// frame that rendered the document change that keystroke caused. The frame is found by requesting
// an animation frame from the editor's update — not from keydown, because the character arrives in
// a later task (keydown and the char event are separate), and a frame scheduled at keydown can
// paint before the edit exists. A MessageChannel task posted from that frame's rAF callback runs
// after the frame's style, layout and paint: the next-paint approximation.
//
// Each keydown is matched FIFO to the next user-typed document change; a keydown that edits nothing
// would leave the queue longer than the samples, which the host sees as samples < keys (the
// non-vacuity guard), never as a fast sample.
//
// The browser's own Event Timing API is recorded alongside as an independent cross-check. Its
// floor is 16 ms (entries shorter than that are not reported at all), so it cannot give a p95 — it
// can only say how many keydowns took longer than a frame budget.
import { EditorView } from "@codemirror/view";

export function latencyHarness(post) {
  let on = false;
  let seq = 0;
  let pending = [];
  let samples = [];
  let eventTiming = [];
  let observer = null;
  const channel = new MessageChannel();
  channel.port1.onmessage = (e) => {
    const { s, t0 } = e.data;
    samples.push(performance.now() - t0);
    post({ k: "painted", seq: s });
  };

  const onKeydown = (e) => {
    if (on) {
      pending.push({ s: seq++, t0: e.timeStamp });
    }
  };

  const extension = [
    EditorView.domEventHandlers({ keydown: onKeydown }),
    EditorView.updateListener.of((update) => {
      if (!on || !update.docChanged || pending.length === 0) {
        return;
      }
      if (!update.transactions.some((tr) => tr.isUserEvent("input.type"))) {
        return;
      }
      const stamp = pending.shift();
      requestAnimationFrame(() => channel.port2.postMessage(stamp));
    }),
  ];

  const api = {
    start() {
      on = true;
      seq = 0;
      pending = [];
      samples = [];
      eventTiming = [];
      observer = new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
          if (entry.name === "keydown") {
            eventTiming.push(entry.duration);
          }
        }
      });
      observer.observe({ type: "event", durationThreshold: 16 });
    },
    stop() {
      on = false;
      if (observer) {
        observer.disconnect();
      }
      return { samples, eventTiming, unmatchedKeydowns: pending.length };
    },
    count() {
      return samples.length;
    },
  };

  return { extension, api };
}
