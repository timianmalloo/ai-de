import { useState, useEffect, useMemo } from "react";

// ---------- tokens ----------
const T = {
  bg: "#0E1620", panel: "#16212D", panel2: "#1C2937", line: "#27384A",
  ink: "#D7E0EA", mute: "#7F91A4", amber: "#F2B544", teal: "#3FB8AF",
  red: "#E0564F", violet: "#9C8CF0",
  mono: "ui-monospace, 'Cascadia Code', 'JetBrains Mono', Menlo, Consolas, monospace",
  sans: "'Segoe UI Variable', 'Segoe UI', system-ui, -apple-system, sans-serif",
};
const AGENT_COLORS = { fable: T.teal, opus: T.violet, "gpt-5.6": T.amber, copilot: "#6FCF97" };

// ---------- content ----------
const PRIORITIES = [
  { id: "conflicts", label: "Fewer merge conflicts" },
  { id: "parallel", label: "More parallelism" },
  { id: "agnostic", label: "Model / tool agnostic" },
  { id: "ops", label: "Low operational overhead" },
  { id: "audit", label: "Auditability & replay" },
];

// scores 1-5 per priority
const DECISIONS = [
  {
    id: "substrate", title: "Where does shared state live?",
    why: "Everything else depends on this. It must be readable by Claude Code, Copilot agent, and anything driving GPT — usually via a CLI, a file, or an MCP tool.",
    options: [
      { name: "Git-tracked files in repo (.agents/)", s: { conflicts: 2, parallel: 2, agnostic: 5, ops: 5, audit: 4 },
        pro: "Zero infra; survives clones; reviewable in PRs.", con: "The coordination files themselves become a merge hotspot unless append-only + one-file-per-agent." },
      { name: "Git-ignored local store + git worktrees", s: { conflicts: 3, parallel: 3, agnostic: 4, ops: 5, audit: 2 },
        pro: "No noise in history; fast.", con: "Only works on one machine; lost if the folder dies." },
      { name: "Local daemon + SQLite (MCP server)", s: { conflicts: 5, parallel: 5, agnostic: 5, ops: 3, audit: 5 },
        pro: "Atomic leases, real queries, one MCP surface every agent can call.", con: "You now own a small service; needs a startup story." },
      { name: "Hosted (Azure Table/Cosmos + Service Bus)", s: { conflicts: 5, parallel: 5, agnostic: 5, ops: 1, audit: 5 },
        pro: "Multi-machine, cloud agents included.", con: "Heaviest; overkill until agents run off-box." },
    ],
  },
  {
    id: "protocol", title: "What is the coordination protocol?",
    why: "You called it a shared console: an append-only stream of intent, not just output. The stream is the source of truth; every view is a projection of it.",
    options: [
      { name: "Append-only JSONL event log (one file per agent)", s: { conflicts: 4, parallel: 4, agnostic: 5, ops: 5, audit: 5 },
        pro: "Trivially mergeable; tail -f works; replayable.", con: "Readers must fold events into state themselves." },
      { name: "MCP tools: claim / release / announce / query", s: { conflicts: 5, parallel: 5, agnostic: 4, ops: 3, audit: 4 },
        pro: "Agents call it natively; server enforces invariants.", con: "Copilot CLI and Claude Code both support MCP, but config differs per tool." },
      { name: "Git notes / commit trailers", s: { conflicts: 2, parallel: 2, agnostic: 4, ops: 4, audit: 4 },
        pro: "Travels with commits.", con: "Too late — you learn about overlap after work is done." },
      { name: "PR / issue comments as the bus", s: { conflicts: 2, parallel: 3, agnostic: 5, ops: 4, audit: 5 },
        pro: "Humans see it; GitHub-native.", con: "Slow, rate-limited, not local." },
    ],
  },
  {
    id: "claims", title: "What granularity do agents claim?",
    why: "Claims are the conflict-avoidance mechanism. Too coarse kills parallelism; too fine is noise nobody honours.",
    options: [
      { name: "Directory / project (.csproj)", s: { conflicts: 5, parallel: 2, agnostic: 5, ops: 5, audit: 4 },
        pro: "Maps to your LOA layers; easy to reason about.", con: "Two agents in the same project serialise." },
      { name: "File path", s: { conflicts: 4, parallel: 4, agnostic: 5, ops: 4, audit: 4 },
        pro: "Matches how git conflicts actually happen.", con: "Shared files (DI registration, csproj, migrations) still collide." },
      { name: "Symbol (Roslyn: type / member)", s: { conflicts: 5, parallel: 5, agnostic: 3, ops: 2, audit: 4 },
        pro: "True parallelism inside large files; you already own the analyzer stack.", con: "Needs Roslyn tooling in the loop; non-C# files fall back to path." },
      { name: "Feature / work-item only (no file claims)", s: { conflicts: 2, parallel: 5, agnostic: 5, ops: 5, audit: 3 },
        pro: "Lowest friction.", con: "Relies on the backlog being well-partitioned — it usually isn't." },
    ],
  },
  {
    id: "strategy", title: "Conflict strategy",
    why: "Claims tell you about overlap; strategy decides what happens next.",
    options: [
      { name: "Advisory leases (warn, don't block)", s: { conflicts: 3, parallel: 5, agnostic: 5, ops: 5, audit: 3 },
        pro: "Agents stay autonomous; simple.", con: "Models ignore warnings under pressure." },
      { name: "Hard leases with TTL + heartbeat", s: { conflicts: 5, parallel: 4, agnostic: 4, ops: 3, audit: 5 },
        pro: "Guarantees exclusivity; dead agents expire.", con: "Needs a pre-edit hook to enforce (Claude Code has PreToolUse; Copilot needs a wrapper)." },
      { name: "Partition the backlog up front (planner agent)", s: { conflicts: 4, parallel: 4, agnostic: 5, ops: 4, audit: 4 },
        pro: "Conflicts avoided by design, not detection.", con: "Planner must know the dependency graph; stale fast." },
      { name: "Optimistic + merge-queue + auto-rebase", s: { conflicts: 2, parallel: 5, agnostic: 5, ops: 3, audit: 5 },
        pro: "Fully parallel; conflicts resolved by a dedicated agent.", con: "Rebase agent can silently break semantics." },
    ],
  },
  {
    id: "graph", title: "Knowledge graph representation",
    why: "Orthogonal to working trees, scoped to in-flight goals. Nodes: Goal, WorkItem, Artifact, Decision, Agent, Run. Edges: decomposes, touches, depends-on, decided-by, blocked-by.",
    options: [
      { name: "Markdown + YAML front-matter (Obsidian-style)", s: { conflicts: 3, parallel: 3, agnostic: 5, ops: 5, audit: 4 },
        pro: "Every model reads it without tools; humans too.", con: "Querying 'who touches X' means grep." },
      { name: "SQLite tables + views (served via MCP)", s: { conflicts: 5, parallel: 5, agnostic: 4, ops: 3, audit: 5 },
        pro: "Real joins; one query answers 'what blocks goal G'.", con: "Opaque to agents without the tool." },
      { name: "JSON-LD / typed JSON files per node", s: { conflicts: 4, parallel: 4, agnostic: 5, ops: 4, audit: 4 },
        pro: "Schema-checkable; maps cleanly to C# records.", con: "Many small files; needs an index." },
      { name: "Graph DB (Neo4j / Cosmos Gremlin)", s: { conflicts: 5, parallel: 5, agnostic: 3, ops: 1, audit: 5 },
        pro: "Native traversal.", con: "Far too heavy for a repo-local tool." },
    ],
  },
];

const EVENTS = [
  ["fable", "claim", "src/HealthHub.Api/Whoop/*.cs", "WI-142"],
  ["opus", "claim", "src/HealthHub.Data/Migrations/", "WI-138"],
  ["copilot", "announce", "intent: extract IResMedClient", "WI-151"],
  ["gpt-5.6", "claim", "HealthHub.Dashboard/Pages/Sleep.razor", "WI-140"],
  ["fable", "touch", "Program.cs  ⚠ shared file", "WI-142"],
  ["opus", "decision", "EF migration naming: <Date>_<WI>_<Slug>", "WI-138"],
  ["copilot", "claim", "src/HealthHub.Api/ResMed/*.cs", "WI-151"],
  ["gpt-5.6", "blocked", "needs SleepSummaryDto from WI-142", "WI-140"],
  ["fable", "release", "src/HealthHub.Api/Whoop/*.cs", "WI-142"],
  ["fable", "done", "PR #88 opened · 6 files", "WI-142"],
  ["gpt-5.6", "resume", "dep satisfied via WI-142", "WI-140"],
  ["opus", "heartbeat", "lease ttl renewed (5m)", "WI-138"],
];

const RISKS = [
  ["Coordination files become the conflict", "Never let two agents write the same file. One append-only log per agent, folded into state by readers."],
  ["Agents forget to claim", "Enforce at the edge: a PreToolUse hook (Claude Code) / wrapper script (Copilot) that refuses Edit on unclaimed paths."],
  ["Stale leases from dead sessions", "TTL + heartbeat. Expired leases are reclaimable; expiry is itself an event."],
  ["Shared hotspots: Program.cs, DI registration, .csproj, migrations", "Treat as 'append-only regions' or route through a single integrator agent. Roslyn source generators can remove some of them entirely."],
  ["Model disagreement on conventions", "Decisions are first-class nodes; agents must read open decisions for their work item before starting."],
  ["Context bloat", "Agents read a projection (their work item + neighbours), never the full graph. Keep a generated AGENTS.md / CLAUDE.md summary under 2k tokens."],
  ["Goal drift", "Every work item links to a goal; a planner pass prunes items whose goal closed."],
];

const PHASES = [
  ["Week 1", "Protocol + log", "Define event schema as C# records. JSONL per agent under .agents/log/. A `agentctl` dotnet tool: claim, release, announce, status, tail. AGENTS.md explains the rules to every model."],
  ["Week 2", "Enforcement", "Claude Code hooks + Copilot wrapper call `agentctl check <path>` before edits. Leases with TTL. Worktree-per-agent script."],
  ["Week 3", "Graph + MCP", "Fold the log into SQLite; expose query/claim/announce as MCP tools so all three model families use one surface. Generate per-agent context projections."],
  ["Week 4", "Planner + integrator", "Planner agent partitions backlog using the graph; integrator agent owns shared hotspots and the merge queue. Measure: conflicts/PR, agent-hours idle."],
];

// ---------- helpers ----------
const score = (s, w) => PRIORITIES.reduce((a, p) => a + s[p.id] * w[p.id], 0);

function Bar({ v, max, color }) {
  return (
    <div style={{ height: 4, background: T.line, borderRadius: 2 }}>
      <div style={{ width: `${(v / max) * 100}%`, height: "100%", background: color, borderRadius: 2, transition: "width .3s" }} />
    </div>
  );
}

// ---------- tabs ----------
function Console() {
  const [n, setN] = useState(4);
  useEffect(() => {
    const id = setInterval(() => setN(k => (k >= EVENTS.length ? 4 : k + 1)), 1400);
    return () => clearInterval(id);
  }, []);
  const shown = EVENTS.slice(0, n);
  const claims = {};
  shown.forEach(([a, k, what]) => { if (k === "claim") claims[what] = a; if (k === "release") delete claims[what]; });
  return (
    <div>
      <p style={{ color: T.mute, margin: "0 0 12px" }}>
        The shared console is the thesis: every agent appends <em>intent</em> before it touches the tree. State is a fold over the stream.
      </p>
      <div style={{ background: "#0A1018", border: `1px solid ${T.line}`, borderRadius: 8, padding: 12, fontFamily: T.mono, fontSize: 12, minHeight: 230 }}>
        {shown.map(([a, k, what, wi], i) => (
          <div key={i} style={{ display: "flex", gap: 8, padding: "3px 0", opacity: i === shown.length - 1 ? 1 : 0.75 }}>
            <span style={{ color: T.mute, minWidth: 44 }}>{wi}</span>
            <span style={{ color: AGENT_COLORS[a], minWidth: 56 }}>{a}</span>
            <span style={{ color: k === "blocked" ? T.red : k === "claim" ? T.amber : k === "done" ? "#6FCF97" : T.ink, minWidth: 64 }}>{k}</span>
            <span style={{ color: T.ink, wordBreak: "break-all" }}>{what}</span>
          </div>
        ))}
        <span style={{ color: T.teal }}>▌</span>
      </div>
      <div style={{ marginTop: 14, fontSize: 12 }}>
        <div style={{ color: T.mute, textTransform: "uppercase", letterSpacing: 1, fontSize: 10, marginBottom: 6 }}>Active leases (folded state)</div>
        {Object.keys(claims).length === 0 && <div style={{ color: T.mute }}>none</div>}
        {Object.entries(claims).map(([p, a]) => (
          <div key={p} style={{ fontFamily: T.mono, display: "flex", gap: 8, padding: "2px 0" }}>
            <span style={{ color: AGENT_COLORS[a] }}>■</span><span style={{ color: T.ink }}>{p}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function Decisions() {
  const [w, setW] = useState({ conflicts: 5, parallel: 5, agnostic: 4, ops: 3, audit: 2 });
  const [open, setOpen] = useState(DECISIONS[0].id);
  const maxW = 5 * PRIORITIES.reduce((a, p) => a + w[p.id], 0) || 1;
  return (
    <div>
      <div style={{ background: T.panel2, borderRadius: 8, padding: 12, marginBottom: 14 }}>
        <div style={{ color: T.mute, fontSize: 10, textTransform: "uppercase", letterSpacing: 1, marginBottom: 8 }}>Weight what matters</div>
        {PRIORITIES.map(p => (
          <label key={p.id} style={{ display: "grid", gridTemplateColumns: "1fr 90px 20px", gap: 8, alignItems: "center", fontSize: 13, padding: "3px 0" }}>
            <span>{p.label}</span>
            <input type="range" min={0} max={5} value={w[p.id]} onChange={e => setW({ ...w, [p.id]: +e.target.value })} style={{ accentColor: T.amber }} />
            <span style={{ fontFamily: T.mono, color: T.amber }}>{w[p.id]}</span>
          </label>
        ))}
      </div>
      {DECISIONS.map(d => {
        const ranked = [...d.options].map(o => ({ ...o, v: score(o.s, w) })).sort((a, b) => b.v - a.v);
        const isOpen = open === d.id;
        return (
          <div key={d.id} style={{ border: `1px solid ${T.line}`, borderRadius: 8, marginBottom: 8, overflow: "hidden" }}>
            <button onClick={() => setOpen(isOpen ? null : d.id)} style={{ width: "100%", textAlign: "left", background: T.panel2, color: T.ink, border: 0, padding: "10px 12px", fontFamily: T.sans, fontSize: 14, display: "flex", justifyContent: "space-between", cursor: "pointer" }}>
              <span>{d.title}</span>
              <span style={{ color: T.teal, fontSize: 12, fontFamily: T.mono }}>→ {ranked[0].name.split(" (")[0]}</span>
            </button>
            {isOpen && (
              <div style={{ padding: 12 }}>
                <p style={{ color: T.mute, fontSize: 13, margin: "0 0 10px" }}>{d.why}</p>
                {ranked.map((o, i) => (
                  <div key={o.name} style={{ padding: "8px 0", borderTop: i ? `1px solid ${T.line}` : 0 }}>
                    <div style={{ display: "flex", justifyContent: "space-between", fontSize: 13, marginBottom: 4 }}>
                      <span style={{ color: i === 0 ? T.amber : T.ink }}>{o.name}</span>
                      <span style={{ fontFamily: T.mono, color: T.mute }}>{Math.round((o.v / maxW) * 100)}</span>
                    </div>
                    <Bar v={o.v} max={maxW} color={i === 0 ? T.amber : T.teal} />
                    <div style={{ fontSize: 12, color: T.mute, marginTop: 5 }}>
                      <span style={{ color: "#6FCF97" }}>+ </span>{o.pro} <span style={{ color: T.red }}>− </span>{o.con}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

function Protocol() {
  const code = `// .agents/protocol — one JSONL file per agent session
public abstract record AgentEvent(
    string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At);

public sealed record Claim(string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At,
    string Path, ClaimScope Scope,      // Dir | File | Symbol
    TimeSpan Ttl) : AgentEvent(Agent, Model, Session, WorkItem, At);

public sealed record Release(...)  : AgentEvent(...);
public sealed record Heartbeat(...) : AgentEvent(...);
public sealed record Announce(string Intent, string[] LikelyPaths, ...);
public sealed record Decision(string Topic, string Choice, string Rationale, ...);
public sealed record Blocked(string OnWorkItem, string Needs, ...);
public sealed record Done(string PullRequest, string[] Files, ...);

// Folded read model (SQLite view or in-memory)
public sealed record Lease(string Path, string Agent, DateTimeOffset Expires);`;
  const rules = [
    "Announce before claim; claim before edit; release before PR.",
    "Claims are globs over the working tree, never over the log.",
    "Heartbeat every 2 min; lease TTL 5 min; expiry is an event.",
    "Shared hotspots (Program.cs, DI, csproj, migrations) are owned by the integrator.",
    "Decisions are nodes; read open decisions for your work item before starting.",
    "Read your projection, not the graph. Budget: 2k tokens.",
  ];
  return (
    <div>
      <p style={{ color: T.mute, margin: "0 0 12px" }}>Mirror the console, but emit intent. Six verbs are enough.</p>
      <pre style={{ background: "#0A1018", border: `1px solid ${T.line}`, borderRadius: 8, padding: 12, fontFamily: T.mono, fontSize: 11, color: T.ink, overflowX: "auto", margin: 0 }}>{code}</pre>
      <div style={{ color: T.mute, fontSize: 10, textTransform: "uppercase", letterSpacing: 1, margin: "14px 0 6px" }}>Rules every model reads (AGENTS.md)</div>
      {rules.map((r, i) => (
        <div key={i} style={{ display: "flex", gap: 10, fontSize: 13, padding: "4px 0", borderTop: i ? `1px solid ${T.line}` : 0 }}>
          <span style={{ color: T.amber, fontFamily: T.mono }}>§{i + 1}</span><span>{r}</span>
        </div>
      ))}
      <div style={{ marginTop: 14, fontSize: 12, color: T.mute }}>
        Surfaces: <span style={{ color: T.ink }}>agentctl</span> (dotnet tool, CLI) → <span style={{ color: T.ink }}>MCP server</span> (same verbs) → <span style={{ color: T.ink }}>hooks</span> (Claude Code PreToolUse · Copilot wrapper) → <span style={{ color: T.ink }}>AGENTS.md</span> projection.
      </div>
    </div>
  );
}

function Graph() {
  const nodes = [
    { id: "G1", t: "Goal", l: "CPAP + Whoop fused sleep view", x: 50, y: 8 },
    { id: "WI-142", t: "WorkItem", l: "Whoop sleep ingest", x: 18, y: 38, a: "fable" },
    { id: "WI-151", t: "WorkItem", l: "ResMed client extract", x: 50, y: 38, a: "copilot" },
    { id: "WI-140", t: "WorkItem", l: "Sleep.razor page", x: 82, y: 38, a: "gpt-5.6" },
    { id: "A1", t: "Artifact", l: "Whoop/*.cs", x: 10, y: 72 },
    { id: "A2", t: "Artifact", l: "SleepSummaryDto", x: 36, y: 72 },
    { id: "A3", t: "Artifact", l: "ResMed/*.cs", x: 60, y: 72 },
    { id: "A4", t: "Artifact", l: "Sleep.razor", x: 86, y: 72 },
    { id: "D1", t: "Decision", l: "DTO lives in .Contracts", x: 36, y: 94 },
  ];
  const edges = [["G1", "WI-142"], ["G1", "WI-151"], ["G1", "WI-140"], ["WI-142", "A1"], ["WI-142", "A2"], ["WI-151", "A3"], ["WI-140", "A4"], ["WI-140", "A2", "dep"], ["A2", "D1"]];
  const [sel, setSel] = useState("A2");
  const by = Object.fromEntries(nodes.map(n => [n.id, n]));
  const col = { Goal: T.amber, WorkItem: T.teal, Artifact: T.ink, Decision: T.violet };
  return (
    <div>
      <p style={{ color: T.mute, margin: "0 0 10px" }}>Orthogonal to the tree, scoped to what's in flight. Tap a node. The shared artifact is where conflict lives.</p>
      <svg viewBox="0 0 100 100" style={{ width: "100%", height: 300, background: "#0A1018", border: `1px solid ${T.line}`, borderRadius: 8 }}>
        {edges.map(([a, b, k], i) => (
          <line key={i} x1={by[a].x} y1={by[a].y} x2={by[b].x} y2={by[b].y} stroke={k === "dep" ? T.red : T.line} strokeWidth={k === "dep" ? 0.7 : 0.4} strokeDasharray={k === "dep" ? "1.5 1" : ""} />
        ))}
        {nodes.map(n => (
          <g key={n.id} onClick={() => setSel(n.id)} style={{ cursor: "pointer" }}>
            <circle cx={n.x} cy={n.y} r={sel === n.id ? 3.2 : 2.4} fill={n.a ? AGENT_COLORS[n.a] : col[n.t]} stroke={sel === n.id ? "#fff" : "none"} strokeWidth={0.5} />
            <text x={n.x} y={n.y + 6} fontSize={2.6} fill={T.mute} textAnchor="middle" fontFamily={T.mono}>{n.id}</text>
          </g>
        ))}
      </svg>
      <div style={{ background: T.panel2, borderRadius: 8, padding: 12, marginTop: 10, fontSize: 13 }}>
        <div style={{ color: col[by[sel].t], fontFamily: T.mono, fontSize: 11 }}>{by[sel].t} · {sel}{by[sel].a ? ` · leased by ${by[sel].a}` : ""}</div>
        <div style={{ marginTop: 4 }}>{by[sel].l}</div>
        {sel === "A2" && <div style={{ color: T.red, marginTop: 6, fontSize: 12 }}>Touched by WI-142, depended on by WI-140. This edge is what your protocol exists to surface early: fable should announce the DTO shape before writing; gpt-5.6 codes against the contract, not the file.</div>}
        {sel === "G1" && <div style={{ color: T.mute, marginTop: 6, fontSize: 12 }}>Goals bound the graph. When this closes, its work items and decisions are archived out of every agent's projection.</div>}
        {sel === "D1" && <div style={{ color: T.mute, marginTop: 6, fontSize: 12 }}>Decisions attach to artifacts, so any agent about to touch SleepSummaryDto inherits the rule without re-deriving it.</div>}
      </div>
    </div>
  );
}

function Risks() {
  const [done, setDone] = useState({});
  return (
    <div>
      <p style={{ color: T.mute, margin: "0 0 10px" }}>Each one has killed a version of this before. Tick them as you design them out.</p>
      {RISKS.map(([r, m], i) => (
        <label key={i} style={{ display: "flex", gap: 10, padding: "8px 0", borderTop: i ? `1px solid ${T.line}` : 0, cursor: "pointer" }}>
          <input type="checkbox" checked={!!done[i]} onChange={() => setDone({ ...done, [i]: !done[i] })} style={{ accentColor: T.teal, marginTop: 3 }} />
          <div>
            <div style={{ fontSize: 13, color: done[i] ? T.mute : T.ink, textDecoration: done[i] ? "line-through" : "none" }}>{r}</div>
            <div style={{ fontSize: 12, color: T.mute, marginTop: 2 }}>{m}</div>
          </div>
        </label>
      ))}
    </div>
  );
}

function Roadmap() {
  return (
    <div>
      <p style={{ color: T.mute, margin: "0 0 10px" }}>Ship the log first. Everything else is a projection of it, so nothing you build later is wasted.</p>
      {PHASES.map(([w, t, d], i) => (
        <div key={i} style={{ display: "grid", gridTemplateColumns: "56px 1fr", gap: 10, padding: "10px 0", borderTop: i ? `1px solid ${T.line}` : 0 }}>
          <div style={{ fontFamily: T.mono, color: T.amber, fontSize: 12 }}>{w}</div>
          <div><div style={{ fontSize: 14 }}>{t}</div><div style={{ fontSize: 12, color: T.mute, marginTop: 3 }}>{d}</div></div>
        </div>
      ))}
      <div style={{ marginTop: 12, fontSize: 12, color: T.mute, background: T.panel2, padding: 10, borderRadius: 8 }}>
        Success metrics: merge conflicts per PR · % of edits on claimed paths · mean agent idle time waiting on a lease · decisions re-litigated per week.
      </div>
    </div>
  );
}

const TABS = [["Console", Console], ["Decisions", Decisions], ["Protocol", Protocol], ["Graph", Graph], ["Risks", Risks], ["Roadmap", Roadmap]];

export default function App() {
  const [tab, setTab] = useState(0);
  const Body = TABS[tab][1];
  return (
    <div style={{ background: T.bg, color: T.ink, minHeight: "100vh", fontFamily: T.sans, padding: 14, maxWidth: 720, margin: "0 auto" }}>
      <div style={{ fontFamily: T.mono, fontSize: 10, letterSpacing: 2, color: T.teal, textTransform: "uppercase" }}>Parallel agents · shared context</div>
      <h1 style={{ fontSize: 22, fontWeight: 600, margin: "4px 0 2px", letterSpacing: -0.3 }}>Claims, not commits, are the unit of coordination.</h1>
      <div style={{ fontSize: 12, color: T.mute, marginBottom: 12 }}>Claude Code · Copilot · fable · opus · gpt-5.6 — one log, one graph, many worktrees.</div>
      <div style={{ display: "flex", gap: 4, overflowX: "auto", paddingBottom: 8, marginBottom: 10, borderBottom: `1px solid ${T.line}` }}>
        {TABS.map(([n], i) => (
          <button key={n} onClick={() => setTab(i)} style={{ background: tab === i ? T.amber : "transparent", color: tab === i ? "#1a1206" : T.mute, border: `1px solid ${tab === i ? T.amber : T.line}`, borderRadius: 999, padding: "5px 11px", fontSize: 12, fontFamily: T.mono, cursor: "pointer", whiteSpace: "nowrap" }}>{n}</button>
        ))}
      </div>
      <Body />
    </div>
  );
}
